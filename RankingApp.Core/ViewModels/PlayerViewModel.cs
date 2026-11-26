using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using RankingApp.Core.ViewModels.HelperClasses;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.Core.ViewModels
{
    public partial class PlayerViewModel(IPlayerService playerService, INavigationService navigation, IProgressDialogService progressDialog,
        FriendService friendService, AllTournamentsViewModel tournamentsViewModel) : BaseViewModel
    {
        private const int FirstRankingYear = 2014;

        private readonly IProgressDialogService _progressDialog = progressDialog;
        private readonly INavigationService _navigation = navigation;
        private readonly IPlayerService _playerService = playerService;
        private readonly FriendService _friends = friendService;
        private readonly AllTournamentsViewModel _tournamentsViewModel = tournamentsViewModel;
        private HashSet<string> _friendKeys = [];
        private List<PlayerDB>? _allPlayers = [];
        private List<PlayerDB>? _filteredPlayers = [];
        private List<PlayerListItem>? _sortedPlayers = [];
        private AppData? _cachedAppData;
        private System.Timers.Timer? _searchDebounceTimer;

        // Only one ranking download at a time. FormDatePicker sets the date twice when it changes
        // (AddDays(-1), then the real date), which used to start two downloads in parallel.
        private bool _isLoadingPlayers;
        private DateTime? _pendingLoadDate;
        private List<Game> _allGames = [];

        // Month shown when it is not the PlayerDB (latest) month - view only, PlayerDB is never changed by it
        private DateTime? _viewMonth;

        [ObservableProperty]
        private ObservableCollection<Game>? filteredGames;

        [ObservableProperty]
        private GameStatistics? stats;

        [ObservableProperty]
        private bool isGamesOverlayVisible;

        [ObservableProperty]
        private PlayerDB? selectedPlayer;

        [ObservableProperty]
        private ObservableCollection<PlayerListItem>? players;

        [ObservableProperty]
        private string selectedFilter = "Men";

        // ── Shown ranking month: year + month pickers ────────────────────
        // Set from code (first open, newest month downloaded) without loading - only a user pick loads a month
        private bool _settingPickers;

        public ObservableCollection<int> Years { get; } = [];

        [ObservableProperty]
        private ObservableCollection<MonthOption> months = [];

        [ObservableProperty]
        private int selectedYear;

        [ObservableProperty]
        private MonthOption? selectedMonth;

        /// <summary>Month shown in the list, e.g. "2026 October".</summary>
        [ObservableProperty]
        private string? appDataLabel;

        /// <summary>"Latest ranking" or, for an older month, "Viewing · latest 2026 November".</summary>
        [ObservableProperty]
        private string rankingCaption = "Latest ranking";

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private PlayerDB? appDefaultPlayer;

        /// <summary>App player in the shown month's ranking: "12. Name Surname 1520(1610)".</summary>
        [ObservableProperty]
        private string appPlayerLabel = "";

        [ObservableProperty]
        private string selectedSort = "PointsWithBonus";

        // ── Friends popup ────────────────────────────────────────────────
        [ObservableProperty]
        private bool isFriendsOverlayVisible;

        [ObservableProperty]
        private string friendsTitle = "";

        [ObservableProperty]
        private string friendsEmptyText = "";

        public ObservableCollection<FriendRankingRow> FriendRows { get; } = [];

        public List<string> FilterOptions { get; } = ["Men", "Women", "AllActive", "Inactive", "All"];
        public List<string> SortOptions { get; } = ["PointsWithBonus", "Points", "PointsChanged", "Age"];

        // ── Sort direction ────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SortDirectionIcon))]
        private bool sortDescending = true;

        public string SortDirectionIcon => SortDescending ? "↓" : "↑";

        [RelayCommand]
        private void ToggleSortDirection() => SortDescending = !SortDescending;

        partial void OnSortDescendingChanged(bool value) => SortPlayers();

        // ── Paging: 200 players per page, search runs over all players first ──
        private const int PageSize = 200;
        private List<PlayerListItem> _searchResult = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageLabel))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand), nameof(FirstPageCommand), nameof(NextPageCommand), nameof(LastPageCommand))]
        private int currentPage = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageLabel))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand), nameof(FirstPageCommand), nameof(NextPageCommand), nameof(LastPageCommand))]
        private int pageCount = 1;

        /// <summary>"2512 players" or, while searching, "12 found".</summary>
        [ObservableProperty]
        private string playerCountLabel = "";

        public string PageLabel => $"{CurrentPage} / {PageCount}";

        private bool CanGoBack() => CurrentPage > 1;
        private bool CanGoForward() => CurrentPage < PageCount;

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void FirstPage() => ShowPage(1);

        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void PreviousPage() => ShowPage(CurrentPage - 1);

        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void NextPage() => ShowPage(CurrentPage + 1);

        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void LastPage() => ShowPage(PageCount);

        private void ShowPage(int page)
        {
            PageCount = Math.Max(1, (int)Math.Ceiling(_searchResult.Count / (double)PageSize));
            CurrentPage = Math.Clamp(page, 1, PageCount);

            Players = new ObservableCollection<PlayerListItem>(
                _searchResult.Skip((CurrentPage - 1) * PageSize).Take(PageSize));
        }

        [RelayCommand]
        public async Task SetAsDefaultPlayerAsync(PlayerDB player)
        {
            if (player == null)
                return;

            // A row of an older month is not a PlayerDB row - use the matching local player (by KeyName)
            if (player.Id == 0 || _viewMonth != null)
            {
                var dbPlayer = (await _playerService.GetPlayersFromDbAsync()).FirstOrDefault(p => p.KeyName == player.KeyName);
                if (dbPlayer == null)
                    return;

                player = dbPlayer;
            }

            var appData = await _playerService.GetAppDataAsync();
            appData.AppUserPlayerId = player.Id;
            appData.AppUserOldId = player.Id;
            appData.AppUserNewId = player.NewId;
            appData.AppUserKeyName = player.KeyName;
            await _playerService.SaveAppDataAsync(appData);
            _cachedAppData = appData;
            AppDefaultPlayer = await _playerService.GetAppDefaultPlayerAsync(appData);
            _friends.NotifyAppPlayerChanged();
            UpdateAppPlayerLabel();
        }

        // ── Friends ───────────────────────────────────────────────────────

        /// <summary>Long press → "Set as friend" / "Remove friend".</summary>
        [RelayCommand]
        private async Task ToggleFriendAsync(PlayerListItem? item)
        {
            if (item?.Player == null || string.IsNullOrEmpty(item.Player.KeyName))
                return;

            string key = item.Player.KeyName;

            if (_friendKeys.Contains(key))
            {
                await _friends.RemoveAsync(key);
                _friendKeys.Remove(key);
            }
            else
            {
                await _friends.AddAsync(item.Player);
                _friendKeys.Add(key);
            }

            // The same player can be on the list once only, but keep every row of that key in sync
            foreach (var row in _sortedPlayers ?? [])
            {
                if (row.Player.KeyName == key)
                    row.IsFriend = _friendKeys.Contains(key);
            }
        }

        [RelayCommand]
        private async Task ShowFriendsAsync()
        {
            BuildFriendRows(await _friends.GetAllAsync());
            IsFriendsOverlayVisible = true;
        }

        [RelayCommand]
        private void CloseFriends() => IsFriendsOverlayVisible = false;

        /// <summary>App player and friends in the shown month's ranking, best first; not ranked ones at the end.</summary>
        private void BuildFriendRows(List<Friend> friends)
        {
            var byKey = ShownPlayersByKey();
            string meKey = AppPlayerKey();
            var rows = new List<FriendRankingRow>();

            void Add(string key, string fallbackName, bool isMe)
            {
                if (string.IsNullOrEmpty(key) || rows.Any(r => r.KeyName == key))
                    return;

                var row = new FriendRankingRow { KeyName = key, IsMe = isMe, FullName = fallbackName };

                if (byKey.TryGetValue(key, out var p) && p.IsActive)
                {
                    row.IsRanked = true;
                    row.FullName = $"{p.Name} {p.Surname}".Trim();
                    row.Place = p.Place;
                    row.Points = p.Points;
                    row.PointsWithBonus = p.PointsWithBonus;
                    row.PointsChanged = p.PointsChanged;
                }

                rows.Add(row);
            }

            if (AppDefaultPlayer != null)
                Add(meKey, $"{AppDefaultPlayer.Name} {AppDefaultPlayer.Surname}".Trim(), true);

            foreach (var friend in friends)
                Add(friend.KeyName, $"{friend.Name} {friend.Surname}".Trim(), false);

            FriendRows.Clear();

            foreach (var row in rows.OrderByDescending(r => r.IsRanked).ThenByDescending(r => r.PointsWithBonus).ThenBy(r => r.FullName))
                FriendRows.Add(row);

            FriendsTitle = string.IsNullOrEmpty(AppDataLabel) ? "Friends" : $"Friends · {AppDataLabel}";
            FriendsEmptyText = friends.Count == 0 ? "No friends yet - long press a player → Set as friend" : "";
        }

        private string AppPlayerKey() =>
            !string.IsNullOrEmpty(AppDefaultPlayer?.KeyName) ? AppDefaultPlayer.KeyName : _cachedAppData?.AppUserKeyName ?? "";

        private Dictionary<string, PlayerDB> ShownPlayersByKey()
        {
            var byKey = new Dictionary<string, PlayerDB>();

            foreach (var p in _allPlayers ?? [])
            {
                if (!string.IsNullOrEmpty(p.KeyName))
                    byKey.TryAdd(p.KeyName, p);
            }

            return byKey;
        }

        /// <summary>The app player's place and points in the shown month (PlayerDB for the latest month, else that month's ranking).</summary>
        private void UpdateAppPlayerLabel()
        {
            if (AppDefaultPlayer == null)
            {
                AppPlayerLabel = "Not set - long press a player → Set as default player";
                return;
            }

            string name = $"{AppDefaultPlayer.Name} {AppDefaultPlayer.Surname}".Trim();

            AppPlayerLabel = ShownPlayersByKey().TryGetValue(AppPlayerKey(), out var p) && p.IsActive
                ? $"{p.Place}. {name} {p.Points}({p.PointsWithBonus})"
                : $"{name} · not ranked";
        }

        [RelayCommand]
        public async Task GoToTournamentAsync(IGame game)
        {
            if ( game == null)
            {
                return;
            }

            var tournament = await _playerService.GetTournamentAsync(game.TournamentId);
            if (tournament == null)
            {
                return;
            }

            await _navigation.GoToTournamentAsync(game.TournamentId);
        }

        [RelayCommand]
        public async Task GoToGameAsync(IGame game)
        {
            if (game == null)
            {
                return;
            }

            await _navigation.GoToGameAsync(game);
        }

        [RelayCommand]
        private void PlayerSelected(PlayerDB player)
        {
            if (player == null)
                return;

            SelectedPlayer = player;

            var games = _allGames.Where(g => GetGameKey(g) == player.KeyName).OrderByDescending(g => g.TournamentDate);

            FilteredGames = new ObservableCollection<Game>(games);
            Stats = GameStatisticsCalculator.Calculate(FilteredGames);
            IsGamesOverlayVisible = true;
        }

        [RelayCommand]
        private void CloseGamesOverlay()
        {
            IsGamesOverlayVisible = false;
            FilteredGames?.Clear();
        }

        partial void OnSelectedFilterChanged(string value)
        {
            FilterPlayers();
        }

        // ── Year / month pickers ──────────────────────────────────────────

        partial void OnSelectedYearChanged(int value)
        {
            if (value <= 0)
                return;

            int keepMonth = SelectedMonth?.Number ?? 1;
            bool setting = _settingPickers;

            _settingPickers = true;
            try
            {
                FillMonths(value);
                SelectedMonth = Months.FirstOrDefault(m => m.Number == keepMonth) ?? Months.LastOrDefault();
            }
            finally
            {
                _settingPickers = setting;
            }

            if (!_settingPickers)
                ShowPickedMonth();
        }

        partial void OnSelectedMonthChanged(MonthOption? value)
        {
            if (!_settingPickers)
                ShowPickedMonth();
        }

        private void ShowPickedMonth()
        {
            if (SelectedYear > 0 && SelectedMonth is { Number: >= 1 and <= 12 } month)
                _ = ShowMonthAsync(new DateTime(SelectedYear, month.Number, 1));
        }

        /// <summary>Months of the year; in the current year only up to the current month (later ones have no ranking).</summary>
        private void FillMonths(int year)
        {
            var now = DateTime.Now;
            int last = year >= now.Year ? now.Month : 12;
            var names = CultureInfo.CurrentCulture.DateTimeFormat;

            if (Months.Count == last && Months.Count > 0)
                return;

            Months = new ObservableCollection<MonthOption>(
                Enumerable.Range(1, last).Select(m => new MonthOption { Number = m, Name = names.GetMonthName(m) }));
        }

        /// <summary>Shows the given month in the pickers without loading it (the list already shows it).</summary>
        private void SetPickers(int year, int month)
        {
            if (year <= 0 || month is < 1 or > 12)
                return;

            _settingPickers = true;
            try
            {
                if (Years.Count == 0 || Years[0] != DateTime.Now.Year)
                {
                    Years.Clear();

                    for (int y = DateTime.Now.Year; y >= FirstRankingYear; y--)
                        Years.Add(y);
                }

                FillMonths(year);
                SelectedYear = year;
                SelectedMonth = Months.FirstOrDefault(m => m.Number == month) ?? Months.LastOrDefault();
            }
            finally
            {
                _settingPickers = false;
            }
        }

        partial void OnSearchTextChanged(string? value)
        {
            _searchDebounceTimer?.Stop();
            _searchDebounceTimer = new System.Timers.Timer(400);
            _searchDebounceTimer.Elapsed += (s, e) =>
            {
                _searchDebounceTimer?.Stop();
                MainThread.BeginInvokeOnMainThread(ApplySearch);
            };
            _searchDebounceTimer.Start();
        }

        partial void OnSelectedSortChanged(string value)
        {
            SortPlayers();
        }

        public async Task LoadDataAsync()
        {
            // An older month is being viewed - keep it (coming back from a game / tournament page)
            if (_viewMonth == null)
                _allPlayers = await _playerService.GetPlayersFromDbAsync();

            _allGames = await _playerService.GetGamesFromDbAsync();
            _cachedAppData = await _playerService.GetAppDataAsync();
            AppDefaultPlayer = await _playerService.GetAppDefaultPlayerAsync(_cachedAppData);
            _friendKeys = await _friends.GetKeysAsync();

            // Filter, sort and search are kept in this view model; also stay on the same page
            int keepPage = CurrentPage;
            FilterPlayers();
            ShowPage(keepPage);

            await UpdateAppDataLabel();

            // First open: the pickers show the latest stored month (or this month if nothing is stored yet)
            if (SelectedYear == 0)
            {
                bool hasMonth = _cachedAppData.CurrentYear > 0 && _cachedAppData.CurrentMonth is >= 1 and <= 12;
                SetPickers(hasMonth ? _cachedAppData.CurrentYear : DateTime.Now.Year, hasMonth ? _cachedAppData.CurrentMonth : DateTime.Now.Month);
            }

            await EnsureStoredRankingsAsync();
            await UpdateToLatestRankingAsync();
        }

        /// <summary>
        /// The latest stored ranking is compared with the system month - a newer published month is downloaded
        /// (at most every 30 minutes, see IPlayerService.UpdateToLatestRankingAsync) and shown if the latest month was shown.
        /// </summary>
        private async Task UpdateToLatestRankingAsync()
        {
            if (_isLoadingPlayers)
                return;

            bool updated;
            _isLoadingPlayers = true;

            try
            {
                updated = await _tournamentsViewModel.CheckRankingUpdateAsync(DateTime.Now);
            }
            finally
            {
                _isLoadingPlayers = false;
            }

            if (updated)
            {
                _cachedAppData = await _playerService.GetAppDataAsync();
                AppDefaultPlayer = await _playerService.GetAppDefaultPlayerAsync(_cachedAppData);

                bool viewingNewLatest = _viewMonth is DateTime v && v.Year == _cachedAppData.CurrentYear && v.Month == _cachedAppData.CurrentMonth;

                if (_viewMonth == null || viewingNewLatest)
                {
                    _viewMonth = null;
                    _allPlayers = await _playerService.GetPlayersFromDbAsync();
                    FilterPlayers();
                    SetPickers(_cachedAppData.CurrentYear, _cachedAppData.CurrentMonth);
                }

                await UpdateAppDataLabel();
            }

            // A month picked while the update ran
            if (_pendingLoadDate is DateTime next)
            {
                _pendingLoadDate = null;
                await ShowMonthAsync(next);
            }
        }

        // Checked once per app session (an offline attempt is tried again on the next start)
        private bool _storedRankingsChecked;

        /// <summary>
        /// First open: the PlayerDB month and the month before it must be in the MonthRanking table,
        /// so PointsChanged can be shown. Missing ones are downloaded and stored once.
        /// </summary>
        private async Task EnsureStoredRankingsAsync()
        {
            if (_storedRankingsChecked || _isLoadingPlayers)
                return;

            _storedRankingsChecked = true;

            if (await _playerService.AreCurrentRankingsStoredAsync())
                return;

            _isLoadingPlayers = true;
            _progressDialog.Show("Downloading rankings...");

            try
            {
                await _playerService.EnsureCurrentRankingsStoredAsync(status => _progressDialog.Update(status));

                if (_viewMonth == null)
                {
                    _allPlayers = await _playerService.GetPlayersFromDbAsync();
                    FilterPlayers();
                }
            }
            catch (Exception ex)
            {
                _progressDialog.Update($"❌ Error: {ex.Message}");
                await Task.Delay(1000);
            }
            finally
            {
                await _progressDialog.HideAsync();
                _isLoadingPlayers = false;
            }
        }

        /// <summary>
        /// Shows the ranking of the picked month. The PlayerDB month (latest) comes from PlayerDB; any other month is
        /// view only - read from the MonthRanking table (downloaded and stored once if missing, with the month before it
        /// for PointsChanged). PlayerDB and AppData are never changed here; the latest ranking is updated by the
        /// ranking update on the home page.
        /// </summary>
        private async Task ShowMonthAsync(DateTime date)
        {
            if (_isLoadingPlayers)
            {
                // Remember only the newest request; handled when the current load finishes
                _pendingLoadDate = date;
                return;
            }

            _isLoadingPlayers = true;

            try
            {
                _cachedAppData ??= await _playerService.GetAppDataAsync();

                bool isLatest = _cachedAppData.CurrentYear == date.Year && _cachedAppData.CurrentMonth == date.Month;

                if (isLatest)
                {
                    _viewMonth = null;
                    _allPlayers = await _playerService.GetPlayersFromDbAsync();
                }
                else
                {
                    _progressDialog.Show("Loading ranking...");

                    try
                    {
                        var monthPlayers = await _playerService.GetMonthRankingViewAsync(date, status => _progressDialog.Update(status));
                        _viewMonth = new DateTime(date.Year, date.Month, 1);
                        _allPlayers = monthPlayers;

                        if (monthPlayers.Count == 0)
                        {
                            _progressDialog.Update($"No ranking for {date:yyyy-MM}");
                            await Task.Delay(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        _progressDialog.Update($"❌ Error: {ex.Message}");
                        await Task.Delay(1000);
                    }
                    finally
                    {
                        await _progressDialog.HideAsync();
                    }
                }

                FilterPlayers();
                await UpdateAppDataLabel();
            }
            finally
            {
                _isLoadingPlayers = false;
            }

            // A different month was picked while loading → show that one too
            if (_pendingLoadDate is DateTime next)
            {
                _pendingLoadDate = null;

                if (next.Year != date.Year || next.Month != date.Month)
                    await ShowMonthAsync(next);
            }
        }

        private void FilterPlayers()
        {
            _filteredPlayers = PlayerLogic.Filter(_allPlayers, SelectedFilter);
            SortPlayers();
        }

        private void SortPlayers()
        {
            if (_filteredPlayers == null)
                return;

            _sortedPlayers = PlayerLogic.Sort(_filteredPlayers, SelectedSort, SortDescending);

            foreach (var item in _sortedPlayers)
                item.IsFriend = !string.IsNullOrEmpty(item.Player.KeyName) && _friendKeys.Contains(item.Player.KeyName);

            ApplySearch();
        }

        private void ApplySearch()
        {
            if (_sortedPlayers == null)
                return;

            _searchResult = PlayerLogic.Search(_sortedPlayers, SearchText);

            PlayerCountLabel = string.IsNullOrWhiteSpace(SearchText)
                ? $"{_searchResult.Count} players"
                : $"{_searchResult.Count} found";

            // New filter / sort / search → back to the first page
            ShowPage(1);
        }

        private async Task UpdateAppDataLabel()
        {
            var appData = await _playerService.GetAppDataAsync();
            string latest = appData.CurrentYear > 0 && appData.CurrentMonth > 0 ? MonthLabel(appData.CurrentYear, appData.CurrentMonth) : "";

            if (_viewMonth is DateTime viewed)
            {
                AppDataLabel = MonthLabel(viewed.Year, viewed.Month);
                RankingCaption = latest.Length > 0 ? $"Viewing · latest {latest}" : "Viewing";
            }
            else
            {
                AppDataLabel = latest;
                RankingCaption = "Latest ranking";
            }

            UpdateAppPlayerLabel();
        }

        private static string MonthLabel(int year, int month) =>
            $"{year} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)}";

        private static string GetGameKey(Game g)
        {
            var nameKey = NameNormalizer.NormalizeKey(g.Name);
            var surnameKey = NameNormalizer.NormalizeKey(g.Surname);

            var combined = nameKey + surnameKey;

            if (combined == "edgarsberzins")
                return "edgars(r)berzins";

            return combined;
        }
    }
}

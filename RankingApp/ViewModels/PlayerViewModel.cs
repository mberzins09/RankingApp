using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Models;
using RankingApp.Services;
using RankingApp.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RankingApp.ViewModels
{
    public partial class PlayerViewModel(PlayerService playerService) : BaseViewModel
    {
        private readonly PlayerService _playerService = playerService;
        private List<PlayerDB>? _allPlayers = [];
        private List<PlayerDB>? _filteredPlayers = [];
        private List<PlayerListItem>? _sortedPlayers = [];
        private AppData? _cachedAppData;
        private System.Timers.Timer? _searchDebounceTimer;
        private List<Game> _allGames = [];

        [ObservableProperty]
        private ObservableCollection<Game>? filteredGames;

        [ObservableProperty]
        private GameStatistics? stats;

        [ObservableProperty]
        private bool isGamesOverlayVisible;

        [ObservableProperty]
        private PlayerDB? selectedPlayer;

        [ObservableProperty]
        private DateTime minDate = new(2014, 1, 1);

        [ObservableProperty]
        private DateTime maxDate = DateTime.Today.AddDays(10);

        [ObservableProperty]
        private ObservableCollection<PlayerListItem>? players;

        [ObservableProperty]
        private string selectedFilter = "Men";

        [ObservableProperty]
        private DateTime selectedDate = DateTime.UtcNow;

        [ObservableProperty]
        private string? appDataLabel;

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private PlayerDB? appDefaultPlayer;

        [ObservableProperty]
        private string selectedSort = "PointsWithBonus";

        [ObservableProperty]
        private bool remindRankingUpdate;

        public List<string> FilterOptions { get; } = ["Men", "Women", "AllActive", "Inactive", "All"];
        public List<string> SortOptions { get; } = ["PointsWithBonus", "Points", "PointsChanged", "Age"];

        [RelayCommand]
        public async Task SetAsDefaultPlayerAsync(PlayerDB player)
        {
            if (player == null)
                return;

            var appData = await _playerService.GetAppDataAsync();
            appData.AppUserPlayerId = player.Id;
            appData.AppUserOldId = player.Id;
            appData.AppUserNewId = player.NewId;
            appData.AppUserKeyName = player.KeyName;
            await _playerService.SaveAppDataAsync(appData);
            AppDefaultPlayer = await _playerService.GetAppDefaultPlayerAsync(appData);
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

            Data.TournamentId = game.TournamentId;
            Data.GameId = game.Id;

            await Shell.Current.GoToAsync(nameof(TournamentView));
        }

        [RelayCommand]
        public async Task GoToGameAsync(IGame game)
        {
            if (game == null)
            {
                return;
            }

            Data.TournamentId = game.TournamentId;
            Data.GameId = game.Id;

            if (game is Game)
            {
                await Shell.Current.GoToAsync(nameof(GameView));
            }
            else if (game is DoublesGame)
            {
                await Shell.Current.GoToAsync(nameof(DoublesGameView));
            }
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

        partial void OnRemindRankingUpdateChanged(bool value)
        {
            if (_cachedAppData == null)
                return;

            _cachedAppData.RemindRankingUpdate = value;

            _ = _playerService.SaveAppDataAsync(_cachedAppData);
        }

        partial void OnSelectedFilterChanged(string value)
        {
            FilterPlayers();
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            if (_cachedAppData == null)
            {
                _ = LoadPlayersFromApiAsync(value);
                return;
            }
            
            if (_cachedAppData.CurrentYear == value.Year &&
                _cachedAppData.CurrentMonth == value.Month)
            {
                return;
            }

            _ = LoadPlayersFromApiAsync(value);
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
            _allPlayers = await _playerService.GetPlayersFromDbAsync();
            _allGames = await _playerService.GetGamesFromDbAsync();
            _cachedAppData = await _playerService.GetAppDataAsync();
            RemindRankingUpdate = _cachedAppData.RemindRankingUpdate;
            AppDefaultPlayer = await _playerService.GetAppDefaultPlayerAsync(_cachedAppData);
            FilterPlayers();
            await UpdateAppDataLabel();
        }

        public async Task LoadPlayersFromApiAsync(DateTime? date = null)
        {
            var popup = new ProcessingPopup { Message = "Loading players..." };
            _ = Application.Current.MainPage.ShowPopupAsync(popup);

            try
            {
                _allPlayers = await _playerService.LoadPlayersFromApiOrDbAsync(date, status => MainThread.BeginInvokeOnMainThread(() => popup.Message = status));
                _cachedAppData = await _playerService.GetAppDataAsync();
                FilterPlayers();
                await UpdateAppDataLabel();
                MainThread.BeginInvokeOnMainThread(() => popup.Message = "✅ Players loaded successfully!");
                AppDefaultPlayer = await _playerService.GetAppDefaultPlayerAsync(_cachedAppData);
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    popup.Message = $"❌ Error: {ex.Message}";
                });
            }
            finally
            {
                await Task.Delay(1000);
                await MainThread.InvokeOnMainThreadAsync(async () => await popup.CloseAsync());
            }
        }

        private void FilterPlayers()
        {
            IEnumerable<PlayerDB> filtered = _allPlayers;
            switch (SelectedFilter)
            {
                case "Men":
                    filtered = _allPlayers.Where(player => player.Gender == "male" && player.IsActive);
                    break;
                case "Women":
                    filtered = _allPlayers.Where(player => player.Gender == "female" && player.IsActive);
                    break;
                case "Inactive":
                    filtered = _allPlayers.Where(player => !player.IsActive);
                    break;
                case "All":
                    filtered = _allPlayers;
                    break;
                case "AllActive":
                default:
                    filtered = _allPlayers.Where(player => player.IsActive);
                    break;
            }

            _filteredPlayers = filtered.ToList();

            SortPlayers();
        }

        private void SortPlayers()
        {
            if (_filteredPlayers == null)
                return;

            IEnumerable<PlayerDB> sorted = SelectedSort switch
            {
                "Points" => _filteredPlayers.OrderByDescending(p => p.Points),
                "PointsChanged" => _filteredPlayers.OrderByDescending(p => p.PointsChanged),
                "Age" => _filteredPlayers.OrderByDescending(p => p.Age),
                _ => _filteredPlayers.OrderByDescending(p => p.PointsWithBonus),
            };

            var list = sorted.ToList();

            _sortedPlayers = new List<PlayerListItem>();

            for (int i = 0; i < list.Count; i++)
            {
                _sortedPlayers.Add(new PlayerListItem
                {
                    Player = list[i],
                    Place = i + 1
                });
            }

            ApplySearch();
        }

        private void ApplySearch()
        {
            if (_sortedPlayers == null)
                return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Players = new ObservableCollection<PlayerListItem>(_sortedPlayers);
                return;
            }

            var input = SearchText.Trim();
            var normalizedInput = NameNormalizer.NormalizeKey(input);

            IEnumerable<PlayerListItem> result = _sortedPlayers;

            var rangePattern = @"^\s*(\d+)\s*[-.]{1,2}\s*(\d+)\s*$";
            var greaterThanPattern = @"^>\s*(\d+)$";
            var greaterOrEqualPattern = @"^>=\s*(\d+)$";
            var lessThanPattern = @"^<\s*(\d+)$";
            var lessOrEqualPattern = @"^<=\s*(\d+)$";

            switch (input)
            {
                case var s when Regex.IsMatch(s, rangePattern):
                    var match = Regex.Match(s, rangePattern);

                    int start = int.Parse(match.Groups[1].Value);
                    int end = int.Parse(match.Groups[2].Value);

                    result = result.Where(p => p.Place >= start && p.Place <= end);
                    break;

                case var s when Regex.IsMatch(s, greaterOrEqualPattern):
                    int val = int.Parse(Regex.Match(s, greaterOrEqualPattern).Groups[1].Value);
                    result = result.Where(p => p.Place >= val);
                    break;

                case var s when Regex.IsMatch(s, greaterThanPattern):
                    val = int.Parse(Regex.Match(s, greaterThanPattern).Groups[1].Value);
                    result = result.Where(p => p.Place > val);
                    break;

                case var s when Regex.IsMatch(s, lessOrEqualPattern):
                    val = int.Parse(Regex.Match(s, lessOrEqualPattern).Groups[1].Value);
                    result = result.Where(p => p.Place <= val);
                    break;

                case var s when Regex.IsMatch(s, lessThanPattern):
                    val = int.Parse(Regex.Match(s, lessThanPattern).Groups[1].Value);
                    result = result.Where(p => p.Place < val);
                    break;

                default:
                    result = result.Where(p =>p.Player.KeyName.Contains(normalizedInput));
                    break;
            }

            Players = new ObservableCollection<PlayerListItem>(result);
        }

        private async Task UpdateAppDataLabel()
        {
            var appData = await _playerService.GetAppDataAsync();
            if (appData.CurrentYear > 0 && appData.CurrentMonth > 0)
            {
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(appData.CurrentMonth);
                AppDataLabel = $"{appData.CurrentYear} {monthName}";
            }
            else
            {
                AppDataLabel = string.Empty;
            }
        }

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

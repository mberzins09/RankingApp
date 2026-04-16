using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using RankingApp.Core.ViewModels.HelperClasses;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.Core.ViewModels
{
    public partial class PlayerViewModel(IPlayerService playerService, INavigationService navigation, IProgressDialogService progressDialog) : BaseViewModel
    {
        private readonly IProgressDialogService _progressDialog = progressDialog;
        private readonly INavigationService _navigation = navigation;
        private readonly IPlayerService _playerService = playerService;
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

            await _navigation.GoToAsync("TournamentView");
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
                await _navigation.GoToAsync("GameView");
            }
            else if (game is DoublesGame)
            {
                await _navigation.GoToAsync("DoublesGameView");
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
            _progressDialog.Show("Loading players...");

            try
            {
                _allPlayers = await _playerService.LoadPlayersFromApiOrDbAsync(date, status => _progressDialog.Update(status));
                _cachedAppData = await _playerService.GetAppDataAsync();
                FilterPlayers();
                await UpdateAppDataLabel();
                _progressDialog.Update("✅ Players loaded successfully!");
                AppDefaultPlayer = await _playerService.GetAppDefaultPlayerAsync(_cachedAppData);
            }
            catch (Exception ex)
            {
                _progressDialog.Update($"❌ Error: {ex.Message}");
            }
            finally
            {
                await Task.Delay(1000);
                await _progressDialog.HideAsync();
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

            _sortedPlayers = PlayerLogic.Sort(_filteredPlayers, SelectedSort);
            ApplySearch();
        }

        private void ApplySearch()
        {
            if (_sortedPlayers == null)
                return;

            var result = PlayerLogic.Search(_sortedPlayers, SearchText);
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

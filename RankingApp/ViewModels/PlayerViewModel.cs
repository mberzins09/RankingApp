using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Data_Storage;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.ViewModels
{
    public partial class PlayerViewModel(DatabaseService database, PlayerReposotoryWithDate repositoryWithDate) : BaseViewModel
    {
        private readonly DatabaseService _database = database;
        private readonly PlayerReposotoryWithDate _repositoryWithDate = repositoryWithDate;

        private List<PlayerDB> _allPlayers = [];

        private List<PlayerDB> _filteredPlayers = [];

        [ObservableProperty]
        private DateTime minDate = new(2014, 1, 1);

        [ObservableProperty]
        private DateTime maxDate = DateTime.Today.AddDays(10);

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private string selectedFilter = "All"; // "Mens", "Womens", "All", "Inactive"

        [ObservableProperty]
        private DateTime selectedDate = DateTime.UtcNow;

        [ObservableProperty]
        private string? appDataLabel;

        [ObservableProperty]
        private string? searchText;

        public List<string> FilterOptions { get; } = ["Mens", "Womens", "All", "Inactive"];

        [RelayCommand]
        public async Task SetAsDefaultPlayerAsync(PlayerDB player)
        {
            if (player == null)
                return;

            var appData = await _database.GetAppDataAsync();
            appData.AppUserPlayerId = player.Id;
            await _database.SaveAppDataAsync(appData);
        }

        partial void OnSelectedFilterChanged(string value)
        {
            FilterPlayers();
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            _ = LoadPlayersFromApiAsync(value);
        }

        partial void OnSearchTextChanged(string? value)
        {
            ApplySearch();
        }

        public async Task LoadDataAsync()
        {
            _allPlayers = (await _database.GetPlayersAsync()).OrderByDescending(x => x.PointsWithBonus).ToList();
            FilterPlayers();
            await UpdateAppDataLabel();
        }

        public async Task LoadPlayersFromApiAsync(DateTime? date = null)
        {
            DateTime now = date ?? DateTime.UtcNow;
            string currentDateString = now.ToString("yyyy-MM");
            string previousDateString = now.AddMonths(-1).ToString("yyyy-MM");

            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(currentDateString);
            if (apiPlayers == null || apiPlayers.Count == 0)
            {
                apiPlayers = await _repositoryWithDate.GetPlayersAsync(previousDateString);
                if (apiPlayers != null && apiPlayers.Count > 0)
                {
                    await UpdateAppDataWithDate(previousDateString);
                }
            }
            else
            {
                await UpdateAppDataWithDate(currentDateString);
            }

            if (apiPlayers != null && apiPlayers.Count > 0)
            {
                var dbPlayers = await _database.GetPlayersAsync();
                var apiPlayerIds = new HashSet<int>(apiPlayers.Select(p => p.Id));
                var missingPlayers = dbPlayers.Where(dbPlayer => !apiPlayerIds.Contains(dbPlayer.Id)).ToList();
                foreach (var player in missingPlayers)
                {
                    await _database.UpdatePlayerAsync(player);
                }
                await _database.UpsertPlayersAsync(apiPlayers);
                _allPlayers = apiPlayers.OrderByDescending(x => x.PointsWithBonus).ToList();
            }
            else
            {
                _allPlayers = (await _database.GetPlayersAsync()).OrderByDescending(x => x.PointsWithBonus).ToList();
            }

            FilterPlayers();
            await UpdateAppDataLabel();

            await Application.Current.MainPage.DisplayAlert("Success", "Players loaded successfully.", "OK");
        }

        private void FilterPlayers()
        {
            IEnumerable<PlayerDB> filtered = _allPlayers;
            switch (SelectedFilter)
            {
                case "Mens":
                    filtered = _allPlayers.Where(x => x.Gender == "male" && x.Place < 6000).OrderBy(x => x.Place);
                    break;
                case "Womens":
                    filtered = _allPlayers.Where(x => x.Gender == "female" && x.Place < 6000).OrderBy(x => x.Place);
                    break;
                case "Inactive":
                    filtered = _allPlayers.Where(x => x.OverallPlace != 0 && x.OverallPlace > 5999)
                                          .OrderByDescending(x => x.PointsWithBonus);
                    break;
                case "All":
                default:
                    filtered = _allPlayers.Where(x => x.OverallPlace != 0 && x.OverallPlace < 6000)
                                          .OrderBy(x => x.OverallPlace);
                    break;
            }

            _filteredPlayers = filtered.ToList();
            ApplySearch();
        }

        private void ApplySearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Players = new ObservableCollection<PlayerDB>(_filteredPlayers);
                return;
            }

            var searched = _filteredPlayers.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Name) && x.Name.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Surname) && x.Surname.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Place.ToString()) && x.Place.ToString().StartsWith(SearchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            Players = new ObservableCollection<PlayerDB>(searched);
        }

        private async Task UpdateAppDataLabel()
        {
            var appData = await _database.GetAppDataAsync();
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

        public void SearchPlayers(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                FilterPlayers();
                return;
            }

            var searched = _allPlayers.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Name) && x.Name.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Surname) && x.Surname.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Place.ToString()) && x.Place.ToString().StartsWith(filterText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            Players = new ObservableCollection<PlayerDB>(searched);
        }
        public async Task GetLatestRankingsAsync()
        {
            var now = DateTime.UtcNow;
            string currentDateString = now.ToString("yyyy-MM");
            string previousDateString = now.AddMonths(-1).ToString("yyyy-MM");

            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(currentDateString);
            if (apiPlayers == null || apiPlayers.Count == 0)
            {
                apiPlayers = await _repositoryWithDate.GetPlayersAsync(previousDateString);
                if (apiPlayers != null && apiPlayers.Count > 0)
                {
                    await UpdateAppDataWithDate(previousDateString);
                }
            }
            else
            {
                await UpdateAppDataWithDate(currentDateString);
            }

            if (apiPlayers != null && apiPlayers.Count > 0)
            {
                List<PlayerDB> dbPlayers = await _database.GetPlayersAsync();
                var apiPlayerIds = new HashSet<int>(apiPlayers.Select(p => p.Id));
                var missingPlayers = dbPlayers.Where(dbPlayer => !apiPlayerIds.Contains(dbPlayer.Id)).ToList();
                foreach (var player in missingPlayers)
                {
                    await _database.UpdatePlayerAsync(player);
                }

                await _database.UpsertPlayersAsync(apiPlayers);
            }
        }

        public async Task FillDatabaseWithOldRankings()
        {
            int startYear = 2014;
            int currentYear = DateTime.UtcNow.Year;
            int currentMonth = DateTime.UtcNow.Month;
            int endYear = (currentMonth == 1) ? currentYear - 1 : currentYear;
            var tasks = new List<Task<List<(PlayerDB Player, int SyncYear)>>>();

            for (int year = startYear; year <= endYear; year++)
            {
                string dateString = $"{year}-01";
                var task = _repositoryWithDate.GetPlayersAsync(dateString)
            .ContinueWith(t =>
            {
                var players = t.Result;
                return players.Select(p => (Player: p, SyncYear: year)).ToList();
            });

                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            var allPlayerTuples = results.SelectMany(x => x).ToList();

            var distinctPlayers = allPlayerTuples
                                    .OrderBy(x => x.SyncYear)
                                    .GroupBy(x => x.Player.Id)
                                    .Select(g => g.Last().Player)
                                    .ToList();

            await _database.UpsertPlayersAsync(distinctPlayers);

            await GetLatestRankingsAsync();
        }

        private async Task UpdateAppDataWithDate(string dateString)
        {
            var appData = await _database.GetAppDataAsync();
            if (DateTime.TryParseExact(dateString, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                appData.CurrentYear = dt.Year;
                appData.CurrentMonth = dt.Month;
                await _database.SaveAppDataAsync(appData);
            }
        }
    }
}

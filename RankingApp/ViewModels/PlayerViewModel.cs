using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.ViewModels
{
    public partial class PlayerViewModel(PlayerService playerService) : BaseViewModel
    {
        private readonly PlayerService _playerService = playerService;
        private List<PlayerDB>? _allPlayers = [];
        private List<PlayerDB>? _filteredPlayers = [];
        private AppData? _cachedAppData;

        [ObservableProperty]
        private DateTime minDate = new(2014, 1, 1);

        [ObservableProperty]
        private DateTime maxDate = DateTime.Today.AddDays(10);

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private string selectedFilter = "All";

        [ObservableProperty]
        private DateTime selectedDate = DateTime.UtcNow;

        [ObservableProperty]
        private string? appDataLabel;

        [ObservableProperty]
        private string? searchText;

        public List<string> FilterOptions { get; } = ["Men", "Women", "All", "Inactive"];

        [RelayCommand]
        public async Task SetAsDefaultPlayerAsync(PlayerDB player)
        {
            if (player == null)
                return;

            var appData = await _playerService.GetAppDataAsync();
            appData.AppUserPlayerId = player.Id;
            await _playerService.SaveAppDataAsync(appData);
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
            ApplySearch();
        }

        public async Task LoadDataAsync()
        {
            _allPlayers = await _playerService.GetPlayersFromDbAsync();
            _cachedAppData = await _playerService.GetAppDataAsync();
            FilterPlayers();
            await UpdateAppDataLabel();
        }

        public async Task LoadPlayersFromApiAsync(DateTime? date = null)
        {
            _allPlayers = await _playerService.LoadPlayersFromApiOrDbAsync(date);
            _cachedAppData = await _playerService.GetAppDataAsync();

            FilterPlayers();
            await UpdateAppDataLabel();

            await Application.Current.MainPage.DisplayAlert("Success", "Players loaded successfully.", "OK");
        }

        private void FilterPlayers()
        {
            IEnumerable<PlayerDB> filtered = _allPlayers;
            switch (SelectedFilter)
            {
                case "Men":
                    filtered = _allPlayers.Where(x => x.Gender == "male" && x.Place < 6000).OrderBy(x => x.Place);
                    break;
                case "Women":
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
    }
}

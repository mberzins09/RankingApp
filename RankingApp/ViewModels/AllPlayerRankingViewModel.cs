using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.ViewModels
{
    public partial class AllPlayerRankingViewModel(DatabaseService database) : BaseViewModel
    {
        private readonly DatabaseService _database = database;
        private List<PlayerDB> _allPlayers = [];
        private List<PlayerDB> _filteredPlayers = [];

        //[ObservableProperty]
        //private DateTime minDate = new(2014, 1, 1);

        //[ObservableProperty]
        //private DateTime maxDate = DateTime.Today.AddDays(10);

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private string selectedFilter = "All";

        //[ObservableProperty]
        //private DateTime selectedDate = DateTime.UtcNow;

        //[ObservableProperty]
        //private string? appDataLabel;

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

        //partial void OnSelectedDateChanged(DateTime value)
        //{
        //    //Todo: Implement logic to load players from API based on the selected date
        //}

        partial void OnSearchTextChanged(string? value)
        {
            ApplySearch();
        }

        public async Task LoadDataAsync()
        {
            _allPlayers = await _database.GetPlayersAsync();
            FilterPlayers();
            //await UpdateAppDataLabel();
        }

        private void FilterPlayers()
        {
            List<PlayerDB> filtered = _allPlayers;
            filtered = SelectedFilter switch
            {
                "Mens" => _allPlayers.Where(x => x.Gender == "male" && x.Place < 6000).OrderBy(x => x.Place).ToList(),
                "Womens" => _allPlayers.Where(x => x.Gender == "female" && x.Place < 6000).OrderBy(x => x.Place).ToList(),
                "Inactive" => _allPlayers.Where(x => x.OverallPlace != 0 && x.OverallPlace > 5999)
                                                          .OrderByDescending(x => x.PointsWithBonus).ToList(),
                _ => _allPlayers.Where(x => x.OverallPlace != 0 && x.OverallPlace < 6000)
                                                          .OrderBy(x => x.OverallPlace).ToList(),
            };
            _filteredPlayers = filtered;
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

        //private async Task UpdateAppDataLabel()
        //{
        //    var appData = await _database.GetAppDataAsync();
        //    if (appData.CurrentYear > 0 && appData.CurrentMonth > 0)
        //    {
        //        var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(appData.CurrentMonth);
        //        AppDataLabel = $"{appData.CurrentYear} {monthName}";
        //    }
        //    else
        //    {
        //        AppDataLabel = string.Empty;
        //    }
        //}
    }
}

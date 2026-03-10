using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Data_Storage;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels
{
    public partial class DoublesGameViewModel(DatabaseService databaseService, PlayerReposotoryWithDate playerReposotory) : BaseViewModel, ISaveBeforeNavigate
    {
        private readonly DatabaseService _databaseService = databaseService;
        private readonly PlayerReposotoryWithDate _playerReposotory = playerReposotory;

        [ObservableProperty]
        private DoublesGame? oneDoublesGame;

        [ObservableProperty]
        private bool isSearchDisabled;

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private PlayerDB? selectedPlayer;

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private string? selectionMode; // "MyPartner", "Opponent1", "Opponent2"

        private List<PlayerDB> _allPlayers = [];
        private List<PlayerDB> _tournamentRankingPlayers = [];

        public ObservableCollection<int> SetsOptions { get; } = [0, 1, 2, 3, 4];

        public List<string> SelectionOptions { get; } = ["MyPartner", "Opponent 1", "Opponent 2"];

        public async Task<bool> SaveBeforeNavigateAsync()
        {
            await SaveDoublesGameAsync();
            return true;
        }

        partial void OnSelectionModeChanged(string? value)
        {
            // Optionally clear selection when mode changes
            SelectedPlayer = null;
        }

        partial void OnSearchTextChanged(string? value)
        {
            FilterPlayers(value ?? string.Empty);
        }

        partial void OnSelectedPlayerChanged(PlayerDB? value)
        {
            SearchText = String.Empty;
            if (value is null || OneDoublesGame is null || string.IsNullOrEmpty(SelectionMode))
                return;

            switch (SelectionMode)
            {
                case "MyPartner":
                    OneDoublesGame.MyPartnerName = value.Name;
                    OneDoublesGame.MyPartnerSurname = value.Surname;
                    OneDoublesGame.MyPartnerPoints = value.Points;
                    OneDoublesGame.MyPartnerAge = value.Age;
                    OneDoublesGame.MyPartnerPointsWithBonus = value.PointsWithBonus;
                    OneDoublesGame.MyPartnerPlace = value.Place;
                    break;
                case "Opponent 1":
                    OneDoublesGame.Opponent1Name = value.Name;
                    OneDoublesGame.Opponent1Surname = value.Surname;
                    OneDoublesGame.Opponent1Points = value.Points;
                    OneDoublesGame.Opponent1Age = value.Age;
                    OneDoublesGame.Opponent1Place = value.Place;
                    OneDoublesGame.Opponent1PointsWithBonus = value.PointsWithBonus;
                    break;
                case "Opponent 2":
                    OneDoublesGame.Opponent2Name = value.Name;
                    OneDoublesGame.Opponent2Surname = value.Surname;
                    OneDoublesGame.Opponent2Points = value.Points;
                    OneDoublesGame.Opponent2Age = value.Age;
                    OneDoublesGame.Opponent2PointsWithBonus = value.PointsWithBonus;
                    OneDoublesGame.Opponent2Place = value.Place;
                    break;
            }
        }

        public async Task LoadDataAsync()
        {
            IsSearchDisabled = true;
            OneDoublesGame = await _databaseService.GetByIdAsync<DoublesGame>(Data.GameId);
            var tournament = await _databaseService.GetByIdAsync<Tournament>(OneDoublesGame.TournamentId);
            OneDoublesGame.GameCoefficient = tournament.Coefficient;
            var appData = await _databaseService.GetAppDataAsync();
            var dbPlayers = await _databaseService.GetAllRecordsAsync<PlayerDB>();

            _allPlayers = dbPlayers.Where(x => x.Id != tournament.TournamentPlayerId && x.Place != 0).OrderBy(x => x.OverallPlace).ToList();
            SelectionMode = SelectionOptions[0];
            int tournamentYear = tournament.Date.Year;
            int tournamentMonth = tournament.Date.Month;
            bool sameRankingMonth = (tournamentYear == appData.CurrentYear && tournamentMonth == appData.CurrentMonth);
            if (!sameRankingMonth)
            {
                string dateString = tournament.Date.ToString("yyyy-MM");
                bool isOldAPIBody = tournamentYear < 2025 || (tournamentYear == 2025 && tournamentMonth <= 10);

                var apiPlayers = await _playerReposotory.GetPlayersAsync(dateString, isOldAPIBody);

                _tournamentRankingPlayers = apiPlayers ?? [];

                var inactivePlayers = _allPlayers.Where(x => x.Place == 6000).ToList();
                var combined = _tournamentRankingPlayers.Concat(inactivePlayers)
                                                        .OrderBy(x => x.OverallPlace)
                                                        .ToList();

                Players = new ObservableCollection<PlayerDB>(combined);
            }
            else
            {
                Players = new ObservableCollection<PlayerDB>(_allPlayers);
            }

            await _databaseService.SaveAsync<DoublesGame>(OneDoublesGame);
            IsSearchDisabled = false;
        }

        public async Task SaveDoublesGameAsync()
        {
            if (OneDoublesGame != null)
                await _databaseService.SaveAsync<DoublesGame>(OneDoublesGame);
        }

        public void FilterPlayers(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Players = new ObservableCollection<PlayerDB>(_allPlayers);
                return;
            }

            var filtered = _allPlayers.Where(x => (!string.IsNullOrWhiteSpace(x.Name) && x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) && x.Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Place.ToString()) && x.Place.ToString().StartsWith(searchText, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

            Players = new ObservableCollection<PlayerDB>(filtered);
        }
    }
}

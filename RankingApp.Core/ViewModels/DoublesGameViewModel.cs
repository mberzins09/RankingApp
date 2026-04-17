using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    public partial class DoublesGameViewModel(IDatabaseService databaseService, IMonthPlayersService monthPlayers) : BaseViewModel, ISaveBeforeNavigate
    {
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly IMonthPlayersService _monthPlayers = monthPlayers;

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

            if (OneDoublesGame == null)
                return;

            var tournament = await _databaseService.GetByIdAsync<Tournament>(OneDoublesGame.TournamentId);

            if (tournament != null)
            {
                OneDoublesGame.GameCoefficient = tournament.Coefficient;
            }

            var appData = await _databaseService.GetAppDataAsync();
            var dbPlayers = await _databaseService.GetAllRecordsAsync<PlayerDB>();

            if (tournament == null)
                return;

            _allPlayers = await _monthPlayers.GetPlayersForTournamentAsync(tournament, appData, dbPlayers);
            Players = new ObservableCollection<PlayerDB>(_allPlayers);

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

            var normalizedInput = NameNormalizer.NormalizeKey(searchText);
            var filtered = _allPlayers.Where(x => x.KeyName.Contains(normalizedInput)).ToList();
            Players = new ObservableCollection<PlayerDB>(filtered);
        }
    }
}

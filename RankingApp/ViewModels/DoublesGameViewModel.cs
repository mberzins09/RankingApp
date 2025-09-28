using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels
{
    public partial class DoublesGameViewModel(DatabaseService databaseService) : BaseViewModel
    {
        private readonly DatabaseService _databaseService = databaseService;

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

        public ObservableCollection<int> SetsOptions { get; } = [0, 1, 2, 3, 4];

        public List<string> SelectionOptions { get; } = ["MyPartner", "Opponent 1", "Opponent 2"];

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
            OneDoublesGame = await _databaseService.GetDoublesGameAsync(Data.GameId);
            var players = await _databaseService.GetPlayersAsync();
            var tournament = await _databaseService.GetTournamentAsync(OneDoublesGame.TournamentId);
            OneDoublesGame.GameCoefficient = tournament.Coefficient;
            _allPlayers = players.Where(x => x.Id != tournament.TournamentPlayerId).OrderByDescending(x => x.PointsWithBonus).ToList();
            Players = new ObservableCollection<PlayerDB>(_allPlayers);
            SelectionMode = SelectionOptions[0];
            await _databaseService.SaveDoublesGameAsync(OneDoublesGame);
            IsSearchDisabled = false;
        }

        public async Task SaveDoublesGameAsync()
        {
            if (OneDoublesGame != null)
                await _databaseService.SaveDoublesGameAsync(OneDoublesGame);
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

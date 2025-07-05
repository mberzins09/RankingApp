using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Services;

namespace RankingApp.ViewModels
{
    public class GameViewModel(DatabaseService databaseService) : BaseViewModel
    {
        private readonly DatabaseService _databaseService = databaseService;
        public ObservableCollection<PlayerDB>? Players { get; set; }
        public List<PlayerDB> PlayerList { get; set; } = new List<PlayerDB>();
        public ObservableCollection<int> SetsOptions { get; } = new ObservableCollection<int> { 0, 1, 2, 3, 4 };
        public bool IsOpponentForeign { get; set; }
        private Game _game;
        public Game Game
        {
            get => _game;
            set
            {
                if (_game != value)
                {
                    _game = value;
                    OnPropertyChanged();
                }
            }
        }

        public async Task LoadDataAsync()
        {
            Game = await _databaseService.GetGameAsync(Data.GameId);
            var tournament = await _databaseService.GetTournamentAsync(Game.TournamentId);
            var players = await _databaseService.GetPlayersAsync();
            PlayerList = players
                .Where(x =>
                    x.Id != tournament.TournamentPlayerId)
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            Players = new ObservableCollection<PlayerDB>(PlayerList);

            OnPropertyChanged(nameof(Players));
        }

        public async Task SaveGameAsync()
        {
            await _databaseService.SaveGameAsync(Game);
        }

        public async Task<List<PlayerDB>> GetPlayers()
        {
            var tournament = await _databaseService.GetTournamentAsync(Game.TournamentId);
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x =>
                    x.Id != tournament.TournamentPlayerId)
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            return list;
        }
        public List<PlayerDB> SearchPlayers(List<PlayerDB> players, string filterText)
        {
            var searchedPlayers = players
                .Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                             x.Name.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) &&
                             x.Surname.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Place.ToString()) &&
                             x.Place.ToString().StartsWith(filterText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return searchedPlayers;
        }
    }

    public static class BoolValues
    {
        public static readonly bool[] Values = { true, false };
    }
}

using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Services;

namespace RankingApp.ViewModels
{
    public class GameViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly RankingAppsDatabase _database;
        public ObservableCollection<PlayerDB> Players { get; set; }
        public List<PlayerDB> PlayerList { get; set; } = new List<PlayerDB>();
        public string OpponentName { get; set; }
        public string OpponentSurname { get; set; }
        public int GameOpponentPoints { get; set; }
        public int MySets { get; set; }
        public int OpponentSets { get; set; }
        public int MyPoints { get; set; }
        public PlayerDB? OpponentDb { get; set; }

        public GameViewModel(DatabaseService databaseService, RankingAppsDatabase database)
        {
            _database = database;
            _databaseService = databaseService;
            MyPoints = Data.TournamentPlayerPoints == null ? 0 : Data.TournamentPlayerPoints;
            _ = LoadPlayersAsync();
        }

        public async Task SaveGameAsync()
        {
            var gameId = Data.GameId;
            var game = await _database.GetGameAsync(gameId);
            game.TournamentId = Data.TournamentId;
            game.GameCoefficient = Data.Coefficient;
            game.TournamentName = Data.TournamentName;
            game.TournamentDate = Data.TournamentDate;
            game.MyPoints = Data.TournamentPlayerPoints;
            game.Name = OpponentName;
            game.Surname = OpponentSurname;
            game.OpponentPoints = GameOpponentPoints;
            game.MySets = MySets;
            game.OpponentSets = OpponentSets;
            game.MyName = Data.TournamentPlayerName;
            game.MySurname = Data.TournamentPlayerSurname;

            await _database.SaveGameAsync(game);
        }

        public async Task LoadPlayersAsync()
        {
            var players = await _databaseService.GetPlayersAsync();
            PlayerList = players
                .Where(x=>
                    x.Name != Data.TournamentPlayerName && 
                    x.Surname != Data.TournamentPlayerSurname &&
                    x.Points != Data.TournamentPlayerPoints)
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();

            Players = new ObservableCollection<PlayerDB>(PlayerList);

            OnPropertyChanged(nameof(Players));
        }

        public async Task<List<PlayerDB>> GetPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players
                .Where(x =>
                    x.Name != Data.TournamentPlayerName &&
                    x.Surname != Data.TournamentPlayerSurname &&
                    x.Points != Data.TournamentPlayerPoints)
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
                             x.Surname.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return searchedPlayers;
        }
    }
}

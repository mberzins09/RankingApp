using RankingApp.Services;
using System.Collections.ObjectModel;
using RankingApp.Models;

namespace RankingApp.ViewModels
{
    public class AddTournamentViewModel(DatabaseService databaseService, RankingAppsDatabase database) : BaseViewModel
    {
        private readonly DatabaseService _databaseService = databaseService;
        private readonly RankingAppsDatabase _database = database;
        public required ObservableCollection<PlayerDB> Players { get; set; }
        public List<PlayerDB> PlayerList { get; set; } = new List<PlayerDB>();
        public required Tournament Tournament { get; set; }

        public async Task LoadDataAsync()
        {
            Tournament = await _database.GetTournamentAsync(Data.TournamentId);
            var players = await _databaseService.GetPlayersAsync();
            PlayerList = players.OrderByDescending(x => x.PointsWithBonus).ToList();

            Players = new ObservableCollection<PlayerDB>(PlayerList);

            OnPropertyChanged(nameof(Players));
        }

        public async Task<List<PlayerDB>> GetPlayers()
        {
            var players = await _databaseService.GetPlayersAsync();
            var list = players.OrderByDescending(x => x.PointsWithBonus).ToList();

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

        public async Task EditDate(DateTime date)
        {
            var games = await _database.GetGamesAsync();
            var dateGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in dateGames)
            {
                game.TournamentDate = date;

                await _database.SaveGameAsync(game);
            }

            await _database.SaveTournamentAsync(Tournament);
        }

        public async Task EditCoefficient(string coef)
        {
            var games = await _database.GetGamesAsync();
            var coefGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in coefGames)
            {
                game.GameCoefficient = coef;

                await _database.SaveGameAsync(game);
            }

            await _database.SaveTournamentAsync(Tournament);
        }

        public async Task EditTournamentName(string name)
        {
            var games = await _database.GetGamesAsync();
            var nameGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in nameGames)
            {
                game.TournamentName = name;

                await _database.SaveGameAsync(game);
            }

            await _database.SaveTournamentAsync(Tournament);
        }

        public async Task EditMe(string name, string surname, int points)
        {
            var games = await _database.GetGamesAsync();
            var nameGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in nameGames)
            {
                game.MyName = name;
                game.MySurname = surname;
                game.MyPoints = points;

                await _database.SaveGameAsync(game);
            }

            await _database.SaveTournamentAsync(Tournament);
        }
    }
}

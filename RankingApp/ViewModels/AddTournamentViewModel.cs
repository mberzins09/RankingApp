using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RankingApp.ViewModels
{
    public class AddTournamentViewModel(DatabaseService databaseService) : BaseViewModel
    {
        private readonly DatabaseService _databaseService = databaseService;
        public required ObservableCollection<PlayerDB> Players { get; set; }
        public List<PlayerDB> PlayerList { get; set; } = new List<PlayerDB>();
        private Tournament _tournament;
        public required Tournament Tournament {
            get => _tournament;
            set
            {
                if (_tournament != value)
                {
                    _tournament = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public List<string> CoefficientOptions { get; } = new()
    {
        "0", "0.25", "0.5", "1", "1.5", "2", "4"
    };

        public async Task LoadDataAsync()
        {
            Tournament = await _databaseService.GetTournamentAsync(Data.TournamentId);
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
            var games = await _databaseService.GetGamesAsync();
            var dateGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in dateGames)
            {
                game.TournamentDate = date;

                await _databaseService.SaveGameAsync(game);
            }

            await _databaseService.SaveTournamentAsync(Tournament);
        }

        public async Task EditCoefficient(string coef)
        {
            var games = await _databaseService.GetGamesAsync();
            var coefGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in coefGames)
            {
                game.GameCoefficient = coef;

                await _databaseService.SaveGameAsync(game);
            }

            await _databaseService.SaveTournamentAsync(Tournament);
        }

        public async Task EditTournamentName(string name)
        {
            var games = await _databaseService.GetGamesAsync();
            var nameGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in nameGames)
            {
                game.TournamentName = name;

                await _databaseService.SaveGameAsync(game);
            }

            await _databaseService.SaveTournamentAsync(Tournament);
        }

        public async Task EditMe(string name, string surname, int points)
        {
            var games = await _databaseService.GetGamesAsync();
            var nameGames = games.Where(x => x.TournamentId == Data.TournamentId).ToList();
            foreach (var game in nameGames)
            {
                game.MyName = name;
                game.MySurname = surname;
                game.MyPoints = points;

                await _databaseService.SaveGameAsync(game);
            }

            await _databaseService.SaveTournamentAsync(Tournament);
        }
    }
}

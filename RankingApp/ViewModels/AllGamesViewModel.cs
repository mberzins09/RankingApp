using System.Collections.ObjectModel;
using RankingApp.Models;

namespace RankingApp.ViewModels
{
    public class AllGamesViewModel : BaseViewModel
    {
        private readonly RankingAppsDatabase _database;
        public ObservableCollection<Game> Games { get; set; }
        public List<Game> GamesList { get; set; } = new List<Game>();

        public AllGamesViewModel(RankingAppsDatabase database)
        {
            _database = database;
            _ = LoadGamesAsync();
        }

        public async Task LoadGamesAsync()
        {
            var games = await _database.GetGamesAsync();
            GamesList = games.OrderByDescending(x => x.TournamentDate).ToList();

            Games = new ObservableCollection<Game>(GamesList);

            OnPropertyChanged();
        }

        public async Task<List<Game>> GetGames()
        {
            var games = await _database.GetGamesAsync();
            var list = games.OrderByDescending(x => x.TournamentDate).ToList();

            return list;
        }

        public List<Game> SearchTournaments(List<Game> games, string filterText)
        {
            var searchedGames = games
                .Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                             x.Name.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) &&
                             x.Surname.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return searchedGames;
        }

        public async Task DeleteGame(Game game)
        {
            await _database.DeleteGameAsync(game);
        }
    }
}

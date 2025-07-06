using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Services;
using Game = RankingApp.Models.Game;

namespace RankingApp.ViewModels
{
    public class TournamentViewModel(DatabaseService database) : BaseViewModel
    {
        private readonly DatabaseService _database = database;
        public ObservableCollection<Game> Games { get; set; } = new();
        private Tournament _tournament;
        public required Tournament Tournament
        {
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

        public async Task SaveTournamentAsync()
        {
            await _database.SaveTournamentAsync(Tournament);
        }

        public async Task LoadGamesAsync()
        {
            Tournament = await _database.GetTournamentAsync(Data.TournamentId);
            var allGames = await _database.GetGamesAsync();
            var tournamentGames = allGames.Where(x => x.TournamentId == Data.TournamentId).ToList();
            Games = new ObservableCollection<Game>(tournamentGames);
            Tournament.PointsDifference = allGames
                                          .Where(x => x.TournamentId == Data.TournamentId)
                                          .Sum(x => x.RatingDifference);

            OnPropertyChanged();
        }
    }
}

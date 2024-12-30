using System.Collections.ObjectModel;
using RankingApp.Models;
using Game = RankingApp.Models.Game;

namespace RankingApp.ViewModels
{
    public class TournamentViewModel(RankingAppsDatabase database) : BaseViewModel
    {
        private readonly RankingAppsDatabase _database = database;
        public ObservableCollection<Game> Games { get; set; } = new();
        public required Tournament Tournament { get; set; }

        public async Task SaveTournamentAsync()
        {
            await _database.SaveTournamentAsync(Tournament);
        }

        public async Task LoadGamesAsync()
        {
            Tournament = await _database.GetTournamentAsync(Data.TournamentId);
            var list = await _database.GetGamesAsync();
            var tgames = list.Where(x => x.TournamentId == Data.TournamentId).ToList();
            Games = new ObservableCollection<Game>(tgames);
            Tournament.PointsDifference = list
                .Where(x => x.TournamentId == Data.TournamentId)
                .Sum(x => x.RatingDifference);

            OnPropertyChanged();
        }
    }
}

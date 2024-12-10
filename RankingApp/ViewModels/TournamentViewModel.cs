using System.Collections.ObjectModel;
using System.Windows.Input;
using RankingApp.Models;
using RankingApp.Views;
using Game = RankingApp.Models.Game;

namespace RankingApp.ViewModels
{
    public class TournamentViewModel : BaseViewModel
    {
        private readonly RankingAppsDatabase _database;
        public ObservableCollection<Game> Games { get; set; } = new();
        public int TournamentDifference { get; set; }

        public TournamentViewModel(RankingAppsDatabase database)
        {
            _database = database;
        }

        public async Task SaveTournamentAsync()
        {
            var tId = Data.TournamentId;
            var tournament = await _database.GetTournamentAsync(tId);
            tournament.Name = Data.TournamentName;
            tournament.Date = Data.TournamentDate;
            tournament.Coefficient = Data.Coefficient;
            tournament.TournamentPlayerName = Data.TournamentPlayerName;
            tournament.TournamentPlayerSurname = Data.TournamentPlayerSurname;
            tournament.TournamentPlayerPoints = Data.TournamentPlayerPoints;
            tournament.PointsDifference = TournamentDifference;
            await _database.SaveTournamentAsync(tournament);
        }

        public async Task LoadGamesAsync()
        {
            var tournamentId = Data.TournamentId;
            var list = await _database.GetGamesAsync();
            var tgames = list.Where(x => x.TournamentId == tournamentId).ToList();
            Games = new ObservableCollection<Game>(tgames);

            OnPropertyChanged(nameof(Games));
        }

        public async Task LoadTournamentDifference()
        {
            var tournamentId = Data.TournamentId;
            var list = await _database.GetGamesAsync();
            TournamentDifference = list
                .Where(x => x.TournamentId == tournamentId)
                .Sum(x=>x.RatingDifference);

            OnPropertyChanged();
        }
    }
}

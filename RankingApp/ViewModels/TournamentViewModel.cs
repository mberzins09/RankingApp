using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Game = RankingApp.Models.Game;

namespace RankingApp.ViewModels
{
    public partial class TournamentViewModel(DatabaseService database) : BaseViewModel
    {
        private readonly DatabaseService _database = database;

        [ObservableProperty]
        private ObservableCollection<Game> games;

        [ObservableProperty]
        private Tournament oneTournament;

        [ObservableProperty]
        private Game selectedGame;

        partial void OnSelectedGameChanged(Game value)
        {
            if (value != null)
            {
                Data.GameId = value.Id;
                Shell.Current.GoToAsync(nameof(Games));
            }
        }

        public async Task SaveTournamentAsync()
        {
            await _database.SaveTournamentAsync(OneTournament);
        }

        public async Task LoadGamesAsync()
        {
            OneTournament = await _database.GetTournamentAsync(Data.TournamentId);
            var allGames = await _database.GetGamesAsync();
            var tournamentGames = allGames.Where(x => x.TournamentId == Data.TournamentId).ToList();
            Games = new ObservableCollection<Game>(tournamentGames);
            OneTournament.PointsDifference = allGames
                                          .Where(x => x.TournamentId == Data.TournamentId)
                                          .Sum(x => x.RatingDifference);
        }

        [RelayCommand]
        private async Task DeleteGameAsync(Game game)
        {
            if (game == null)
                return;

            await _database.DeleteGameAsync(game);
            await LoadGamesAsync();
        }

        public async Task CreateNewGameSave()
        {
            var player = await _database.GetPlayerAsync(OneTournament.TournamentPlayerId);

            var game = new Game()
            {
                MyName = player.Name == "Edgars(R)" ? "Edgars" : player.Name,
                MySurname = player.Surname,
                MyPoints = player.Points,
                MyPointsWithBonus = player.PointsWithBonus,
                MyAge = player.Age,
                MyPlace = player.Place,
                GameCoefficient = OneTournament.Coefficient,
                TournamentDate = OneTournament.Date,
                IsOpponentForeign = false,
                OpponentPoints = 0,
                TournamentId = OneTournament.Id,
                TournamentName = OneTournament.Name
            };

            await _database.SaveGameAsync(game);

            Data.GameId = game.Id;
        }
    }
}

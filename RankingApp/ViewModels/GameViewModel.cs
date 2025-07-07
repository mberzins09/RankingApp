using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RankingApp.ViewModels
{
    public partial class GameViewModel(DatabaseService databaseService) : BaseViewModel
    {
        private readonly DatabaseService _databaseService = databaseService;
        
        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private Game? oneGame;

        [ObservableProperty]
        private PlayerDB? selectedOpponent;

        private List<PlayerDB> _players = [];
        public ObservableCollection<int> SetsOptions { get; } = [0, 1, 2, 3, 4];

        partial void OnSelectedOpponentChanged(PlayerDB? value)
        {
            if (value is null || OneGame is null)
                return;

            OneGame.Name = value.Name;
            OneGame.Surname = value.Surname;
            OneGame.OpponentPoints = value.Points;
        }

        public async Task LoadDataAsync()
        {
            OneGame = await _databaseService.GetGameAsync(Data.GameId);
            var players = await _databaseService.GetPlayersAsync();
            var tournament = await _databaseService.GetTournamentAsync(OneGame.TournamentId);
            _players = players.Where(x => x.Id != tournament.TournamentPlayerId).OrderByDescending(x => x.PointsWithBonus).ToList();
            Players = new ObservableCollection<PlayerDB>(_players);

            OneGame.GameCoefficient = tournament.Coefficient;
            await _databaseService.SaveGameAsync(OneGame);
        }

        public async Task SaveGameAsync()
        {
            await _databaseService.SaveGameAsync(OneGame);
        }

        public async Task<List<PlayerDB>> GetPlayers()
        {
            var tournament = await _databaseService.GetTournamentAsync(OneGame.TournamentId);
            var players = await _databaseService.GetPlayersAsync();
            var list = players.Where(x => x.Id != tournament.TournamentPlayerId).OrderByDescending(x => x.PointsWithBonus).ToList();

            return list;
        }

        public void FilterPlayers(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Players = new ObservableCollection<PlayerDB>(_players);
                return;
            }

            var filtered = _players.Where(x => (!string.IsNullOrWhiteSpace(x.Name) && x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) && x.Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Place.ToString()) && x.Place.ToString().StartsWith(searchText, StringComparison.OrdinalIgnoreCase)))
                            .ToList();
            
            Players = new ObservableCollection<PlayerDB>(filtered);
        }
    }

    public static class BoolValues
    {
        public static readonly bool[] Values = { true, false };
    }
}

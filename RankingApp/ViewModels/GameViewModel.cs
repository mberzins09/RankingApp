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

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private BoolOption? selectedOpponentForeignOption;

        private List<PlayerDB> _allPlayers = [];
        public ObservableCollection<int> SetsOptions { get; } = [0, 1, 2, 3, 4];
        public List<BoolOption> IsOpponentForeignOptions { get; } = new()
        {
            new BoolOption { Value = true, Label = "Yes" },
            new BoolOption { Value = false, Label = "No" }
        };

        partial void OnSelectedOpponentForeignOptionChanged(BoolOption? value)
        {
            if (OneGame is null || value is null)
                return;

            OneGame.IsOpponentForeign = value.Value;
        }

        partial void OnSearchTextChanged(string? value)
        {
            FilterPlayers(value ?? string.Empty);
        }

        partial void OnSelectedOpponentChanged(PlayerDB? value)
        {
            if (value is null || OneGame is null)
                return;

            AssignOpponentProperties(value);
        }

        public async Task LoadDataAsync()
        {
            OneGame = await _databaseService.GetGameAsync(Data.GameId);
            var players = await _databaseService.GetPlayersAsync();
            var tournament = await _databaseService.GetTournamentAsync(OneGame.TournamentId);
            var me = await _databaseService.GetPlayerAsync(tournament.TournamentPlayerId);
            AssignMyProperties(me);
            _allPlayers = players.Where(x => x.Id != tournament.TournamentPlayerId).OrderByDescending(x => x.PointsWithBonus).ToList();
            Players = new ObservableCollection<PlayerDB>(_allPlayers);

            OneGame.GameCoefficient = tournament.Coefficient;
            SelectedOpponentForeignOption = IsOpponentForeignOptions.FirstOrDefault(x => x.Value == OneGame.IsOpponentForeign);
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

        public void FilterPlayers(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Players = new ObservableCollection<PlayerDB>(_allPlayers);
                return;
            }

            var filtered = _allPlayers.Where(x => (!string.IsNullOrWhiteSpace(x.Name) && x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Surname) && x.Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Place.ToString()) && x.Place.ToString().StartsWith(searchText, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

            Players = new ObservableCollection<PlayerDB>(filtered);
        }

        private void AssignOpponentProperties(PlayerDB opponent)
        {
            if (OneGame is null)
                return;

            OneGame.Name = opponent.Name;
            OneGame.Surname = opponent.Surname;
            OneGame.OpponentPoints = opponent.Points;
            OneGame.OpponentPointsWithBonus = opponent.PointsWithBonus;
            OneGame.OpponentAge = opponent.Age;
            OneGame.OpponentPlace = opponent.Place;
        }

        private void AssignMyProperties(PlayerDB me)
        {
            if (OneGame is null)
                return;

            OneGame.MyName = me.Name;
            OneGame.MySurname = me.Surname;
            OneGame.MyPoints = me.Points;
            OneGame.MyPointsWithBonus = me.PointsWithBonus;
            OneGame.MyAge = me.Age;
            OneGame.MyPlace = me.Place;
        }
    }
}

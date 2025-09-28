using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RankingApp.ViewModels
{
    public partial class EditTournamentPlayerViewModel(DatabaseService database) : BaseViewModel
    {
        private readonly DatabaseService _database = database;

        private List<PlayerDB> _allPlayers = [];

        [ObservableProperty]
        private bool isSearchDisabled;

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private PlayerDB? selectedPlayer;

        [ObservableProperty]
        private Tournament? currentTournament;

        partial void OnSearchTextChanged(string? value)
        {
            FilterPlayers(value ?? string.Empty);
        }

        partial void OnSelectedPlayerChanged(PlayerDB? value)
        {
            if (value is null || CurrentTournament is null)
                return;

            CurrentTournament.TournamentPlayerName = value.Name == "Edgars(R)" ? "Edgars" : value.Name;
            CurrentTournament.TournamentPlayerSurname = value.Surname;
            CurrentTournament.TournamentPlayerPoints = value.Points;
            CurrentTournament.TournamentPlayerId = value.Id;

            _ = EditMe(value.Name, value.Surname, value.Points);
        }

        public async Task LoadDataAsync()
        {
            IsSearchDisabled = true;
            CurrentTournament = await _database.GetTournamentAsync(Data.TournamentId);
            var players = await _database.GetPlayersAsync();
            _allPlayers = players.OrderByDescending(x => x.PointsWithBonus).ToList();

            Players = new ObservableCollection<PlayerDB>(_allPlayers);
            IsSearchDisabled = false;
        }

        public async Task<List<PlayerDB>> GetPlayers()
        {
            var players = await _database.GetPlayersAsync();
            var list = players.OrderByDescending(x => x.PointsWithBonus).ToList();

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

        public async Task EditMe(string name, string surname, int points)
        {
            if (CurrentTournament is null)
                return;

            var Games = await _database.GetGamesAsync();
            var nameGames = Games.Where(x => x.TournamentId == CurrentTournament.Id).ToList();
            foreach (var game in nameGames)
            {
                game.MyName = name == "Edgars(R)" ? "Edgars" : name;
                game.MySurname = surname;
                game.MyPoints = points;

                await _database.SaveGameAsync(game);
            }

            await _database.SaveTournamentAsync(CurrentTournament);
        }
    }
}

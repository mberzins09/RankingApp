using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels
{
    public partial class AllGamesViewModel(DatabaseService database) : BaseViewModel
    {
        private readonly DatabaseService _database = database;

        private List<Game> _allGames = [];

        private List<DoublesGame> _allDoublesGames = [];

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private ObservableCollection<Game>? games;

        [ObservableProperty]
        private ObservableCollection<DoublesGame>? doublesGames;

        public List<string> GameModes { get; } = new() { "Singles", "Doubles" };

        [ObservableProperty]
        private string selectedGameMode = "Singles";

        [ObservableProperty]
        private ObservableCollection<IGame> displayGames = [];

        partial void OnSelectedGameModeChanged(string value)
        {
            RefreshDisplayGames();
        }

        partial void OnSearchTextChanged(string? value)
        {
            FilterGames(value ?? string.Empty);
        }

        public async Task LoadDataAsync()
        {
            var localGames = await _database.GetGamesAsync();
            _allGames = localGames.OrderByDescending(x => x.TournamentDate).ToList();
            //Games = new ObservableCollection<Game>(_allGames);

            var localDoubles = await _database.GetDoublesGamesAsync();
            _allDoublesGames = localDoubles.OrderByDescending(x => x.TournamentDate).ToList();
            //DoublesGames = new ObservableCollection<DoublesGame>(_allDoublesGames);

            RefreshDisplayGames();
        }

        private void RefreshDisplayGames()
        {
            DisplayGames.Clear();

            if (SelectedGameMode == "Doubles")
            {
                foreach (var game in _allDoublesGames)
                    DisplayGames.Add(game);
            }
            else
            {
                foreach (var game in _allGames)
                    DisplayGames.Add(game);
            }
        }

        public void FilterGames(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                RefreshDisplayGames();
                return;
            }

            DisplayGames.Clear();

            if (SelectedGameMode == "Doubles")
            {
                var searchedDoublesGames = _allDoublesGames.Where(x => (!string.IsNullOrWhiteSpace(x.MyPartnerName) &&
                                                x.MyPartnerName.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                                (!string.IsNullOrWhiteSpace(x.MyPartnerSurname) &&
                                                x.MyPartnerSurname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                                (!string.IsNullOrWhiteSpace(x.Opponent1Name) &&
                                                x.Opponent1Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                                (!string.IsNullOrWhiteSpace(x.Opponent2Name) &&
                                                x.Opponent2Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                                (!string.IsNullOrWhiteSpace(x.Opponent1Surname) &&
                                                x.Opponent1Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                                (!string.IsNullOrWhiteSpace(x.Opponent2Surname) &&
                                                x.Opponent2Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))).ToList();

                foreach (var game in searchedDoublesGames)
                    DisplayGames.Add(game);
            }
            else
            {
                var searchedGames = _allGames.Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                                                x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                                (!string.IsNullOrWhiteSpace(x.Surname) &&
                                                x.Surname.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))).ToList();

                foreach (var game in searchedGames)
                    DisplayGames.Add(game);
            }
        }
    }
}

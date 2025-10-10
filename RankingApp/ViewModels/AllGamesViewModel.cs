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

        public List<string> GameModes { get; } = new() { "Singles", "Doubles" };

        [ObservableProperty]
        private string selectedGameMode = "Singles";

        [ObservableProperty]
        private ObservableCollection<IGame> displayGames = [];

        [ObservableProperty]
        private int totalGames;

        [ObservableProperty]
        private int totalWins;

        [ObservableProperty]
        private int totalLosses;

        [ObservableProperty]
        private int fifthSetTotal;

        [ObservableProperty]
        private double fifthSetWinPercentage;

        public double GameFontSize => SelectedGameMode == "Singles" ? 16 : 10;

        partial void OnSelectedGameModeChanged(string value)
        {
            RefreshDisplayGames();
            OnPropertyChanged(nameof(GameFontSize));
        }

        partial void OnSearchTextChanged(string? value)
        {
            FilterGames(value ?? string.Empty);
        }

        public async Task LoadDataAsync()
        {
            var localGames = await _database.GetGamesAsync();
            _allGames = [.. localGames.OrderByDescending(x => x.TournamentDate)];

            var localDoubles = await _database.GetDoublesGamesAsync();
            _allDoublesGames = [.. localDoubles.OrderByDescending(x => x.TournamentDate)];

            RefreshDisplayGames();
        }

        private void RefreshDisplayGames()
        {
            DisplayGames.Clear();

            IEnumerable<IGame> source = SelectedGameMode == "Doubles" ? _allDoublesGames : _allGames;

            foreach (var game in source)
                DisplayGames.Add(game);

            UpdateStats();
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

            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalGames = DisplayGames.Count;
            TotalWins = DisplayGames.Count(g => g.IsWin);
            TotalLosses = DisplayGames.Count(g => !g.IsWin);

            var fifthSetGames = DisplayGames.Where(g => (g.MySets ?? 0) + (g.OpponentSets ?? 0) == 5);
            FifthSetTotal = fifthSetGames.Count();
            var fifthSetWins = fifthSetGames.Count(g => g.IsWin);

            FifthSetWinPercentage = FifthSetTotal > 0
                ? Math.Round((double)fifthSetWins / FifthSetTotal * 100, 2)
                : 0;
        }
    }
}

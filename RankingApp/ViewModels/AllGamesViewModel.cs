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

        [ObservableProperty]
        private int selectedYear = 0;

        [ObservableProperty]
        private ObservableCollection<int> years = [];

        public double GameFontSize => SelectedGameMode == "Singles" ? 16 : 10;

        partial void OnSelectedGameModeChanged(string value)
        {
            ApplyAllFilters();
            OnPropertyChanged(nameof(GameFontSize));
        }

        partial void OnSearchTextChanged(string? value) => ApplyAllFilters();

        partial void OnSelectedYearChanged(int value) => ApplyAllFilters();

        public async Task LoadDataAsync()
        {
            var localGames = await _database.GetAllRecordsAsync<Game>();
            _allGames = [.. localGames.OrderByDescending(x => x.TournamentDate)];

            var localDoubles = await _database.GetAllRecordsAsync<DoublesGame>();
            _allDoublesGames = [.. localDoubles.OrderByDescending(x => x.TournamentDate)];

            var allYears = _allGames
                .Select(g => g.TournamentDate.Year)
                .Concat(_allDoublesGames.Select(d => d.TournamentDate.Year))
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            Years.Clear();
            Years.Add(0);
            foreach (var year in allYears)
                Years.Add(year);

            SelectedYear = 0;
            OnPropertyChanged(nameof(SelectedYear));

            ApplyAllFilters();
        }

        private void ApplyAllFilters()
        {
            IEnumerable<IGame> source = SelectedGameMode == "Doubles"
                ? _allDoublesGames.Cast<IGame>()
                : _allGames.Cast<IGame>();

            if (SelectedYear > 0)
                source = source.Where(g => g.TournamentDate.Year == SelectedYear);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var parts = SearchText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var hasTwoParts = parts.Length >= 2;
                var firstPart = parts[0];
                var secondPart = hasTwoParts ? parts[1] : string.Empty;

                if (SelectedGameMode == "Doubles")
                {
                    source = source.OfType<DoublesGame>().Where(x =>
                        (hasTwoParts &&
                            ((!string.IsNullOrWhiteSpace(x.MyPartnerName) &&
                              x.MyPartnerName.StartsWith(firstPart, StringComparison.OrdinalIgnoreCase) &&
                              !string.IsNullOrWhiteSpace(x.MyPartnerSurname) &&
                              x.MyPartnerSurname.StartsWith(secondPart, StringComparison.OrdinalIgnoreCase)) ||

                             (!string.IsNullOrWhiteSpace(x.Opponent1Name) &&
                              x.Opponent1Name.StartsWith(firstPart, StringComparison.OrdinalIgnoreCase) &&
                              !string.IsNullOrWhiteSpace(x.Opponent1Surname) &&
                              x.Opponent1Surname.StartsWith(secondPart, StringComparison.OrdinalIgnoreCase)) ||

                             (!string.IsNullOrWhiteSpace(x.Opponent2Name) &&
                              x.Opponent2Name.StartsWith(firstPart, StringComparison.OrdinalIgnoreCase) &&
                              !string.IsNullOrWhiteSpace(x.Opponent2Surname) &&
                              x.Opponent2Surname.StartsWith(secondPart, StringComparison.OrdinalIgnoreCase))
                            )
                        ) ||
                        (!hasTwoParts && (
                            (!string.IsNullOrWhiteSpace(x.MyPartnerName) &&
                             x.MyPartnerName.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.MyPartnerSurname) &&
                             x.MyPartnerSurname.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Opponent1Name) &&
                             x.Opponent1Name.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Opponent1Surname) &&
                             x.Opponent1Surname.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Opponent2Name) &&
                             x.Opponent2Name.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(x.Opponent2Surname) &&
                             x.Opponent2Surname.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)))
                        )
                    );
                }
                else
                {
                    source = source.OfType<Game>().Where(x =>
                        (hasTwoParts &&
                            (!string.IsNullOrWhiteSpace(x.Name) &&
                             x.Name.StartsWith(firstPart, StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrWhiteSpace(x.Surname) &&
                             x.Surname.StartsWith(secondPart, StringComparison.OrdinalIgnoreCase))) ||
                        (!hasTwoParts &&
                            ((!string.IsNullOrWhiteSpace(x.Name) &&
                              x.Name.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                             (!string.IsNullOrWhiteSpace(x.Surname) &&
                              x.Surname.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase))))
                    );
                }
            }

            DisplayGames = new ObservableCollection<IGame>(source.OrderByDescending(g => g.TournamentDate));

            UpdateStats();
        }

        private void UpdateStats()
        {
            IEnumerable<IGame> statsSource = DisplayGames;

            TotalGames = statsSource.Count();
            TotalWins = statsSource.Count(g => g.IsWin);
            TotalLosses = statsSource.Count(g => !g.IsWin);

            var fifthSetGames = statsSource.Where(g => (g.MySets ?? 0) + (g.OpponentSets ?? 0) == 5);
            FifthSetTotal = fifthSetGames.Count();
            var fifthSetWins = fifthSetGames.Count(g => g.IsWin);

            FifthSetWinPercentage = FifthSetTotal > 0
                ? Math.Round((double)fifthSetWins / FifthSetTotal * 100, 2)
                : 0;
        }
    }
}

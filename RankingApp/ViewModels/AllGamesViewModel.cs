using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private GameStatistics? stats;

        [ObservableProperty]
        private int selectedYear = 0;

        [ObservableProperty]
        private ObservableCollection<int> years = [];

        public List<string> RatingFilters { get; } = new() { "All Games", "Biggest wins", "Biggest losses" };

        [ObservableProperty]
        private string selectedRatingFilter = "All Games";

        public double GameFontSize => SelectedGameMode == "Singles" ? 16 : 10;

        partial void OnSelectedGameModeChanged(string value)
        {
            ApplyAllFilters();
            OnPropertyChanged(nameof(GameFontSize));
        }

        partial void OnSelectedRatingFilterChanged(string value) => ApplyAllFilters();

        partial void OnSearchTextChanged(string? value) => ApplyAllFilters();

        partial void OnSelectedYearChanged(int value) => ApplyAllFilters();

        [RelayCommand]
        private async Task DeleteItemAsync(IGame item)
        {
            switch (item)
            {
                case Game game:
                    await _database.DeleteAsync<Game>(game);
                    break;

                case DoublesGame doublesGame:
                    await _database.DeleteAsync<DoublesGame>(doublesGame);
                    break;

                default:
                    return;
            }

            await LoadDataAsync();
        }

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

            if (!string.IsNullOrWhiteSpace(SelectedRatingFilter) && SelectedRatingFilter != "All Games")
            {
                if (SelectedGameMode == "Doubles")
                {
                    source = source.OfType<DoublesGame>().Where(doubleGame =>
                    {
                        return SelectedRatingFilter switch
                        {
                            "Biggest wins" => doubleGame.GameCoefficient switch
                            {
                                "0.5" => doubleGame.RatingDifference >= 8,
                                "1" => doubleGame.RatingDifference >= 16,
                                _ => false
                            },
                            "Biggest losses" => doubleGame.GameCoefficient switch
                            {
                                "0.5" => doubleGame.RatingDifference <= -7,
                                "1" => doubleGame.RatingDifference <= -14,
                                _ => false
                            },
                            _ => true
                        };
                    });
                }
                else
                {
                    source = source.OfType<Game>().Where(game =>
                    {
                        return SelectedRatingFilter switch
                        {
                            "Biggest wins" => game.GameCoefficient switch
                            {
                                "0.5" => game.RatingDifference >= 8,
                                "1" => game.RatingDifference >= 16,
                                _ => false
                            },
                            "Biggest losses" => game.GameCoefficient switch
                            {
                                "0.5" => game.RatingDifference <= -7,
                                "1" => game.RatingDifference <= -14,
                                _ => false
                            },
                            _ => true
                        };
                    });
                }
            }

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

            IEnumerable<IGame> ordered;
            switch (SelectedRatingFilter)
            {
                case "Biggest wins":
                    ordered = source.OrderByDescending(g => g.RatingDifference).ThenByDescending(g => g.TournamentDate);
                    break;
                case "Biggest losses":
                    ordered = source.OrderBy(g => g.RatingDifference).ThenByDescending(g => g.TournamentDate);
                    break;
                default:
                    ordered = source.OrderByDescending(g => g.TournamentDate);
                    break;
            }
            DisplayGames = new ObservableCollection<IGame>(ordered);

            Stats = GameStatisticsCalculator.Calculate(DisplayGames);
        }
    }
}

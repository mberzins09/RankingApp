using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    public partial class AllGamesViewModel(IDatabaseService database, INavigationService navigation) : BaseViewModel
    {
        private readonly INavigationService _navigation = navigation;
        private readonly IDatabaseService _database = database;

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

        public List<string> RatingFilters { get; } = new() { "All Games", "Biggest wins", "Biggest losses", "Most games vs opponent", "Most wins vs opponent", "Most losses vs opponent" };

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
        public async Task GoToTournamentAsync(IGame game)
        {
            if (game == null)
            {
                return;
            }

            var tournament = await _database.GetByIdAsync<Tournament>(game.TournamentId);
            if (tournament == null)
            {
                return;
            }

            Data.TournamentId = game.TournamentId;
            Data.GameId = game.Id;

            await _navigation.GoToAsync("TournamentView");
        }

        [RelayCommand]
        public async Task GoToGameAsync(IGame game)
        {
            if (game == null)
            {
                return;
            }

            Data.TournamentId = game.TournamentId;
            Data.GameId = game.Id;

            if (game is Game)
            {
                await _navigation.GoToAsync("GameView");
            }
            else if (game is DoublesGame)
            {
                await _navigation.GoToAsync("DoublesGameView");
            }
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
                    source = source.OfType<DoublesGame>().Where(g =>
                    {
                        return SelectedRatingFilter switch
                        {
                            "Biggest wins" =>
                                ((g.OpponentPoints - g.MyTeamPoints) >= 20 && g.IsWin),

                            "Biggest losses" =>
                                ((g.MyTeamPoints - g.OpponentPoints) >= 20 && !g.IsWin),

                            _ => true
                        };
                    });
                }
                else
                {
                    var singlesSource = source.OfType<Game>();

                    switch (SelectedRatingFilter)
                    {
                        case "Biggest wins":
                            source = singlesSource.Where(g =>
                                (g.OpponentPoints - g.MyPoints) >= 20 && g.IsWin);
                            break;

                        case "Biggest losses":
                            source = singlesSource.Where(g =>
                                (g.MyPoints - g.OpponentPoints) >= 20 &&
                                !g.IsWin &&
                                !g.IsOpponentForeign);
                            break;

                        case "Most games vs opponent":
                            {
                                var topOpp = singlesSource
                                    .GroupBy(g => g.OppKeyName)
                                    .OrderByDescending(g => g.Count())
                                    .Select(g => g.Key)
                                    .FirstOrDefault();

                                source = topOpp == null
                                    ? []
                                    : singlesSource.Where(g => g.OppKeyName == topOpp);

                                break;
                            }

                        case "Most wins vs opponent":
                            {
                                var topOpp = singlesSource
                                    .Where(g => g.IsWin)
                                    .GroupBy(g => g.OppKeyName)
                                    .OrderByDescending(g => g.Count())
                                    .Select(g => g.Key)
                                    .FirstOrDefault();

                                source = topOpp == null
                                    ? []
                                    : singlesSource.Where(g => g.OppKeyName == topOpp);

                                break;
                            }

                        case "Most losses vs opponent":
                            {
                                var topOpp = singlesSource
                                    .Where(g => !g.IsWin)
                                    .GroupBy(g => g.OppKeyName)
                                    .OrderByDescending(g => g.Count())
                                    .Select(g => g.Key)
                                    .FirstOrDefault();

                                source = topOpp == null
                                    ? []
                                    : singlesSource.Where(g => g.OppKeyName == topOpp);

                                break;
                            }
                    }
                }
            }

            bool ignoreSearch = SelectedGameMode == "Singles" &&
                                (SelectedRatingFilter == "Most games vs opponent" ||
                                SelectedRatingFilter == "Most wins vs opponent" ||
                                SelectedRatingFilter == "Most losses vs opponent");

            if (!ignoreSearch && !string.IsNullOrWhiteSpace(SearchText))
            {
                string normalizedSearch = NameNormalizer.NormalizeKey(SearchText);

                if (SelectedGameMode == "Doubles")
                {
                    source = source.OfType<DoublesGame>().Where(x =>
                        x.FirstOppKeyName.Contains(normalizedSearch) ||
                        x.SecondOppKeyName.Contains(normalizedSearch));
                }
                else
                {
                    source = source.OfType<Game>().Where(x => x.OppKeyName.Contains(normalizedSearch));
                }
            }

            IEnumerable<IGame> ordered;
            switch (SelectedRatingFilter)
            {
                case "Biggest wins":
                    ordered = source.OrderByDescending(g => g.OpponentPoints - g.MyPoints).ThenByDescending(g => g.TournamentDate).ThenByDescending(g => g.RatingDifference);
                    break;
                case "Biggest losses":
                    ordered = source.OrderByDescending(g => g.MyPoints - g.OpponentPoints).ThenByDescending(g => g.TournamentDate).ThenBy(g => g.RatingDifference);
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

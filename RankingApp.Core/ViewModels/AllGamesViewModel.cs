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

        // ── "Most ... vs opponent": top 10 opponents instead of all games ─────

        private const int TopOpponentCount = 10;

        private static readonly string[] OpponentFilters =
            ["Most games vs opponent", "Most wins vs opponent", "Most losses vs opponent"];

        private List<Game> _yearSinglesGames = [];
        private string? _openOpponentKey;

        /// <summary>True when the list shows opponents (singles + one of the "Most ... vs opponent" filters).</summary>
        [ObservableProperty]
        private bool isOpponentMode;

        [ObservableProperty]
        private ObservableCollection<OpponentSummary> topOpponents = [];

        [ObservableProperty]
        private bool isOpponentOverlayVisible;

        [ObservableProperty]
        private string opponentTitle = "";

        [ObservableProperty]
        private ObservableCollection<IGame> opponentGames = [];

        [ObservableProperty]
        private GameStatistics? opponentStats;

        [RelayCommand]
        private void OpenOpponent(OpponentSummary? opponent)
        {
            if (opponent == null)
                return;

            _openOpponentKey = opponent.KeyName;
            OpponentTitle = opponent.Name;
            FillOpponentGames();
            IsOpponentOverlayVisible = true;
        }

        [RelayCommand]
        private void CloseOpponentOverlay()
        {
            IsOpponentOverlayVisible = false;
            _openOpponentKey = null;
        }

        private void FillOpponentGames()
        {
            var games = _yearSinglesGames
                .Where(g => g.OppKeyName == _openOpponentKey)
                .OrderByDescending(g => g.TournamentDate)
                .Cast<IGame>()
                .ToList();

            OpponentGames = new ObservableCollection<IGame>(games);
            OpponentStats = GameStatisticsCalculator.Calculate(games);
        }

        /// <summary>
        /// Groups the year's singles games by opponent and keeps the 10 with most games / wins / losses.
        /// Search narrows the opponents. Stats show all games against the listed opponents.
        /// </summary>
        private void ShowTopOpponents(List<Game> games)
        {
            _yearSinglesGames = games;

            var groups = games
                .Where(g => !string.IsNullOrEmpty(g.OppKeyName))
                .GroupBy(g => g.OppKeyName);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string normalizedSearch = NameNormalizer.NormalizeKey(SearchText);
                groups = groups.Where(gr => gr.Key.Contains(normalizedSearch));
            }

            var summaries = groups.Select(gr =>
            {
                var latest = gr.OrderByDescending(g => g.TournamentDate).First();
                int wins = gr.Count(g => g.IsWin);

                return new OpponentSummary
                {
                    KeyName = gr.Key,
                    Name = $"{latest.Name} {latest.Surname}".Trim(),
                    Games = gr.Count(),
                    Wins = wins,
                    Losses = gr.Count() - wins
                };
            });

            summaries = SelectedRatingFilter switch
            {
                "Most wins vs opponent" => summaries.Where(o => o.Wins > 0)
                    .OrderByDescending(o => o.Wins).ThenByDescending(o => o.Games),
                "Most losses vs opponent" => summaries.Where(o => o.Losses > 0)
                    .OrderByDescending(o => o.Losses).ThenByDescending(o => o.Games),
                _ => summaries.OrderByDescending(o => o.Games).ThenByDescending(o => o.Wins)
            };

            var top = summaries.Take(TopOpponentCount).ToList();

            for (int i = 0; i < top.Count; i++)
                top[i].Rank = i + 1;

            TopOpponents = new ObservableCollection<OpponentSummary>(top);

            var keys = top.Select(o => o.KeyName).ToHashSet();
            DisplayGames = new ObservableCollection<IGame>(
                games.Where(g => keys.Contains(g.OppKeyName)).OrderByDescending(g => g.TournamentDate));

            Stats = GameStatisticsCalculator.Calculate(DisplayGames);

            if (IsOpponentOverlayVisible && _openOpponentKey != null)
                FillOpponentGames();
        }

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

            if (IsOpponentOverlayVisible && _openOpponentKey != null)
                FillOpponentGames();
        }

        private void ApplyAllFilters()
        {
            IEnumerable<IGame> source = SelectedGameMode == "Doubles"
                ? _allDoublesGames.Cast<IGame>()
                : _allGames.Cast<IGame>();

            if (SelectedYear > 0)
                source = source.Where(g => g.TournamentDate.Year == SelectedYear);

            IsOpponentMode = SelectedGameMode == "Singles" && OpponentFilters.Contains(SelectedRatingFilter);

            if (IsOpponentMode)
            {
                ShowTopOpponents(source.OfType<Game>().ToList());
                return;
            }

            TopOpponents = [];

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

                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
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

using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.Core.ViewModels
{
    /// <summary>
    /// Start page: who is using the app, which ranking month is downloaded and a summary of the
    /// player's singles games. Year filter applies to games, tournaments and best place/rating;
    /// "current" place/rating always shows the newest known value.
    /// </summary>
    public partial class HomeViewModel(IDatabaseService database) : BaseViewModel
    {
        private const int UnrankedPlace = 10000;

        private readonly IDatabaseService _database = database;

        private List<Game> _allGames = [];
        private List<Tournament> _allTournaments = [];

        [ObservableProperty]
        private string playerName = "";

        [ObservableProperty]
        private bool hasPlayer;

        [ObservableProperty]
        private string rankingMonth = "";

        [ObservableProperty]
        private ObservableCollection<int> years = [];

        [ObservableProperty]
        private int selectedYear;

        [ObservableProperty]
        private GameStatistics? stats;

        [ObservableProperty]
        private int tournamentCount;

        [ObservableProperty]
        private string bestPlace = "-";

        [ObservableProperty]
        private string bestPlaceDate = "";

        [ObservableProperty]
        private string bestRating = "-";

        [ObservableProperty]
        private string bestRatingDate = "";

        [ObservableProperty]
        private string currentPlace = "-";

        [ObservableProperty]
        private string currentRating = "-";

        [ObservableProperty]
        private string currentSource = "";

        partial void OnSelectedYearChanged(int value) => ApplyFilters();

        public async Task LoadDataAsync()
        {
            var appData = await _database.GetAppDataAsync();

            var me = await FindAppPlayerAsync(appData);

            HasPlayer = me != null;
            PlayerName = me == null ? "" : $"{DisplayName(me.Name)} {me.Surname}".Trim();

            RankingMonth = appData.CurrentYear > 0 && appData.CurrentMonth is >= 1 and <= 12
                ? $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(appData.CurrentMonth)} {appData.CurrentYear}"
                : "Not downloaded yet";

            _allGames = await _database.GetAllRecordsAsync<Game>();
            _allTournaments = await _database.GetAllRecordsAsync<Tournament>();

            var allYears = _allTournaments.Select(t => t.Date.Year)
                .Concat(_allGames.Select(g => g.TournamentDate.Year))
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            int keepYear = allYears.Contains(SelectedYear) ? SelectedYear : 0;

            Years.Clear();
            Years.Add(0);
            foreach (var year in allYears)
                Years.Add(year);

            SelectedYear = keepYear;
            OnPropertyChanged(nameof(SelectedYear));

            ApplyFilters();
            UpdateCurrent(appData, me);
        }

        private void ApplyFilters()
        {
            IEnumerable<Game> games = _allGames;
            IEnumerable<Tournament> tournaments = _allTournaments;

            if (SelectedYear > 0)
            {
                games = games.Where(g => g.TournamentDate.Year == SelectedYear);
                tournaments = tournaments.Where(t => t.Date.Year == SelectedYear);
            }

            var gameList = games.ToList();

            Stats = GameStatisticsCalculator.Calculate(gameList);
            TournamentCount = tournaments.Count();

            // Best place = smallest number; the first time it was reached
            var bestPlaceGame = gameList
                .Where(g => g.MyPlace > 0 && g.MyPlace < UnrankedPlace)
                .OrderBy(g => g.MyPlace)
                .ThenBy(g => g.TournamentDate)
                .FirstOrDefault();

            BestPlace = bestPlaceGame?.MyPlace.ToString() ?? "-";
            BestPlaceDate = bestPlaceGame?.TournamentDate.ToString("d MMM yyyy") ?? "";

            var bestRatingGame = gameList
                .Where(g => g.MyPointsWithBonus > 0)
                .OrderByDescending(g => g.MyPointsWithBonus)
                .ThenBy(g => g.TournamentDate)
                .FirstOrDefault();

            BestRating = bestRatingGame?.MyPointsWithBonus.ToString() ?? "-";
            BestRatingDate = bestRatingGame?.TournamentDate.ToString("d MMM yyyy") ?? "";
        }

        /// <summary>
        /// Downloaded ranking month newer than the last played game → values from the ranking.
        /// Otherwise → values stored with the last played game.
        /// </summary>
        private void UpdateCurrent(AppData appData, PlayerDB? me)
        {
            var lastGame = _allGames
                .OrderByDescending(g => g.TournamentDate)
                .ThenByDescending(g => g.Id)
                .FirstOrDefault();

            DateTime? rankingDate = appData.CurrentYear > 0 && appData.CurrentMonth is >= 1 and <= 12
                ? new DateTime(appData.CurrentYear, appData.CurrentMonth, 1)
                : null;

            bool useRanking = me != null && rankingDate != null &&
                              (lastGame == null || rankingDate.Value > lastGame.TournamentDate.Date);

            if (useRanking)
            {
                CurrentPlace = me!.Place > 0 && me.Place < UnrankedPlace ? me.Place.ToString() : "-";
                CurrentRating = me.PointsWithBonus > 0 ? me.PointsWithBonus.ToString() : "-";
                CurrentSource = $"Ranking {RankingMonth}";
            }
            else if (lastGame != null)
            {
                CurrentPlace = lastGame.MyPlace > 0 && lastGame.MyPlace < UnrankedPlace ? lastGame.MyPlace.ToString() : "-";
                CurrentRating = lastGame.MyPointsWithBonus > 0 ? lastGame.MyPointsWithBonus.ToString() : "-";
                CurrentSource = $"Last game {lastGame.TournamentDate:d MMM yyyy}";
            }
            else
            {
                CurrentPlace = "-";
                CurrentRating = "-";
                CurrentSource = "";
            }
        }

        private async Task<PlayerDB?> FindAppPlayerAsync(AppData appData)
        {
            PlayerDB? me = null;

            if (appData.AppUserPlayerId != 0)
                me = await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId);

            if (me == null && !string.IsNullOrEmpty(appData.AppUserKeyName))
                me = await _database.GetPlayerByKeyAsync(appData.AppUserKeyName);

            return me;
        }

        // Same rule as elsewhere in the app: "Edgars(R)" is shown as "Edgars"
        private static string DisplayName(string? name) => name == "Edgars(R)" ? "Edgars" : name ?? "";
    }
}

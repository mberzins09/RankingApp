using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.Core.ViewModels
{
    public partial class ImportTournamentViewModel(TournamentService tournamentService, IDatabaseService databaseService, IPlayerRepositoryWithDate playerReposotoryWithDate, ApiGameImporterService importer, INavigationService navigation) : BaseViewModel
    {
        private readonly INavigationService _navigation = navigation;
        private readonly TournamentService _tournamentService = tournamentService;
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly IPlayerRepositoryWithDate _playerReposotoryWithDate = playerReposotoryWithDate;
        private readonly ApiGameImporterService _importer = importer; 
        private readonly Dictionary<string, List<PlayerDB>> playersCacheByMonth = [];
        private List<PlayerDB> players = [];
        private List<PlayerDB> databasePlayers = [];
        private AppData? _appData;

        // Lists behind the two modes of the page
        private List<APITournament> singlesTournaments = [];
        private List<APITournament>? teamsTournaments;   // loaded lazily, first time the toggle is switched on

        [ObservableProperty]
        private string statusText = "";

        [ObservableProperty]
        private bool isLoading;

        /// <summary>false = singles events of this player (default), true = all teams events.</summary>
        [ObservableProperty]
        private bool showTeamsEvents;

        public ObservableCollection<APITournament> Tournaments { get; set; } = [];

        public ObservableCollection<object> SelectedTournaments { get; set; } = [];

        public async Task LoadTournaments()
        {
            SelectedTournaments.Clear();
            StatusText = "";
            _appData = await _databaseService.GetAppDataAsync();
            databasePlayers = await _databaseService.GetAllRecordsAsync<PlayerDB>();

            await EnsureAppUserNewIdAsync();

            singlesTournaments = _appData.AppUserNewId > 0
                ? await _tournamentService.GetPlayerTournamentsAsync(_appData.AppUserNewId)
                : [];
            teamsTournaments = null; // refresh teams list too, if it is shown

            await ShowCurrentListAsync();
        }

        /// <summary>
        /// AppUserNewId (API player id) is copied only when the default player is chosen. Players that
        /// came from lgtf.sqlite may not have NewId yet at that moment (it is filled later by the
        /// ranking sync), so take it from the player's current PlayerDB row and save it.
        /// </summary>
        private async Task EnsureAppUserNewIdAsync()
        {
            if (_appData == null)
                return;

            var me = databasePlayers.FirstOrDefault(p => p.Id == _appData.AppUserPlayerId)
                     ?? databasePlayers.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            if (me != null && me.NewId > 0 && me.NewId != _appData.AppUserNewId)
            {
                _appData.AppUserNewId = me.NewId;
                await _databaseService.SaveAppDataAsync(_appData);
            }
        }

        private string SinglesEmptyMessage() =>
            _appData == null || _appData.AppUserPlayerId == 0
                ? "Select App Default Player first."
                : _appData.AppUserNewId == 0
                    ? "Your player has no API id yet - update the ranking list and try again."
                    : "No singles events found.";

        partial void OnShowTeamsEventsChanged(bool value)
        {
            _ = ShowCurrentListAsync();
        }

        /// <summary>Fills <see cref="Tournaments"/> with singles or teams events depending on the toggle.</summary>
        private async Task ShowCurrentListAsync()
        {
            try
            {
                SelectedTournaments.Clear();

                if (!ShowTeamsEvents)
                {
                    FillList(singlesTournaments);
                    StatusText = singlesTournaments.Count == 0 ? SinglesEmptyMessage() : "";
                    return;
                }

                if (teamsTournaments == null)
                {
                    Tournaments.Clear();
                    IsLoading = true;
                    StatusText = "Loading teams events...";

                    var events = await _tournamentService.GetTeamsEventsAsync();

                    // Season events come once per playing date (same id) - show each event once,
                    // spanning its first to last date. The import splits it by date again.
                    var mapped = events
                        .GroupBy(e => e.Id)
                        .Select(g => MergeTeamsEvent(g.ToList()))
                        .OrderByDescending(t => ParseDate(t.Date) ?? DateTime.MinValue)
                        .ToList();

                    // Don't cache a failed / empty response, so it is tried again next time
                    teamsTournaments = mapped.Count > 0 ? mapped : null;

                    IsLoading = false;
                    StatusText = mapped.Count > 0 ? "" : "No teams events found.";

                    // Toggle was switched back while loading - singles list is already shown
                    if (!ShowTeamsEvents)
                        return;

                    FillList(mapped);
                    return;
                }

                FillList(teamsTournaments);
            }
            catch (Exception ex)
            {
                IsLoading = false;
                StatusText = $"Failed to load events: {ex.Message}";
            }
        }

        private void FillList(List<APITournament> source)
        {
            Tournaments.Clear();

            foreach (var t in source)
                Tournaments.Add(t);
        }

        private static APITournament MergeTeamsEvent(List<APITeamsEventListItem> items)
        {
            var result = MapTeamsEvent(items[0]);

            var dates = items
                .SelectMany(i => new[] { i.StartDate, i.EndDate })
                .Where(d => ParseDate(d) != null)
                .OrderBy(d => ParseDate(d))
                .ToList();

            if (dates.Count > 0)
            {
                result.Date = dates.First();
                result.EndDate = dates.Last();
            }

            return result;
        }

        private static APITournament MapTeamsEvent(APITeamsEventListItem e) => new()
        {
            Id = e.Id,
            Date = e.StartDate,
            EndDate = e.EndDate,
            Competition = e.Competition?.Name is { } compName && !string.IsNullOrWhiteSpace(compName) ? compName : e.Name,
            EventName = e.Name,
            Coefficient = e.RankingCoef,
            IsSeason = e.IsSeasonRankingInstance,
            IsTeamsEvent = true
        };

        [RelayCommand]
        private async Task ImportSelectedTournamentsAsync()
        {
            var progress = new Progress<string>(msg =>
            {
                StatusText = msg;
            });

            await ImportSelectedTournamentsInternal(progress);
        }

        private async Task ImportSelectedTournamentsInternal(IProgress<string> progress)
        {
            var apiTournaments = SelectedTournaments.Cast<APITournament>().DistinctBy(t => t.Id).OrderBy(t => t.Date).ToList();

            if (apiTournaments.Count == 0)
                return;

            progress.Report("Starting import...");
            var existingTournaments = await _databaseService.GetAllRecordsAsync<Tournament>();
            await Task.Delay(300);

            // Messages that must stay visible (e.g. "you are not in any team") - page is not left then
            var problems = new List<string>();

            foreach (var apiTournament in apiTournaments)
            {
                if (_appData == null)
                    return;

                if (apiTournament.IsTeamsEvent)
                {
                    var problem = await ImportTeamsEventAsync(apiTournament, existingTournaments, progress);

                    if (problem != null)
                        problems.Add(problem);

                    continue;
                }

                bool alreadyExists = existingTournaments.Any(t => t.ExternalTournamentId == apiTournament.Id);

                if (alreadyExists && !apiTournament.IsSeason)
                {
                    progress.Report($"Skipping {apiTournament.Competition} (already imported)");

                    await Task.Delay(400);
                    continue;
                }

                progress.Report($"Importing {apiTournament.Competition} - {apiTournament.EventName}");

                if (!DateTime.TryParse(apiTournament.Date, out var parsedDate))
                    continue;

                players = await GetPlayersForDateAsync(parsedDate);

                await InsertTournament(apiTournament, parsedDate);
            }

            if (problems.Count > 0)
            {
                // Stay on the page so the user can read what happened
                progress.Report(string.Join(Environment.NewLine, problems));
                return;
            }

            progress.Report("Finished");
            await Task.Delay(500);

            await _navigation.GoToAsync("AllTournaments");
        }

        /// <summary>Ranking list valid for the month of the given date (cached per month).</summary>
        private async Task<List<PlayerDB>> GetPlayersForDateAsync(DateTime date)
        {
            if (_appData == null)
                return databasePlayers;

            int year = date.Year;
            int month = date.Month;

            if (year == _appData.CurrentYear && month == _appData.CurrentMonth)
                return databasePlayers;

            string key = $"{year}-{month}";

            if (!playersCacheByMonth.ContainsKey(key))
            {
                bool isOldApi = year >= 2014 && year <= 2025 && month >= 1 && month <= 10;

                string dateString = date.ToString("yyyy-MM");

                playersCacheByMonth[key] = await _playerReposotoryWithDate.GetPlayersAsync(dateString, isOldApi);
            }

            return playersCacheByMonth[key];
        }

        private async Task InsertTournament(APITournament apiTournament, DateTime parsedDate)
        {
            if (_appData == null)
                return;

            var me = players.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            me ??= databasePlayers.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            if (me == null)
                return;

            var tournament = new Tournament
            {
                ExternalTournamentId = apiTournament.Id,
                TournamentPlayerId = _appData.AppUserPlayerId,
                TournamentPlayerName = me.Name,
                TournamentPlayerSurname = me.Surname,
                Date = parsedDate,
                Name = BuildTournamentName(apiTournament),
                Coefficient = apiTournament.Coefficient
            };

            await _databaseService.SaveAsync(tournament);
            Data.TournamentId = tournament.Id;

            var apiGames = await _tournamentService.GetTournamentGames(_appData.AppUserNewId,apiTournament.Id,parsedDate.ToString("yyyy-MM-dd"),apiTournament.IsSeason);

            if (apiGames == null || apiGames.Count == 0)
                return;

            await _importer.InsertGamesAsync(apiGames, tournament, players, databasePlayers, me);
        }

        private static string BuildTournamentName(APITournament apiTournament)
        {
            if (apiTournament.EventName == null || apiTournament.EventName == "" || apiTournament.EventName == apiTournament.Competition)
            {
                return apiTournament.EventName ?? "Bez EventName";
            }

            return $"{apiTournament.Competition} - {apiTournament.EventName}";
        }

        // ====================================================================
        //  Teams events
        // ====================================================================

        /// <summary>
        /// Imports one teams event.
        ///  1. Loads the whole event once (no playing_date) and checks that the app user is in a team.
        ///  2. Groups the user's singles and doubles games by the date of their team match - every
        ///     playing day becomes its own tournament (season leagues have several days).
        /// Returns a message to keep on screen when something prevented the import, otherwise null.
        /// </summary>
        private async Task<string?> ImportTeamsEventAsync(APITournament apiTournament, List<Tournament> existingTournaments, IProgress<string> progress)
        {
            if (_appData == null)
                return null;

            string title = BuildTournamentName(apiTournament);
            int myApiId = _appData.AppUserNewId;
            string myKey = _appData.AppUserKeyName;

            progress.Report($"Loading {title}...");

            var eventResult = await _tournamentService.GetEventResultsAsync(apiTournament.Id);

            if (eventResult == null)
                return $"❌ Could not load data for {title}.";

            // ── 1. Is the user in any team of this event? ───────────────────
            if (!TeamsGamesCollector.IsMeListed(eventResult, myApiId, myKey))
                return $"❌ You are not listed in any team in {title}.";

            if (!string.IsNullOrWhiteSpace(eventResult.CompetitionEvent?.RankingCoef))
                apiTournament.Coefficient = eventResult.CompetitionEvent.RankingCoef;

            // Used only when a team match has no date at all
            var fallbackDate = ParseDate(eventResult.CompetitionEvent?.StartDate) ?? ParseDate(apiTournament.Date) ?? DateTime.Today;

            // ── 2. My games split by playing day ────────────────────────────
            var gamesByDate = TeamsGamesCollector.CollectMyGamesByDate(eventResult, myApiId, myKey, fallbackDate);

            if (gamesByDate.Count == 0)
                return $"ℹ️ {title}: you have no finished games there yet.";

            var existingSinglesIds = (await _databaseService.GetAllRecordsAsync<Game>())
                .Select(g => g.ExternalGameId).Where(id => id != 0).ToHashSet();

            var existingDoublesIds = (await _databaseService.GetAllRecordsAsync<DoublesGame>())
                .Select(g => g.ExternalGameId).Where(id => id != 0).ToHashSet();

            bool severalDays = gamesByDate.Count > 1;
            int importedRounds = 0;

            // ── 3. One tournament per day ───────────────────────────────────
            foreach (var (date, day) in gamesByDate.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)))
            {
                var newSingles = day.Singles.Where(g => !existingSinglesIds.Contains(g.Id)).ToList();
                var newDoubles = day.Doubles.Where(g => !existingDoublesIds.Contains(g.Id)).ToList();

                string roundName = severalDays ? $"{title} {date:yyyy-MM-dd}" : title;

                if (newSingles.Count + newDoubles.Count == 0)
                {
                    progress.Report($"Skipping {roundName} (already imported)");
                    await Task.Delay(400);
                    continue;
                }

                progress.Report($"Importing {roundName} ({newSingles.Count} singles, {newDoubles.Count} doubles)");

                players = await GetPlayersForDateAsync(date);

                var me = players.FirstOrDefault(p => p.KeyName == myKey)
                         ?? databasePlayers.FirstOrDefault(p => p.KeyName == myKey);

                if (me == null)
                    return "❌ App default player not found in the players list.";

                // Same event + same day already imported → add missing games to it, don't create a duplicate
                var tournament = existingTournaments.FirstOrDefault(t =>
                    t.ExternalTournamentId == apiTournament.Id && t.Date.Date == date.Date);

                if (tournament == null)
                {
                    tournament = new Tournament
                    {
                        ExternalTournamentId = apiTournament.Id,
                        TournamentPlayerId = _appData.AppUserPlayerId,
                        TournamentPlayerName = me.Name,
                        TournamentPlayerSurname = me.Surname,
                        Date = date,
                        Name = roundName,
                        Coefficient = apiTournament.Coefficient
                    };

                    await _databaseService.SaveAsync(tournament);
                    existingTournaments.Add(tournament);
                }

                Data.TournamentId = tournament.Id;

                await _importer.InsertGamesAsync(newSingles, tournament, players, databasePlayers, me, myApiId);
                await _importer.InsertDoublesGamesAsync(newDoubles, tournament, players, databasePlayers, me);

                foreach (var g in newSingles)
                    existingSinglesIds.Add(g.Id);

                foreach (var g in newDoubles)
                    existingDoublesIds.Add(g.Id);

                importedRounds++;
            }

            if (importedRounds == 0)
                return $"ℹ️ {title}: no new games to import.";

            return null;
        }

        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}

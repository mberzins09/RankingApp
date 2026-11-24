using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using System.Globalization;

namespace RankingApp.Core.Services
{
    public class PlayerService(IDatabaseService database, IPlayerRepositoryWithDate repositoryWithDate) : IPlayerService
    {
        private readonly IDatabaseService _database = database;
        private readonly IPlayerRepositoryWithDate _repositoryWithDate = repositoryWithDate;

        public async Task<List<PlayerDB>> LoadPlayersFromApiOrDbAsync(DateTime? date = null, Action<string>? progressCallback = null)
        {
            progressCallback?.Invoke("Checking current date...");
            DateTime now = date ?? DateTime.UtcNow;
            string currentDateString = now.ToString("yyyy-MM");
            string previousDateString = now.AddMonths(-1).ToString("yyyy-MM");

            // Months are read from the MonthRanking table when already downloaded (see CachedPlayerRepositoryWithDate)
            progressCallback?.Invoke($"Fetching players for {currentDateString}...");
            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(currentDateString, false);
            DateTime shownMonth = now;

            if (!HasRankedPlayers(apiPlayers))
            {
                progressCallback?.Invoke($"No data for {currentDateString}, trying {previousDateString}...");
                apiPlayers = await _repositoryWithDate.GetPlayersAsync(previousDateString, false);
                shownMonth = now.AddMonths(-1);

                if (HasRankedPlayers(apiPlayers))
                {
                    progressCallback?.Invoke($"Updating AppData for {previousDateString}...");
                    await UpdateAppDataWithDate(previousDateString);
                }
            }
            else
            {
                progressCallback?.Invoke($"Updating AppData for {currentDateString}...");
                await UpdateAppDataWithDate(currentDateString);
            }

            if (HasRankedPlayers(apiPlayers))
            {
                // PointsChanged = this month's PointsWithBonus - previous month's (previous month is downloaded and stored if missing)
                string beforeString = shownMonth.AddMonths(-1).ToString("yyyy-MM");
                progressCallback?.Invoke($"Comparing with {beforeString}...");
                var before = await _repositoryWithDate.GetPlayersAsync(beforeString, false);
                SetPointsChanged(apiPlayers, before);

                progressCallback?.Invoke("Syncing local database...");
                await SyncWithLocalDb(apiPlayers);
            }

            progressCallback?.Invoke("Sorting players...");
            return (await _database.GetAllRecordsAsync<PlayerDB>()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        public async Task<List<PlayerDB>> GetMonthRankingViewAsync(DateTime month, Action<string>? progressCallback = null)
        {
            string monthString = month.ToString("yyyy-MM");
            string beforeString = month.AddMonths(-1).ToString("yyyy-MM");

            // Read from the MonthRanking table, downloaded and stored only when missing
            progressCallback?.Invoke($"Loading ranking {monthString}...");
            var current = await _repositoryWithDate.GetPlayersAsync(monthString, false);

            if (!HasRankedPlayers(current))
                return [];

            progressCallback?.Invoke($"Comparing with {beforeString}...");
            var before = await _repositoryWithDate.GetPlayersAsync(beforeString, false);
            SetPointsChanged(current, before);

            // View only: nothing is written to PlayerDB. The local Id is attached (matched by KeyName),
            // so tapping a player / "set as default" still work with the real PlayerDB row.
            var dbByKey = (await _database.GetAllRecordsAsync<PlayerDB>())
                .Where(p => !string.IsNullOrEmpty(p.KeyName))
                .GroupBy(p => p.KeyName)
                .ToDictionary(g => g.Key, g => g.First());

            var list = current
                .Where(p => !string.IsNullOrEmpty(p.KeyName) && p.KeyName != "unrankedplayer")
                .ToList();

            foreach (var p in list)
            {
                p.IsActive = true;   // everyone in a month's ranking was ranked (active) that month

                if (dbByKey.TryGetValue(p.KeyName, out var dbPlayer))
                    p.Id = dbPlayer.Id;
            }

            return [.. list.OrderByDescending(p => p.PointsWithBonus)];
        }

        public async Task<bool> AreCurrentRankingsStoredAsync()
        {
            var appData = await _database.GetAppDataAsync();
            if (appData.CurrentYear <= 0 || appData.CurrentMonth is < 1 or > 12)
                return true;   // no ranking downloaded yet - nothing to complete

            var month = new DateTime(appData.CurrentYear, appData.CurrentMonth, 1);
            var before = month.AddMonths(-1);

            return await _database.HasMonthRankingAsync(month.Year, month.Month)
                && await _database.HasMonthRankingAsync(before.Year, before.Month);
        }

        public async Task EnsureCurrentRankingsStoredAsync(Action<string>? progressCallback = null)
        {
            var appData = await _database.GetAppDataAsync();
            if (appData.CurrentYear <= 0 || appData.CurrentMonth is < 1 or > 12)
                return;

            var month = new DateTime(appData.CurrentYear, appData.CurrentMonth, 1);
            string monthString = month.ToString("yyyy-MM");
            string beforeString = month.AddMonths(-1).ToString("yyyy-MM");

            // Stored months come from the table, missing ones are downloaded and stored (CachedPlayerRepositoryWithDate)
            progressCallback?.Invoke($"Downloading ranking {monthString}...");
            var current = await _repositoryWithDate.GetPlayersAsync(monthString, false);

            progressCallback?.Invoke($"Downloading ranking {beforeString}...");
            var before = await _repositoryWithDate.GetPlayersAsync(beforeString, false);

            if (!HasRankedPlayers(current) || !HasRankedPlayers(before))
                return;

            // PlayerDB already holds this month - only PointsChanged is set, nothing else is synced
            SetPointsChanged(current, before);

            progressCallback?.Invoke("Updating points changes...");
            await _database.UpdatePointsChangedAsync(current
                .Where(p => !string.IsNullOrEmpty(p.KeyName) && p.KeyName != "unrankedplayer")
                .GroupBy(p => p.KeyName)
                .ToDictionary(g => g.Key, g => g.First().PointsChanged));
        }

        public async Task GetOldTournamentsAsync(IProgress<string> progress)
        {
            var appData = await _database.GetAppDataAsync();

            if (appData.AppUserPlayerId == 0 ||
                string.IsNullOrWhiteSpace(appData.AppUserKeyName))
            {
                progress.Report("❌ You must select App Default Player first.");
                await Task.Delay(2000);
                return;
            }

            progress.Report("Loading local data...");

            // ── App DB: existing tournaments & player list ────────────────────
            var tournaments = await _database.GetAllRecordsAsync<Tournament>();
            var existingTournamentDates = new HashSet<DateOnly>(
                tournaments.Select(t => DateOnly.FromDateTime(t.Date)));

            var databasePlayers = await _database.GetAllRecordsAsync<PlayerDB>();

            // KeyName → PlayerDB for fast foreign-status lookup
            var databasePlayersByKey = databasePlayers
                .Where(p => !string.IsNullOrEmpty(p.KeyName))
                .ToDictionary(p => p.KeyName, p => p);

            var playerMe = await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId);
            if (playerMe == null)
            {
                progress.Report("❌ App Default Player not found. Select it again.");
                await Task.Delay(2000);
                return;
            }

            // ── Web API: all games and competitions for this player ───────────
            // Looked up by KeyName: player Ids on the server can differ from local PlayerDB Ids.
            var keyName = string.IsNullOrWhiteSpace(playerMe.KeyName) ? appData.AppUserKeyName : playerMe.KeyName;

            progress.Report("Downloading tournament history...");

            LgtfApiPlayerHistory? history;
            try
            {
                history = await LgtfWebApi.GetPlayerHistoryAsync(keyName);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                progress.Report("❌ Could not download data. Check your internet connection.");
                await Task.Delay(2000);
                return;
            }

            if (history == null)
            {
                progress.Report("ℹ️ No tournament history found for this player.");
                await Task.Delay(2000);
                return;
            }

            var meServerId = history.PlayerId;

            // Deduplicate games (same logic as before)
            var uniqueGames = history.Games
                .GroupBy(g => new { g.CompetitionId, g.Player1Id, g.Player2Id, g.Player1Sets, g.Player2Sets })
                .Select(g => g.First())
                .ToList();

            var competitionsSorted = history.Competitions
                .OrderBy(c => DateTime.Parse(c.StartDate, CultureInfo.InvariantCulture))
                .ToList();

            var competitionsToInsert = competitionsSorted
                .Where(c => !existingTournamentDates.Contains(
                    DateOnly.FromDateTime(DateTime.Parse(c.StartDate, CultureInfo.InvariantCulture))))
                .ToList();

            if (competitionsToInsert.Count == 0)
            {
                progress.Report("ℹ️ No new tournaments to import.");
                await Task.Delay(2000);
                return;
            }

            // ── Import each competition ───────────────────────────────────────
            foreach (var competition in competitionsToInsert)
            {
                var tournamentDate = DateTime.Parse(competition.StartDate, CultureInfo.InvariantCulture);

                progress.Report($"Importing {competition.Name}...");

                var tournamentName = string.IsNullOrEmpty(competition.Places)
                    ? competition.Name
                    : $"{competition.Name} ({competition.Places})";

                var tournament = new Tournament
                {
                    Name                    = tournamentName,
                    Coefficient             = competition.Coef.ToString(CultureInfo.InvariantCulture),
                    Date                    = tournamentDate,
                    TournamentPlayerId      = playerMe.Id,
                    TournamentPlayerName    = playerMe.Name,
                    TournamentPlayerSurname = playerMe.Surname
                };

                await _database.SaveAsync(tournament);

                var competitionGames = uniqueGames
                    .Where(g => g.CompetitionId == competition.Id)
                    .ToList();

                foreach (var lgtfGame in competitionGames)
                {
                    bool isMePlayer1 = lgtfGame.Player1Id == meServerId;

                    // ── Points and age — read straight from the pre-filled DB columns ──
                    int myPoints           = isMePlayer1 ? lgtfGame.Player1Points          : lgtfGame.Player2Points;
                    int myPointsWithBonus  = isMePlayer1 ? lgtfGame.Player1PointsWithBonus : lgtfGame.Player2PointsWithBonus;
                    int myAge              = isMePlayer1 ? lgtfGame.Player1Age             : lgtfGame.Player2Age;
                    int myPlace            = isMePlayer1 ? lgtfGame.Player1Place           : lgtfGame.Player2Place;

                    int oppPoints          = isMePlayer1 ? lgtfGame.Player2Points          : lgtfGame.Player1Points;
                    int oppPointsWithBonus = isMePlayer1 ? lgtfGame.Player2PointsWithBonus : lgtfGame.Player1PointsWithBonus;
                    int oppAge             = isMePlayer1 ? lgtfGame.Player2Age             : lgtfGame.Player1Age;
                    int oppPlace           = isMePlayer1 ? lgtfGame.Player2Place           : lgtfGame.Player1Place;

                    // ── Foreign status — opponent is foreign if not in app PlayerDB ──
                    string oppKeyName = isMePlayer1 ? lgtfGame.Player2KeyName : lgtfGame.Player1KeyName;
                    bool   isForeign  = string.IsNullOrEmpty(oppKeyName) ||
                                        !databasePlayersByKey.ContainsKey(oppKeyName);

                    // ── Opponent name — joined on the server ──────────────────────
                    string oppName    = isMePlayer1 ? lgtfGame.Player2Name    : lgtfGame.Player1Name;
                    string oppSurname = isMePlayer1 ? lgtfGame.Player2Surname : lgtfGame.Player1Surname;

                    var gameEntity = new Game
                    {
                        MyPoints                = myPoints,
                        MyName                  = playerMe.Name,
                        MySurname               = playerMe.Surname,
                        OpponentPoints          = oppPoints,
                        Name                    = oppName,
                        Surname                 = oppSurname,
                        MySets                  = isMePlayer1 ? lgtfGame.Player1Sets : lgtfGame.Player2Sets,
                        OpponentSets            = isMePlayer1 ? lgtfGame.Player2Sets : lgtfGame.Player1Sets,
                        TournamentId            = tournament.Id,
                        TournamentName          = tournament.Name,
                        TournamentDate          = tournament.Date,
                        GameCoefficient         = tournament.Coefficient,
                        IsOpponentForeign       = isForeign,
                        MyPointsWithBonus       = myPointsWithBonus,
                        OpponentPointsWithBonus = oppPointsWithBonus,
                        MyPlace                 = myPlace,
                        OpponentPlace           = oppPlace,
                        MyAge                   = myAge,
                        OpponentAge             = oppAge
                    };

                    await _database.SaveAsync(gameEntity);
                }
            }

            progress.Report("✅ Import finished");
            await Task.Delay(2000);
        }

        public async Task<List<PlayerDB>> GetPlayersFromDbAsync()
        {
            return (await _database.GetAllRecordsAsync<PlayerDB>()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        public async Task<List<Game>> GetGamesFromDbAsync()
        {
            return await _database.GetAllRecordsAsync<Game>();
        }

        public async Task<Tournament?> GetTournamentAsync(int id)
        {
            return await _database.GetByIdAsync<Tournament>(id);
        }

        public async Task<AppData> GetAppDataAsync()
        {
            return await _database.GetAppDataAsync();
        }

        public async Task SaveAppDataAsync(AppData appData)
        {
            await _database.SaveAppDataAsync(appData);
        }

        public async Task<PlayerDB?> GetAppDefaultPlayerAsync(AppData appData)
        {
            return await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>The ranking lists always end with an "Unranked Player" entry - real data means more than that.</summary>
        private static bool HasRankedPlayers(List<PlayerDB>? players) =>
            players != null && players.Any(p => !string.IsNullOrEmpty(p.KeyName) && p.KeyName != "unrankedplayer");

        /// <summary>
        /// Difference to the previous month's ranking. A player who was not in the previous ranking
        /// (new / returning) and every player when the previous month could not be loaded get 0.
        /// </summary>
        private static void SetPointsChanged(List<PlayerDB> players, List<PlayerDB>? previous)
        {
            var previousByKey = new Dictionary<string, int>();

            foreach (var p in previous ?? [])
            {
                if (!string.IsNullOrEmpty(p.KeyName) && p.KeyName != "unrankedplayer")
                    previousByKey.TryAdd(p.KeyName, p.PointsWithBonus);
            }

            foreach (var p in players)
            {
                p.PointsChanged = !string.IsNullOrEmpty(p.KeyName) && previousByKey.TryGetValue(p.KeyName, out int before)
                    ? p.PointsWithBonus - before
                    : 0;
            }
        }

        private async Task SyncWithLocalDb(List<PlayerDB> apiPlayers)
        {
            await _database.BulkUpsertPlayersAsync(apiPlayers);

            // Default player may have been chosen before his NewId (API id) was known
            var appData = await _database.GetAppDataAsync();
            if (appData.AppUserPlayerId == 0)
                return;

            var me = await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId);
            if (me != null && me.NewId > 0 && me.NewId != appData.AppUserNewId)
            {
                appData.AppUserNewId = me.NewId;
                await _database.SaveAppDataAsync(appData);
            }
        }

        private async Task UpdateAppDataWithDate(string dateString)
        {
            var appData = await _database.GetAppDataAsync();
            if (DateTime.TryParseExact(dateString, "yyyy-MM", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
            {
                appData.CurrentYear  = dt.Year;
                appData.CurrentMonth = dt.Month;
                await _database.SaveAppDataAsync(appData);
            }
        }
    }
}

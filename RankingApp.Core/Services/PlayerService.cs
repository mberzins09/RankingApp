using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using SQLite;
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

            progressCallback?.Invoke($"Fetching players for {currentDateString}...");
            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(currentDateString, false);
            if (apiPlayers == null || apiPlayers.Count == 0)
            {
                progressCallback?.Invoke($"No data for {currentDateString}, trying {previousDateString}...");
                apiPlayers = await _repositoryWithDate.GetPlayersAsync(previousDateString, false);
                if (apiPlayers != null && apiPlayers.Count > 0)
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

            if (apiPlayers != null && apiPlayers.Count > 0)
            {
                progressCallback?.Invoke("Syncing local database...");
                await SyncWithLocalDb(apiPlayers);
            }

            progressCallback?.Invoke("Sorting players...");
            return (await _database.GetAllRecordsAsync<PlayerDB>()).OrderByDescending(x => x.PointsWithBonus).ToList();
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

            var lgtfPath = Path.Combine(FileSystem.AppDataDirectory, "lgtf.sqlite");
            var lgtfDb = new SQLiteAsyncConnection(lgtfPath);

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

            // ── lgtf.sqlite: players table for opponent name lookups ──────────
            var lgtfPlayers = await lgtfDb.QueryAsync<LgtfPlayer>("SELECT id, name, surname FROM players");
            var lgtfPlayersById = lgtfPlayers.ToDictionary(p => p.id, p => p);

            // ── lgtf.sqlite: games and competitions for this player ───────────
            var lgtfGames = await lgtfDb.QueryAsync<LgtfGame>(
                "SELECT * FROM games WHERE player1_id = ? OR player2_id = ?",
                playerMe.Id, playerMe.Id);

            var competitionIds = lgtfGames.Select(g => g.competition_id).Distinct().ToList();

            var lgtfCompetitions = await lgtfDb.QueryAsync<LgtfCompetition>(
                $"SELECT * FROM competitions WHERE id IN ({string.Join(",", competitionIds)})");

            // Deduplicate games (same logic as before)
            var uniqueGames = lgtfGames
                .GroupBy(g => new { g.competition_id, g.player1_id, g.player2_id, g.player1_sets, g.player2_sets })
                .Select(g => g.First())
                .ToList();

            var competitionsSorted = lgtfCompetitions
                .OrderBy(c => DateTime.Parse(c.start_date))
                .ToList();

            var competitionsToInsert = competitionsSorted
                .Where(c => !existingTournamentDates.Contains(
                    DateOnly.FromDateTime(DateTime.Parse(c.start_date))))
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
                var tournamentDate = DateTime.Parse(competition.start_date);

                progress.Report($"Importing {competition.name}...");

                var tournamentName = "";
                if (competition.places == "" || competition.places == null)
                {
                    tournamentName = competition.name;
                }
                else
                {
                    tournamentName = $"{competition.name} ({competition.places})";
                }

                var tournament = new Tournament
                {
                    Name                    = tournamentName,
                    Coefficient             = competition.coef.ToString(CultureInfo.InvariantCulture),
                    Date                    = tournamentDate,
                    TournamentPlayerId      = playerMe.Id,
                    TournamentPlayerName    = playerMe.Name,
                    TournamentPlayerSurname = playerMe.Surname
                };

                await _database.SaveAsync(tournament);

                var competitionGames = uniqueGames
                    .Where(g => g.competition_id == competition.id)
                    .ToList();

                foreach (var lgtfGame in competitionGames)
                {
                    bool isMePlayer1 = lgtfGame.player1_id == playerMe.Id;
                    int  opponentId  = isMePlayer1 ? lgtfGame.player2_id : lgtfGame.player1_id;

                    // ── Points and age — read straight from the pre-filled DB columns ──
                    int myPoints           = isMePlayer1 ? lgtfGame.player1_points          : lgtfGame.player2_points;
                    int myPointsWithBonus  = isMePlayer1 ? lgtfGame.player1_PointsWithBonus : lgtfGame.player2_PointsWithBonus;
                    int myAge              = isMePlayer1 ? lgtfGame.player1_age             : lgtfGame.player2_age;
                    int myPlace            = isMePlayer1 ? lgtfGame.player1_place           : lgtfGame.player2_place;

                    int oppPoints          = isMePlayer1 ? lgtfGame.player2_points          : lgtfGame.player1_points;
                    int oppPointsWithBonus = isMePlayer1 ? lgtfGame.player2_PointsWithBonus : lgtfGame.player1_PointsWithBonus;
                    int oppAge             = isMePlayer1 ? lgtfGame.player2_age             : lgtfGame.player1_age;
                    int oppPlace           = isMePlayer1 ? lgtfGame.player2_place           : lgtfGame.player1_place;

                    // ── Foreign status — opponent is foreign if not in app PlayerDB ──
                    string oppKeyName = isMePlayer1 ? lgtfGame.player2_keyName : lgtfGame.player1_keyName;
                    bool   isForeign  = string.IsNullOrEmpty(oppKeyName) ||
                                        !databasePlayersByKey.ContainsKey(oppKeyName);

                    // ── Opponent name — from lgtf.sqlite players table ────────────
                    lgtfPlayersById.TryGetValue(opponentId, out var lgtfOpponent);
                    string oppName    = lgtfOpponent?.name    ?? "";
                    string oppSurname = lgtfOpponent?.surname ?? "";

                    var gameEntity = new Game
                    {
                        MyPoints                = myPoints,
                        MyName                  = playerMe.Name,
                        MySurname               = playerMe.Surname,
                        OpponentPoints          = oppPoints,
                        Name                    = oppName,
                        Surname                 = oppSurname,
                        MySets                  = isMePlayer1 ? lgtfGame.player1_sets : lgtfGame.player2_sets,
                        OpponentSets            = isMePlayer1 ? lgtfGame.player2_sets : lgtfGame.player1_sets,
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

        private async Task SyncWithLocalDb(List<PlayerDB> apiPlayers)
        {
            await _database.BulkUpsertPlayersAsync(apiPlayers);
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

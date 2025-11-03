using RankingApp.Models;
using RankingApp.Data_Storage;
using System.Globalization;

namespace RankingApp.Services
{
    public class PlayerService(DatabaseService database, PlayerReposotoryWithDate repositoryWithDate)
    {
        private readonly DatabaseService _database = database;
        private readonly PlayerReposotoryWithDate _repositoryWithDate = repositoryWithDate;
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
            return (await _database.GetPlayersAsync()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        public async Task<List<PlayerDB>> GetPlayersFromDbAsync()
        {
            return (await _database.GetPlayersAsync()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        private async Task SyncWithLocalDb(List<PlayerDB> apiPlayers)
        {
            await _database.BulkUpsertPlayersAsync(apiPlayers);
        }

        public async Task FillDatabaseWithOldRankingsUntilIdChangeAsync(Action<string>? statusCallback = null)
        {
            int startYear = 2014;
            int endYear = 2025;
            int endMonth = 9;

            var allPlayerTuples = new List<(PlayerDB Player, int SyncYear, int SyncMonth)>();

            for (int year = startYear; year <= endYear; year++)
            {
                int startMonth = 1;
                int monthLimit = (year == endYear) ? endMonth : 1;

                for (int month = startMonth; month <= monthLimit; month++)
                {
                    string dateString = $"{year}-{month:D2}";
                    statusCallback?.Invoke($"Fetching {dateString} data…");

                    try
                    {
                        var players = await _repositoryWithDate.GetPlayersAsync(dateString, true);
                        if (players != null && players.Count > 0)
                        {
                            allPlayerTuples.AddRange(players.Select(p => (Player: p, SyncYear: year, SyncMonth: month)));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Failed to fetch players for {dateString}: {ex.Message}");
                    }

                    await Task.Delay(200);
                }
            }

            var distinctPlayers = allPlayerTuples
                .OrderBy(x => x.SyncYear)
                .ThenBy(x => x.SyncMonth)
                .GroupBy(x => x.Player.Id)
                .Select(g => g.Last().Player)
                .ToList();

            await _database.BulkUpsertPlayersAsync(distinctPlayers);
        }

        private async Task UpdateAppDataWithDate(string dateString)
        {
            var appData = await _database.GetAppDataAsync();
            if (DateTime.TryParseExact(dateString, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                appData.CurrentYear = dt.Year;
                appData.CurrentMonth = dt.Month;
                await _database.SaveAppDataAsync(appData);
            }
        }

        public async Task<AppData> GetAppDataAsync()
        {
            return await _database.GetAppDataAsync();
        }

        public async Task SaveAppDataAsync(AppData appData)
        {
            await _database.SaveAppDataAsync(appData);
        }

        public async Task DeleteAllPlayersInDatabse()
        {
            await _database.DeletePlayersAsync();
        }

        public async Task UpdateAppDefaultPlayer(string? name, string? surname)
        {
            var appData = await GetAppDataAsync();

            if ( name != "" || surname != "")
            {
                var players = await _database.GetPlayersAsync();
                var currentPlayer = players.Where(p =>  p.Name == name && p.Surname == surname).FirstOrDefault();
                appData.Id = currentPlayer != null ? currentPlayer.Id : 0;
                await SaveAppDataAsync(appData);
            }
        }

        private async Task UpdateOctoberWithIdReassignmentAsync(Action<string>? statusCallback = null)
        {
            string dateString = "2025-10";
            statusCallback?.Invoke($"Processing October 2025 (handling ID changes)…");

            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(dateString, false);
            if (apiPlayers == null || apiPlayers.Count == 0)
            {
                statusCallback?.Invoke("⚠️ No API players found for October 2025.");
                statusCallback?.Invoke("Processing Finished.");
                return;
            }

            var dbPlayers = await _database.GetPlayersAsync();
            int nextNewId = 20000;

            while (dbPlayers.Any(p => p.Id == nextNewId))
                nextNewId++;

            var idUpdates = new List<(int oldId, int newId)>();

            foreach (var apiPlayer in apiPlayers)
            {
                int apiId = apiPlayer.Id;

                var match = dbPlayers.FirstOrDefault(p =>
                    string.Equals(p.Name, apiPlayer.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.Surname, apiPlayer.Surname, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    if (apiId != match.Id)
                    {
                        var occupant = dbPlayers.FirstOrDefault(p => p.Id == apiId);
                        if (occupant != null && occupant.Id != match.Id)
                        {
                            int assignedNewId = nextNewId++;
                            idUpdates.Add((occupant.Id, assignedNewId));
                            occupant.Id = assignedNewId;
                            statusCallback?.Invoke($"ID changed for {occupant.Name} {occupant.Surname} -> {assignedNewId}");
                        }

                        idUpdates.Add((match.Id, apiId));
                        match.Id = apiId;
                    }

                    match.Place = apiPlayer.Place;
                    match.Points = apiPlayer.Points;
                    match.PointsWithBonus = apiPlayer.PointsWithBonus;
                    match.BirthDate = apiPlayer.BirthDate ?? "";
                    apiPlayer.Id = match.Id;
                }
                else
                {
                    var occupant = dbPlayers.FirstOrDefault(p => p.Id == apiId);
                    if (occupant != null)
                    {
                        int assignedNewId = nextNewId++;
                        idUpdates.Add((occupant.Id, assignedNewId));
                        occupant.Id = assignedNewId;
                        statusCallback?.Invoke($"ID changed for {occupant.Name} {occupant.Surname} -> {assignedNewId}");
                    }
                }
            }

            if (idUpdates.Count > 0)
            {
                 statusCallback?.Invoke($"Applying {idUpdates.Count} ID updates in batch...");
                 await _database.BatchUpdatePlayerIdsAsync(idUpdates);
            }

            statusCallback?.Invoke("💾 Saving updated October 2025 players to database...");
            await _database.BulkUpsertPlayersAsync(apiPlayers);
        }

        public async Task FillDatabaseWithNewRankingsAfterIdChangeAsync(Action<string>? statusCallback = null)
        {
            try
            {
                int startYear = 2025;
                int startMonth = 11;

                var currentDate = DateTime.Now;
                int endYear = currentDate.Year;
                int endMonth = currentDate.Month;

                var allPlayerTuples = new List<(PlayerDB Player, int SyncYear, int SyncMonth)>();

                for (int year = startYear; year <= endYear; year++)
                {
                    int monthStart = (year == startYear) ? startMonth : 1;
                    int monthEnd = (year == endYear) ? endMonth : 12;

                    for (int month = monthStart; month <= monthEnd; month++)
                    {
                        string dateString = $"{year}-{month:D2}";
                        statusCallback?.Invoke($"Fetching {dateString} data (new API)…");

                        try
                        {
                            var players = await _repositoryWithDate.GetPlayersAsync(dateString, false);
                            if (players != null && players.Count > 0)
                            {
                                allPlayerTuples.AddRange(players.Select(p => (Player: p, SyncYear: year, SyncMonth: month)));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Failed to fetch players for {dateString}: {ex.Message}");
                        }

                        await Task.Delay(200);
                    }
                }

                var distinctPlayers = allPlayerTuples
                    .OrderBy(x => x.SyncYear)
                    .ThenBy(x => x.SyncMonth)
                    .GroupBy(x => x.Player.Id)
                    .Select(g => g.Last().Player)
                    .ToList();

                statusCallback?.Invoke("💾 Saving new post-October rankings to database…");
                await _database.BulkUpsertPlayersAsync(distinctPlayers);

                statusCallback?.Invoke("✅ New rankings (post-October) update complete!");
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"❌ Failed to update new rankings: {ex.Message}");
            }
        }

        public async Task FillDatabaseWithIdChangeTransitionAsync(Action<string>? statusCallback = null)
        {
            try
            {
                statusCallback?.Invoke("Updating players database — this may take a while…");
                await FillDatabaseWithOldRankingsUntilIdChangeAsync(statusCallback);
                await UpdateOctoberWithIdReassignmentAsync(statusCallback);
                await FillDatabaseWithNewRankingsAfterIdChangeAsync(statusCallback);
                var appData = await _database.GetAppDataAsync();
                appData.CurrentYear = DateTime.Now.Year;
                appData.CurrentMonth = DateTime.Now.Month;
                await _database.SaveAppDataAsync(appData);

                statusCallback?.Invoke("✅ Player database update complete!");
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"❌ Update failed: {ex.Message}");
            }
        }
    }
}

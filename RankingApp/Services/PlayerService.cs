using RankingApp.Models;
using RankingApp.Data_Storage;
using System.Globalization;

namespace RankingApp.Services
{
    public class PlayerService(DatabaseService database, PlayerReposotoryWithDate repositoryWithDate)
    {
        private readonly DatabaseService _database = database;
        private readonly PlayerReposotoryWithDate _repositoryWithDate = repositoryWithDate;
        public record OctoberSummary(int Matched, int DuplicatesFixed, int NewPlayers, int Total);

        public async Task<List<PlayerDB>> LoadPlayersFromApiOrDbAsync(DateTime? date = null, Action<string>? progressCallback = null)
        {
            progressCallback?.Invoke("Checking current date...");
            DateTime now = date ?? DateTime.UtcNow;
            string currentDateString = now.ToString("yyyy-MM");
            string previousDateString = now.AddMonths(-1).ToString("yyyy-MM");

            progressCallback?.Invoke($"Fetching players for {currentDateString}...");
            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(currentDateString);
            if (apiPlayers == null || apiPlayers.Count == 0)
            {
                progressCallback?.Invoke($"No data for {currentDateString}, trying {previousDateString}...");
                apiPlayers = await _repositoryWithDate.GetPlayersAsync(previousDateString);
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
                        var players = await _repositoryWithDate.GetPlayersAsync(dateString);
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

            await _database.UpsertPlayersAsync(distinctPlayers);
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

        private async Task<OctoberSummary> UpdateOctoberWithIdReassignmentAsync(Action<string>? statusCallback = null)
        {
            string dateString = "2025-10";
            statusCallback?.Invoke($"Processing October 2025 (handling ID changes)…");

            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(dateString);
            if (apiPlayers == null || apiPlayers.Count == 0)
                return new OctoberSummary(0, 0, 0, 0);

            var dbPlayers = await _database.GetPlayersAsync();
            int nextNewId = 20000;

            int matched = 0;
            int duplicatesFixed = 0;
            int newPlayers = 0;

            while (dbPlayers.Any(p => p.Id == nextNewId))
                nextNewId++;

            foreach (var apiPlayer in apiPlayers)
            {
                int apiId = apiPlayer.Id;

                var match = dbPlayers.FirstOrDefault(p =>
                    string.Equals(p.Name, apiPlayer.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.Surname, apiPlayer.Surname, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    matched++;

                    int oldId = match.Id;

                    if (apiId != match.Id)
                    {
                        var occupant = dbPlayers.FirstOrDefault(p => p.Id == apiId);
                        if (occupant != null && occupant.Id != match.Id)
                        {
                            int assignedNewId = nextNewId;
                            nextNewId++;
                            await _database.UpdatePlayerIdAsync(occupant.Id, assignedNewId);
                            occupant.Id = assignedNewId;
                            duplicatesFixed++;
                            statusCallback?.Invoke($"⚠️ ID collision: moved {occupant.Name} {occupant.Surname} -> {assignedNewId}");
                        }

                        await _database.UpdatePlayerIdAsync(match.Id, apiId);
                        match.Id = apiId;
                    }

                    apiPlayer.Id = match.Id;
                }
                else
                {
                    newPlayers++;

                    var occupant = dbPlayers.FirstOrDefault(p => p.Id == apiId);
                    if (occupant != null)
                    {
                        int assignedNewId = nextNewId;
                        nextNewId++;
                        await _database.UpdatePlayerIdAsync(occupant.Id, assignedNewId);
                        occupant.Id = assignedNewId;
                        duplicatesFixed++;
                        statusCallback?.Invoke($"⚠️ ID collision: moved {occupant.Name} {occupant.Surname} -> {assignedNewId}");
                    }
                }

                var upsertInMemory = dbPlayers.FirstOrDefault(p => p.Id == apiPlayer.Id);
                if (upsertInMemory == null)
                {
                    // add a lightweight PlayerDB instance to in-memory list to reflect upcoming insert
                    dbPlayers.Add(new PlayerDB
                    {
                        Id = apiPlayer.Id,
                        Name = apiPlayer.Name,
                        Surname = apiPlayer.Surname,
                        Place = apiPlayer.Place,
                        Points = apiPlayer.Points,
                        PointsWithBonus = apiPlayer.PointsWithBonus,
                        BirthDate = apiPlayer.BirthDate ?? ""
                    });
                }
                else
                {
                    // update fields so in-memory reflect latest
                    upsertInMemory.Name = apiPlayer.Name;
                    upsertInMemory.Surname = apiPlayer.Surname;
                    upsertInMemory.Place = apiPlayer.Place;
                    upsertInMemory.Points = apiPlayer.Points;
                    upsertInMemory.PointsWithBonus = apiPlayer.PointsWithBonus;
                    upsertInMemory.BirthDate = apiPlayer.BirthDate ?? "";
                }
            }

            statusCallback?.Invoke("💾 Saving updated October 2025 players to database...");
            await _database.UpsertPlayersAsync(apiPlayers);

            statusCallback?.Invoke($"✅ October 2025 processed — Matched: {matched}, New: {newPlayers}, Collisions fixed: {duplicatesFixed}, Total: {apiPlayers.Count}");
            return new OctoberSummary(matched, duplicatesFixed, newPlayers, apiPlayers.Count);
        }

        public async Task FillDatabaseWithIdChangeTransitionAsync(Action<string>? statusCallback = null)
        {
            try
            {
                statusCallback?.Invoke("Updating players database — this may take a while…");
                await FillDatabaseWithOldRankingsUntilIdChangeAsync(statusCallback);
                var summary = await UpdateOctoberWithIdReassignmentAsync(statusCallback);

                statusCallback?.Invoke(
                                $"October 2025 processed.\n" +
                                $"Matched existing players: {summary.Matched}\n" +
                                $"Reassigned IDs (duplicates): {summary.DuplicatesFixed}\n" +
                                $"New players added: {summary.NewPlayers}\n" +
                                $"Total players processed: {summary.Total}");

                var appData = await _database.GetAppDataAsync();
                appData.CurrentYear = 2025;
                appData.CurrentMonth = 10;
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

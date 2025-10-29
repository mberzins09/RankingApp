using RankingApp.Models;
using RankingApp.Data_Storage;
using RankingApp.Services;
using System.Globalization;

namespace RankingApp.Services
{
    public class PlayerService(DatabaseService database, PlayerReposotoryWithDate repositoryWithDate)
    {
        private readonly DatabaseService _database = database;
        private readonly PlayerReposotoryWithDate _repositoryWithDate = repositoryWithDate;

        public async Task<List<PlayerDB>> LoadPlayersFromApiOrDbAsync(DateTime? date = null)
        {
            DateTime now = date ?? DateTime.UtcNow;
            string currentDateString = now.ToString("yyyy-MM");
            string previousDateString = now.AddMonths(-1).ToString("yyyy-MM");

            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(currentDateString);
            if (apiPlayers == null || apiPlayers.Count == 0)
            {
                apiPlayers = await _repositoryWithDate.GetPlayersAsync(previousDateString);
                if (apiPlayers != null && apiPlayers.Count > 0)
                {
                    await UpdateAppDataWithDate(previousDateString);
                }
            }
            else
            {
                await UpdateAppDataWithDate(currentDateString);
            }

            if (apiPlayers != null && apiPlayers.Count > 0)
            {
                await SyncWithLocalDb(apiPlayers);
            }

            return (await _database.GetPlayersAsync()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        public async Task<List<PlayerDB>> GetPlayersFromDbAsync()
        {
            return (await _database.GetPlayersAsync()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        private async Task SyncWithLocalDb(List<PlayerDB> apiPlayers)
        {
            var dbPlayers = await _database.GetPlayersAsync();
            var appData = await _database.GetAppDataAsync();

            foreach (var apiPlayer in apiPlayers)
            {
                var existingPlayer = dbPlayers.FirstOrDefault(p => p.Id == apiPlayer.Id);

                if (existingPlayer == null)
                {
                    // Try to find by name + surname (and optionally birthdate)
                    existingPlayer = dbPlayers.FirstOrDefault(p =>
                        string.Equals(p.Name, apiPlayer.Name, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(p.Surname, apiPlayer.Surname, StringComparison.OrdinalIgnoreCase));
                }

                if (existingPlayer != null)
                {
                    // Update ID if changed
                    if (existingPlayer.Id != apiPlayer.Id)
                    {
                        existingPlayer.Id = apiPlayer.Id;
                        appData.AppUserPlayerId = existingPlayer.Id;
                        await _database.SaveAppDataAsync(appData);
                    }

                    existingPlayer.PointsChanged = apiPlayer.PointsWithBonus - existingPlayer.PointsWithBonus;
                    existingPlayer.PointsWithBonus = apiPlayer.PointsWithBonus;
                    existingPlayer.Points = apiPlayer.Points;
                    existingPlayer.Place = apiPlayer.Place == 0 ? 6000 : apiPlayer.Place;
                    existingPlayer.OverallPlace = apiPlayer.OverallPlace == 0 ? 6000 : apiPlayer.OverallPlace;

                    await _database.UpdateSimplePlayerAsync(existingPlayer);
                }
                else
                {
                    // New player
                    await _database.InsertPlayerAsync(apiPlayer);
                }
            }

            // Now mark players not found in API as inactive
            var apiIds = new HashSet<int>(apiPlayers.Select(p => p.Id));
            var inactivePlayers = dbPlayers.Where(p => !apiIds.Contains(p.Id)).ToList();

            foreach (var player in inactivePlayers)
            {
                player.Place = 6000;
                player.OverallPlace = 6000;
                await _database.UpdateSimplePlayerAsync(player);
            }

            //var apiPlayerIds = new HashSet<int>(apiPlayers.Select(p => p.Id));
            //var missingPlayers = dbPlayers.Where(dbPlayer => !apiPlayerIds.Contains(dbPlayer.Id)).ToList();

            //foreach (var player in missingPlayers)
            //{
            //    await _database.UpdatePlayerAsync(player);
            //}

            //await _database.UpsertPlayersAsync(apiPlayers);
        }

        public async Task FillDatabaseWithOldRankingsAsync()
        {
            int startYear = 2014;
            int currentYear = DateTime.UtcNow.Year;
            int currentMonth = DateTime.UtcNow.Month;
            int endYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            var tasks = new List<Task<List<(PlayerDB Player, int SyncYear)>>>();

            for (int year = startYear; year <= endYear; year++)
            {
                string dateString = $"{year}-01";
                var task = _repositoryWithDate.GetPlayersAsync(dateString)
                    .ContinueWith(t =>
                    {
                        var players = t.Result;
                        return players.Select(p => (Player: p, SyncYear: year)).ToList();
                    });

                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            var allPlayerTuples = results.SelectMany(x => x).ToList();

            var distinctPlayers = allPlayerTuples
                .OrderBy(x => x.SyncYear)
                .GroupBy(x => x.Player.Id)
                .Select(g => g.Last().Player)
                .ToList();

            await _database.UpsertPlayersAsync(distinctPlayers);
            await LoadPlayersFromApiOrDbAsync();
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
    }
}

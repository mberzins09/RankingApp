using RankingApp.Models;
using SQLite;

namespace RankingApp.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "AllP.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            _database.CreateTableAsync<PlayerDB>().Wait();
            _database.CreateTableAsync<Game>().Wait();
            _database.CreateTableAsync<Tournament>().Wait();
            _database.CreateTableAsync<AppData>().Wait();
            _database.CreateTableAsync<DoublesGame>().Wait();
        }

        public async Task<List<T>> GetAllRecordsAsync<T>() where T : Entity, new()
        {
            return await _database.Table<T>().ToListAsync();
        }

        public async Task<T?> GetByIdAsync<T>(int id) where T : Entity, new()
        {
            return await _database.Table<T>().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveAsync<T>(T entity) where T : Entity, new()
        {
            return entity.Id == 0 ? await _database.InsertAsync(entity) : await _database.UpdateAsync(entity);
        }

        public async Task<int> DeleteAsync<T>(T entity) where T : Entity, new()
        {
            return await _database.DeleteAsync(entity);
        }

        public async Task<int> DeleteAllAsync<T>() where T : Entity, new()
        {
            return await _database.DeleteAllAsync<T>();
        }

        public async Task<AppData> GetAppDataAsync()
        {
            var data = await _database.Table<AppData>().FirstOrDefaultAsync();
            if (data == null)
            {
                data = new AppData();
                await _database.InsertAsync(data);
            }

            return data;
        }

        public async Task SaveAppDataAsync(AppData appData)
        {
            await _database.InsertOrReplaceAsync(appData);
        }

        public async Task BatchUpdatePlayerIdsAsync(IEnumerable<(int oldId, int newId)> updates)
        {
            if (updates == null || !updates.Any())
                return;

            await _database.RunInTransactionAsync(transaction =>
            {
                foreach (var (oldId, newId) in updates)
                {
                    transaction.Execute("UPDATE PlayerDB SET Id = ? WHERE Id = ?", newId, oldId);
                }
            });
        }

        public async Task BulkUpsertPlayersAsync(List<PlayerDB> apiPlayers)
        {
            if (apiPlayers == null || apiPlayers.Count == 0)
                return;

            var dbPlayers = await _database.Table<PlayerDB>().ToListAsync();
            var dbById = dbPlayers.ToDictionary(p => p.Id);

            var usedIds = new HashSet<int>(dbPlayers.Select(p => p.Id));

            var toInsert = new List<PlayerDB>();
            var toUpdate = new List<PlayerDB>();

            var groups = apiPlayers.GroupBy(p => p.Id);

            foreach (var group in groups)
            {
                var incomingList = group.ToList();

                if (incomingList.Count == 1)
                {
                    var apiPlayer = incomingList[0];

                    if (dbById.TryGetValue(apiPlayer.Id, out var existing))
                    {
                        if (apiPlayer.PointsWithBonus != existing.PointsWithBonus ||
                            apiPlayer.Points != existing.Points ||
                            apiPlayer.Place != existing.Place ||
                            apiPlayer.OverallPlace != existing.OverallPlace)
                        {
                            existing.PointsChanged = apiPlayer.PointsWithBonus - existing.PointsWithBonus;
                            existing.PointsWithBonus = apiPlayer.PointsWithBonus;
                            existing.Points = apiPlayer.Points;
                            existing.Place = apiPlayer.Place;
                            existing.OverallPlace = apiPlayer.OverallPlace;
                            toUpdate.Add(existing);
                        }
                    }
                    else
                    {
                        if (usedIds.Contains(apiPlayer.Id))
                        {
                            apiPlayer.Id = GetNextAvailableId(usedIds);
                        }
                        else
                        {
                            usedIds.Add(apiPlayer.Id);
                        }
                        toInsert.Add(apiPlayer);
                    }
                }
                else
                {
                    var ordered = incomingList
                        .OrderByDescending(p => p.PointsWithBonus)
                        .ThenByDescending(p => p.Points)
                        .ToList();

                    var keeper = ordered[0];
                    if (dbById.TryGetValue(keeper.Id, out var existingKeeper))
                    {
                        if (keeper.PointsWithBonus != existingKeeper.PointsWithBonus ||
                            keeper.Points != existingKeeper.Points ||
                            keeper.Place != existingKeeper.Place ||
                            keeper.OverallPlace != existingKeeper.OverallPlace)
                        {
                            existingKeeper.PointsChanged = keeper.PointsWithBonus - existingKeeper.PointsWithBonus;
                            existingKeeper.PointsWithBonus = keeper.PointsWithBonus;
                            existingKeeper.Points = keeper.Points;
                            existingKeeper.Place = keeper.Place;
                            existingKeeper.OverallPlace = keeper.OverallPlace;
                            toUpdate.Add(existingKeeper);
                        }

                        usedIds.Add(existingKeeper.Id);
                    }
                    else
                    {
                        if (usedIds.Contains(keeper.Id))
                        {
                            keeper.Id = GetNextAvailableId(usedIds);
                        }
                        else
                        {
                            usedIds.Add(keeper.Id);
                        }
                        toInsert.Add(keeper);
                    }

                    for (int i = 1; i < ordered.Count; i++)
                    {
                        var duplicate = ordered[i];
                        duplicate.Id = GetNextAvailableId(usedIds);
                        toInsert.Add(duplicate);
                    }
                }
            }

            var apiIdsFinal = new HashSet<int>(apiPlayers.Select(p => p.Id));
            var toDeactivate = dbPlayers.Where(p => !apiIdsFinal.Contains(p.Id)).ToList();
            foreach (var player in toDeactivate)
            {
                player.Place = 6000;
                player.OverallPlace = 6000;
            }

            await _database.RunInTransactionAsync(conn =>
            {
                if (toInsert.Count > 0)
                    conn.InsertAll(toInsert, runInTransaction: false);
                if (toUpdate.Count > 0)
                    conn.UpdateAll(toUpdate, runInTransaction: false);
                if (toDeactivate.Count > 0)
                    conn.UpdateAll(toDeactivate, runInTransaction: false);
            });
        }

        public async Task AddColumnIfNotExistsAsync(string tableName, string columnName, string columnType, string defaultValue = "0")
        {
            try
            {
                await _database.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType} DEFAULT {defaultValue}");
            }
            catch (SQLiteException ex)
            {
                if (!ex.Message.Contains("duplicate column name"))
                    throw;
            }
        }

        public async Task MigrateAppDataTableAsync()
        {
            await AddColumnIfNotExistsAsync("AppData", "AppUserOldId", "INTEGER", "0");
            await AddColumnIfNotExistsAsync("AppData", "AppUserNewId", "INTEGER", "0");
        }

        public async Task MigrateGameTableAsync()
        {
            // Add new columns if not exist
            await AddColumnIfNotExistsAsync("Game", "MyPointsWithBonus", "INTEGER");
            await AddColumnIfNotExistsAsync("Game", "OpponentPointsWithBonus", "INTEGER");
            await AddColumnIfNotExistsAsync("Game", "MyAge", "INTEGER");
            await AddColumnIfNotExistsAsync("Game", "OpponentAge", "INTEGER");
            await AddColumnIfNotExistsAsync("Game", "MyPlace", "INTEGER");
            await AddColumnIfNotExistsAsync("Game", "OpponentPlace", "INTEGER");
        }

        public async Task MigratePlayerTableAsync()
        {
            await AddColumnIfNotExistsAsync("PlayerDB", "PointsChanged", "INTEGER");
        }

        public async Task UpdateGamesWithPlayerDataAsync()
        {
            var Games = await _database.Table<Game>().ToListAsync();
            var players = await _database.Table<PlayerDB>().ToListAsync();

            foreach (var game in Games)
            {
                PlayerDB? me;
                if (game.MyName == "Edgars" && game.MySurname == "Bērziņš")
                {
                    me = players.FirstOrDefault(p => p.Name == "Edgars(R)" && p.Surname == game.MySurname);
                }
                else
                {
                    me = players.FirstOrDefault(p => p.Name == game.MyName && p.Surname == game.MySurname);
                }
                var opponent = players.FirstOrDefault(p => p.Name == game.Name && p.Surname == game.Surname);

                if (me != null)
                {
                    game.MyPointsWithBonus = me.PointsWithBonus;
                    game.MyAge = me.Age;
                    game.MyPlace = me.Place;
                }
                if (opponent != null)
                {
                    game.OpponentPointsWithBonus = opponent.PointsWithBonus;
                    game.OpponentAge = opponent.Age;
                    game.OpponentPlace = opponent.Place;
                }

                await _database.UpdateAsync(game);
            }
        }

        public async Task MigrateOldDatabaseAsync()
        {
            var appDataDir = FileSystem.AppDataDirectory;

            var mainDbPath = Path.Combine(appDataDir, "AllP.db3");
            var oldDbPath = Path.Combine(appDataDir, "Data3.db3");

            if (File.Exists(oldDbPath))
            {
                var mainDb = new SQLiteAsyncConnection(mainDbPath);
                await mainDb.CreateTableAsync<Game>();
                await mainDb.CreateTableAsync<Tournament>();

                var oldDb = new SQLiteAsyncConnection(oldDbPath);

                await oldDb.CreateTableAsync<Game>();
                await oldDb.CreateTableAsync<Tournament>();

                var oldGames = await oldDb.Table<Game>().ToListAsync();
                var oldTournaments = await oldDb.Table<Tournament>().ToListAsync();

                foreach (var game in oldGames)
                    await mainDb.InsertOrReplaceAsync(game);

                foreach (var tournament in oldTournaments)
                    await mainDb.InsertOrReplaceAsync(tournament);

                File.Delete(oldDbPath);
            }
        }

        public async Task RunAllMigrationsAsync()
        {
            var appData = await GetAppDataAsync();

            if (!appData.GamesIsUpdated)
            {
                await MigrateOldDatabaseAsync();
                await MigrateGameTableAsync();
                await UpdateGamesWithPlayerDataAsync();

                appData.GamesIsUpdated = true;
                await SaveAppDataAsync(appData);
            }

            await MigrateAppDataTableAsync();
        }

        private int GetNextAvailableId(HashSet<int> usedIds, int start = 20000)
        {
            int candidate = Math.Max(start, usedIds.Any() ? usedIds.Max() + 1 : start);
            while (usedIds.Contains(candidate))
            {
                candidate++;
            }
            usedIds.Add(candidate);
            return candidate;
        }
    }
}

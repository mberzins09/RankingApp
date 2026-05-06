using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using SQLite;

namespace RankingApp.Core.Services
{
    public class DatabaseService : IDatabaseService
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

        public Task<PlayerDB?> GetPlayerByKeyAsync(string key)
        {
            return _database.Table<PlayerDB?>().FirstOrDefaultAsync(p => p.KeyName == key);
        }

        public Task DeleteGamesForTournamentAsync(int tournamentId)
        {
            return _database.ExecuteAsync("DELETE FROM Game WHERE TournamentId = ?", tournamentId);
        }

        public Task DeleteDoublesForTournamentAsync(int tournamentId)
        {
            return _database.ExecuteAsync("DELETE FROM DoublesGame WHERE TournamentId = ?", tournamentId);
        }

        public async Task<Dictionary<int, int>> GetGamePointSumsAsync()
        {
            var sums = new Dictionary<int, int>();

            var games = await _database.Table<Game>().ToListAsync();
            foreach (var g in games)
            {
                if (g.TournamentId == 0) continue;

                var diff = g.RatingDifference;
                if (sums.ContainsKey(g.TournamentId))
                    sums[g.TournamentId] += diff;
                else
                    sums[g.TournamentId] = diff;
            }

            return sums;
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

        public async Task BulkUpsertPlayersAsync(List<PlayerDB> apiPlayers)
        {
            if (apiPlayers == null || apiPlayers.Count == 0)
                return;

            var dbPlayers = await _database.Table<PlayerDB>().ToListAsync();
            var dbByKey = dbPlayers.ToDictionary(p => p.KeyName);

            var apiKeys = new HashSet<string>();

            var toInsert = new List<PlayerDB>();
            var toUpdate = new List<PlayerDB>();

            foreach (var apiPlayer in apiPlayers)
            {
                if (string.IsNullOrEmpty(apiPlayer.KeyName))
                {
                    continue;
                }

                apiKeys.Add(apiPlayer.KeyName);

                if (dbByKey.TryGetValue(apiPlayer.KeyName, out var existing))
                {
                    existing.PointsChanged = apiPlayer.PointsWithBonus - existing.PointsWithBonus;
                    existing.Points = apiPlayer.Points;
                    existing.PointsWithBonus = apiPlayer.PointsWithBonus;
                    existing.Place = apiPlayer.Place;
                    existing.OverallPlace = apiPlayer.OverallPlace;
                    existing.Gender = apiPlayer.Gender;
                    existing.IsActive = true;
                    existing.NewId = apiPlayer.NewId;

                    toUpdate.Add(existing);
                }
                else
                {
                    apiPlayer.IsActive = true;
                    toInsert.Add(apiPlayer);
                    dbPlayers.Add(apiPlayer);
                    dbByKey[apiPlayer.KeyName] = apiPlayer;
                }
            }

            foreach (var dbPlayer in dbPlayers)
            {
                if (!apiKeys.Contains(dbPlayer.KeyName))
                {
                    dbPlayer.IsActive = false;
                    toUpdate.Add(dbPlayer);
                }
            }

            await _database.RunInTransactionAsync(conn =>
            {
                if (toInsert.Count > 0)
                    conn.InsertAll(toInsert);

                if (toUpdate.Count > 0)
                    conn.UpdateAll(toUpdate);
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

        public async Task MigratePlayerTableAsync()
        {
            await AddColumnIfNotExistsAsync("PlayerDB", "KeyName", "TEXT");
            await AddColumnIfNotExistsAsync("PlayerDB", "IsActive", "INTEGER", "0");
            await AddColumnIfNotExistsAsync("PlayerDB", "NewId", "INTEGER");
        }

        public async Task MigrateAppDataTableAsync()
        {
            await AddColumnIfNotExistsAsync("AppData", "PlayersDbMigrated", "INTEGER", "0");
            await AddColumnIfNotExistsAsync("AppData", "AppUserKeyName", "Text");
            await AddColumnIfNotExistsAsync("AppData", "RemindRankingUpdate", "INTEGER", "0");
        }

        public async Task MigrateExternalIdsAsync()
        {
            await AddColumnIfNotExistsAsync("Tournament", "ExternalTournamentId", "INTEGER", "0");
            await AddColumnIfNotExistsAsync("Game", "ExternalGameId", "INTEGER", "0");
        }

        public async Task MigrateDatabaseAsync()
        {
            var appData = await GetAppDataAsync();

            var refDbPath = Path.Combine(FileSystem.AppDataDirectory, "lgtf.sqlite");

            if (!File.Exists(refDbPath))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("lgtf.sqlite");
                using var fs = File.Create(refDbPath);
                await stream.CopyToAsync(fs);
            }
            else
            {
                // Upgrade stale copy if DbFixer columns are missing
                await EnsureLgtfDbIsUpdatedAsync(refDbPath);
            }

            if (appData.PlayersDbMigrated)
            { return; }

            await MigratePlayerTableAsync();
            await MigrateAppDataTableAsync();

            var referenceDb = new SQLiteAsyncConnection(refDbPath);

            var refPlayers = await referenceDb.Table<PlayerDB>().ToListAsync();
            refPlayers = refPlayers.OrderBy(p => p.Id).ToList();
            await _database.ExecuteAsync("DELETE FROM PlayerDB");
            await _database.ExecuteAsync("DELETE FROM sqlite_sequence WHERE name = 'PlayerDB'");

            await _database.RunInTransactionAsync(tran =>
            {
                foreach (var p in refPlayers)
                {
                    tran.Execute(@"
            INSERT INTO PlayerDB
            (Id, Name, Surname, BirthDate, Gender, Place, OverallPlace,
             Points, PointsWithBonus, IsActive, NewId, KeyName)
            VALUES
            (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        p.Id,
                        p.Name,
                        p.Surname,
                        p.BirthDate,
                        p.Gender,
                        p.Place,
                        p.OverallPlace,
                        p.Points,
                        p.PointsWithBonus,
                        p.IsActive,
                        p.NewId,
                        p.KeyName
                    );
                }
            });

            int maxId = refPlayers.Max(p => p.Id);

            await _database.ExecuteAsync(
                "INSERT OR REPLACE INTO sqlite_sequence (name, seq) VALUES ('PlayerDB', ?)",
                maxId
            );

            await FixAppUserPlayerIdAsync(appData);
            
            appData.PlayersDbMigrated = true;
            await SaveAppDataAsync(appData);

            await _database.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS idx_playerdb_keyname ON PlayerDB(KeyName)");
        }

        private async Task FixAppUserPlayerIdAsync(AppData appData)
        {
            var players = await _database.Table<PlayerDB>().ToListAsync();

            var match = players.FirstOrDefault(p =>
                p.Id == appData.AppUserPlayerId);

            if (match != null)
            {
                appData.AppUserPlayerId = match.Id;
                appData.AppUserOldId = match.Id;
                appData.AppUserKeyName = match.KeyName;
                appData.AppUserNewId = match.NewId;
                await SaveAppDataAsync(appData);
            }
        }

        private async Task EnsureLgtfDbIsUpdatedAsync(string lgtfPath)
        {
            bool needsUpdate = false;
            var checkDb = new SQLiteAsyncConnection(lgtfPath);

            try
            {
                await checkDb.ExecuteScalarAsync<int>(
                    "SELECT ranking_data_filled FROM games LIMIT 1");
            }
            catch
            {
                // Column missing → stale copy
                needsUpdate = true;
            }
            finally
            {
                // MUST close before File.Delete — Android locks open files
                await checkDb.CloseAsync();
            }

            if (!needsUpdate)
                return;

            if (File.Exists(lgtfPath))
                File.Delete(lgtfPath);

            using var stream = await FileSystem.OpenAppPackageFileAsync("lgtf.sqlite");
            using var fs = File.Create(lgtfPath);
            await stream.CopyToAsync(fs);
        }
    }
}

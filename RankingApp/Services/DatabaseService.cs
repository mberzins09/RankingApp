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

        public async Task<List<PlayerDB>> GetPlayersAsync()
        {
            return await _database.Table<PlayerDB>().ToListAsync();
        }

        public async Task<PlayerDB> GetPlayerAsync(int id)
        {
            var player = await _database.Table<PlayerDB>().Where(x => x.Id == id).FirstOrDefaultAsync();

            return player;
        }

        public async Task<List<Game>> GetGamesAsync()
        {
            return await _database.Table<Game>().ToListAsync();
        }

        public async Task<List<Tournament>> GetTournamentsAsync()
        {
            return await _database.Table<Tournament>().ToListAsync();
        }

        public async Task<Game> GetGameAsync(int id)
        {
            return await _database.Table<Game>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Tournament> GetTournamentAsync(int id)
        {
            return await _database.Table<Tournament>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveGameAsync(Game game)
        {
            return (game.Id != 0) ? await _database.UpdateAsync(game) : await _database.InsertAsync(game);
        }

        public async Task<int> SaveTournamentAsync(Tournament tournament)
        {
            return (tournament.Id != 0) ? await _database.UpdateAsync(tournament) : await _database.InsertAsync(tournament);
        }

        public async Task<int> DeleteGameAsync(Game game)
        {
            return await _database.DeleteAsync(game);
        }

        public async Task<int> DeleteTournamentAsync(Tournament tournament)
        {
            return await _database.DeleteAsync(tournament);
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

        public async Task<List<DoublesGame>> GetDoublesGamesAsync()
        {
            return await _database.Table<DoublesGame>().ToListAsync();
        }

        public async Task<DoublesGame> GetDoublesGameAsync(int id)
        {
            return await _database.Table<DoublesGame>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveDoublesGameAsync(DoublesGame doublesGame)
        {
            return (doublesGame.Id != 0) ? await _database.UpdateAsync(doublesGame) : await _database.InsertAsync(doublesGame);
        }

        public async Task<int> DeleteDoublesGameAsync(DoublesGame doublesGame)
        {
            return await _database.DeleteAsync(doublesGame);
        }

        public async Task UpdatePlayerAsync(PlayerDB player)
        {
            var existingPlayer = await _database.Table<PlayerDB>()
                    .Where(p => p.Id == player.Id)
                    .FirstOrDefaultAsync();
            if (existingPlayer != null)
            {
                existingPlayer.Place = 6000;
                existingPlayer.OverallPlace = 6000;
                await _database.UpdateAsync(existingPlayer);
            }
        }

        public async Task<int> UpdatePlayerIdAsync(int oldId, int newId)
        {
            var exists = await _database.Table<PlayerDB>().Where(p => p.Id == newId).FirstOrDefaultAsync();
            if (exists != null)
            {
                return 0;
            }

            var rows = await _database.ExecuteAsync("UPDATE PlayerDB SET Id = ? WHERE Id = ?", newId, oldId);
            return rows;
        }

        public async Task UpsertPlayersAsync(List<PlayerDB> players)
        {
            foreach (var player in players)
            {
                var existingPlayer = await _database.Table<PlayerDB>()
                    .Where(p => p.Id == player.Id)
                    .FirstOrDefaultAsync();

                if (existingPlayer != null)
                {
                    existingPlayer.PointsChanged = player.PointsWithBonus - existingPlayer.PointsWithBonus;

                    existingPlayer.PointsWithBonus = player.PointsWithBonus;
                    existingPlayer.Points = player.Points;
                    existingPlayer.Place = player.Place;
                    existingPlayer.OverallPlace = player.OverallPlace;

                    await _database.UpdateAsync(existingPlayer);
                }
                else
                {
                    await _database.InsertAsync(player);
                }
            }
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

        public async Task<int> DeletePlayersAsync()
        {
            return await _database.DeleteAllAsync<PlayerDB>();
        }

        public async Task BulkUpsertPlayersAsync(List<PlayerDB> apiPlayers)
        {
            if (apiPlayers == null || apiPlayers.Count == 0)
                return;

            var dbPlayers = await _database.Table<PlayerDB>().ToListAsync();
            var dbById = dbPlayers.ToDictionary(p => p.Id);

            // Track used IDs (existing DB IDs)
            var usedIds = new HashSet<int>(dbPlayers.Select(p => p.Id));

            var toInsert = new List<PlayerDB>();
            var toUpdate = new List<PlayerDB>();

            // Group incoming players by Id to detect duplicates within apiPlayers
            var groups = apiPlayers.GroupBy(p => p.Id);

            foreach (var group in groups)
            {
                var incomingList = group.ToList();

                if (incomingList.Count == 1)
                {
                    var apiPlayer = incomingList[0];

                    // If this id already exists in DB and DB record is present, treat as update candidate
                    if (dbById.TryGetValue(apiPlayer.Id, out var existing))
                    {
                        // update only if fields changed
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
                        // id not in DB; ensure it doesn't collide with already allocated ids (from other groups)
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
                    // Duplicate id found in apiPlayers
                    // Choose one to keep the original id (highest PointsWithBonus), reassign others
                    var ordered = incomingList
                        .OrderByDescending(p => p.PointsWithBonus)
                        .ThenByDescending(p => p.Points)
                        .ToList();

                    // First (best) player: try to keep original id if not used; otherwise assign new id
                    var keeper = ordered[0];
                    if (dbById.TryGetValue(keeper.Id, out var existingKeeper))
                    {
                        // If DB already has that id -> treat keeper as update candidate (merge into DB record)
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

                        // Mark the id as used
                        usedIds.Add(existingKeeper.Id);
                    }
                    else
                    {
                        // Keeper id is not in DB. If usedIds already contains it (from earlier reassignments), we must allocate new id.
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

                    // Others in this duplicate group: assign new IDs
                    for (int i = 1; i < ordered.Count; i++)
                    {
                        var duplicate = ordered[i];
                        duplicate.Id = GetNextAvailableId(usedIds);
                        toInsert.Add(duplicate);
                    }
                }
            }

            // Deactivate players missing from API
            var apiIdsFinal = new HashSet<int>(apiPlayers.Select(p => p.Id));
            var toDeactivate = dbPlayers.Where(p => !apiIdsFinal.Contains(p.Id)).ToList();
            foreach (var player in toDeactivate)
            {
                player.Place = 6000;
                player.OverallPlace = 6000;
            }

            // Perform DB operations inside a single transaction and await it
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

                // Ensure old DB has tables
                await oldDb.CreateTableAsync<Game>();
                await oldDb.CreateTableAsync<Tournament>();

                var oldGames = await oldDb.Table<Game>().ToListAsync();
                var oldTournaments = await oldDb.Table<Tournament>().ToListAsync();

                foreach (var game in oldGames)
                    await mainDb.InsertOrReplaceAsync(game);

                foreach (var tournament in oldTournaments)
                    await mainDb.InsertOrReplaceAsync(tournament);

                // Optional: delete old DB
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

        // Helper: find next available id (starts at 20000 if lower ids are free)
        private int GetNextAvailableId(HashSet<int> usedIds, int start = 20000)
        {
            // Prefer an id >= start, but if there are used ids above start, pick max+1
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

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
        }
    }
}

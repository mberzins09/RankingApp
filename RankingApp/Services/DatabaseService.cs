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
    }
}

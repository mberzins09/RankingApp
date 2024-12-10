using RankingApp.Models;
using SQLite;

namespace RankingApp.Services
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Players.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            _database.CreateTableAsync<PlayerDB>().Wait();
        }

        public async Task<List<PlayerDB>> GetPlayersAsync()
        {
            return await _database.Table<PlayerDB>().ToListAsync();
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

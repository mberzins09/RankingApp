using SQLite;
using RankingApp.Models;

namespace RankingApp
{
    public class RankingAppsDatabase
    {
        private SQLiteAsyncConnection DatabaseGames;
        private SQLiteAsyncConnection DatabaseTournaments;
        public RankingAppsDatabase()
        {
        }

        async Task InitGame()
        {
            if (DatabaseGames is not null)
                return;

            DatabaseGames = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            var result = await DatabaseGames.CreateTableAsync<Game>();
        }

        async Task InitTournament()
        {
            if (DatabaseTournaments is not null)
                return;

            DatabaseTournaments = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            var result = await DatabaseTournaments.CreateTableAsync<Tournament>();
        }

        public async Task ClearGameDatabase()
        {
            await DatabaseGames.DeleteAllAsync<Game>();
        }

        public async Task ClearTournamentDatabase()
        {
            await DatabaseTournaments.DeleteAllAsync<Tournament>();
        }

        public async Task<List<Game>> GetGamesAsync()
        {
            await InitGame();
            return await DatabaseGames.Table<Game>().ToListAsync();
        }

        public async Task<List<Tournament>> GetTournamentsAsync()
        {
            await InitTournament();
            return await DatabaseTournaments.Table<Tournament>().ToListAsync();
        }

        public async Task<Game> GetGameAsync(int id)
        {
            await InitGame();
            return await DatabaseGames.Table<Game>()
                .Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Tournament> GetTournamentAsync(int id)
        {
            await InitTournament();
            return await DatabaseTournaments.Table<Tournament>()
                .Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveGameAsync(Game game)
        {
            await InitGame();
            if (game.Id != 0)
                return await DatabaseGames.UpdateAsync(game);
            else
                return await DatabaseGames.InsertAsync(game);
        }

        public async Task<int> SaveTournamentAsync(Tournament tournament)
        {
            await InitTournament();
            if (tournament.Id != 0)
                return await DatabaseTournaments.UpdateAsync(tournament);
            else
                return await DatabaseTournaments.InsertAsync(tournament);
        }

        public async Task<int> DeleteGameAsync(Game game)
        {
            await InitGame();
            return await DatabaseGames.DeleteAsync(game);
        }

        public async Task<int> DeleteTournamentAsync(Tournament tournament)
        {
            await InitTournament();
            return await DatabaseTournaments.DeleteAsync(tournament);
        }
    }
}

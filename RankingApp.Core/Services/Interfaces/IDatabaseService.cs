using RankingApp.Core.Models;

namespace RankingApp.Core.Services.Interfaces
{
    public interface IDatabaseService
    {
        Task<List<T>> GetAllRecordsAsync<T>() where T : Entity, new();
        Task<T?> GetByIdAsync<T>(int id) where T : Entity, new();
        Task<int> SaveAsync<T>(T entity) where T : Entity, new();
        Task<int> DeleteAsync<T>(T entity) where T : Entity, new();

        Task<AppData> GetAppDataAsync();
        Task SaveAppDataAsync(AppData appData);

        Task BulkUpsertPlayersAsync(List<PlayerDB> players);
        Task<PlayerDB?> GetPlayerByKeyAsync(string key);
        Task MigrateDatabaseAsync();
        Task MigrateExternalIdsAsync();
        Task DeleteGamesForTournamentAsync(int tournamentId);
        Task DeleteDoublesForTournamentAsync(int tournamentId);
        Task<int> DeleteAllAsync<T>() where T : Entity, new();
        Task<Dictionary<int, int>> GetGamePointSumsAsync();
    }
}

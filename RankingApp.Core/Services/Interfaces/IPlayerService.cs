using RankingApp.Core.Models;

namespace RankingApp.Core.Services.Interfaces
{
    public interface IPlayerService
    {
        Task<List<PlayerDB>> LoadPlayersFromApiOrDbAsync(DateTime? date = null, Action<string>? progressCallback = null);
        Task GetOldTournamentsAsync(IProgress<string> progress);

        Task<List<PlayerDB>> GetPlayersFromDbAsync();
        Task<List<Game>> GetGamesFromDbAsync();

        Task<Tournament?> GetTournamentAsync(int id);

        Task<AppData> GetAppDataAsync();
        Task SaveAppDataAsync(AppData appData);

        Task<PlayerDB?> GetAppDefaultPlayerAsync(AppData appData);
    }
}

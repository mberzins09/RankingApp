using RankingApp.Core.Models;

namespace RankingApp.Core.Services.Interfaces
{
    public interface IPlayerService
    {
        Task<List<PlayerDB>> LoadPlayersFromApiOrDbAsync(DateTime? date = null, Action<string>? progressCallback = null);
        Task GetOldTournamentsAsync(IProgress<string> progress);

        /// <summary>
        /// View-only ranking of any month (with PointsChanged against the month before it), from the MonthRanking table
        /// or downloaded and stored. PlayerDB is not changed. Empty when the month has no ranking.
        /// </summary>
        Task<List<PlayerDB>> GetMonthRankingViewAsync(DateTime month, Action<string>? progressCallback = null);

        /// <summary>True when the PlayerDB month (AppData) and the month before it are both in the MonthRanking table.</summary>
        Task<bool> AreCurrentRankingsStoredAsync();

        /// <summary>Downloads and stores the PlayerDB month and the month before it, and sets PlayerDB.PointsChanged from them.</summary>
        Task EnsureCurrentRankingsStoredAsync(Action<string>? progressCallback = null);

        Task<List<PlayerDB>> GetPlayersFromDbAsync();
        Task<List<Game>> GetGamesFromDbAsync();

        Task<Tournament?> GetTournamentAsync(int id);

        Task<AppData> GetAppDataAsync();
        Task SaveAppDataAsync(AppData appData);

        Task<PlayerDB?> GetAppDefaultPlayerAsync(AppData appData);
    }
}

using RankingApp.Core.Models;

namespace RankingApp.Core.Services.Interfaces
{
    public interface IPlayerRepositoryWithDate
    {
        Task<List<PlayerDB>> GetPlayersAsync(string date, bool isOldAPIBody);
    }
}

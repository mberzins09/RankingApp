using RankingApp.Core.Models;

namespace RankingApp.Core.Services.Interfaces
{
    public interface IMonthPlayersService
    {
        Task<List<PlayerDB>> GetPlayersForTournamentAsync(Tournament tournament, AppData appData, List<PlayerDB> dbPlayers);
    }
}

using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;

namespace RankingApp.Core.Services
{
    public class MonthPlayersService(IPlayerRepositoryWithDate repo) : IMonthPlayersService
    {
        private readonly IPlayerRepositoryWithDate _repo = repo;

        public async Task<List<PlayerDB>> GetPlayersForTournamentAsync(Tournament tournament, AppData appData, List<PlayerDB> dbPlayers)
        {
            var allPlayers = dbPlayers.Where(x => x.Id != tournament.TournamentPlayerId && x.Place != 0).OrderByDescending(x => x.PointsWithBonus).ToList();

            bool sameMonth = tournament.Date.Year == appData.CurrentYear && tournament.Date.Month == appData.CurrentMonth;

            if (sameMonth)
                return allPlayers;

            string dateString = tournament.Date.ToString("yyyy-MM-01");
            var apiPlayers = await _repo.GetPlayersAsync(dateString, true) ?? [];

            var inactivePlayers = allPlayers.Where(x => !x.IsActive);

            return [.. apiPlayers.Concat(inactivePlayers).OrderByDescending(x => x.PointsWithBonus)];
        }
    }
}

using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;

namespace RankingApp.Core.Services
{
    public class PlayerReposotoryWithDate(PlayerServiceWithDate dataService) : IPlayerRepositoryWithDate
    {
        private readonly PlayerServiceWithDate _dataService = dataService;
        private List<PlayerDB>? _players;

        public async Task<List<PlayerDB>> GetPlayersAsync(string date, bool isOldAPIBody)
        {
            var males = await _dataService.GetPlayersAsync("virietis", date, isOldAPIBody);
            await Task.Delay(300);

            var females = await _dataService.GetPlayersAsync("sieviete", date, isOldAPIBody);
            await Task.Delay(300);

            var malesDb = males?.Select(player => new PlayerDB()
            {
                Gender = "male",
                NewId = player.Id,
                Name = player.Name,
                Surname = player.Surname,
                Place = player.Place,
                Points = player.Points,
                PointsWithBonus = player.PointsWithBonus,
                BirthDate = player.BirthDate == null ? "" : player.BirthDate.ToString(),
                KeyName = player.KeyName
            })
                .ToList() ?? [];

            var femalesDb = females?.Select(player => new PlayerDB()
            {
                Gender = "female",
                NewId = player.Id,
                Name = player.Name,
                Surname = player.Surname,
                Place = player.Place,
                Points = player.Points,
                PointsWithBonus = player.PointsWithBonus,
                BirthDate = player.BirthDate == null ? "" : player.BirthDate.ToString(),
                KeyName = player.KeyName
            })
                .ToList() ?? [];

            _players = malesDb.Concat(femalesDb)
                .OrderByDescending(x => x.PointsWithBonus)
                .ToList();
            int place = 1;
            foreach (var p in _players)
            {
                p.OverallPlace = place;
                place++;
            }

            var player = new PlayerDB()
            {
                Gender = "Unknown",
                Name = "Unranked",
                Surname = "Player",
                Place = 10000,
                OverallPlace = 10000,
                Points = 0,
                PointsWithBonus = 0,
                KeyName = "unrankedplayer",
            };
            _players.Add(player);

            return _players;
        }
    }
}

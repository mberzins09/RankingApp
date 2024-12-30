using RankingApp.Services;
using RankingApp.Models;

namespace RankingApp.Data_Storage
{
    public class PlayerRepository(PlayerService dataService)
    {

        private readonly PlayerService _dataService = dataService;
        private List<PlayerDB> _players;

        public async Task<List<PlayerDB>> GetPlayersAsync()
        {
            var males =await _dataService.GetPlayersAsync("virietis");
            var females =await _dataService.GetPlayersAsync("sieviete");

            var malesDb = males.Select(player => new PlayerDB()
                {
                    Gender = "male",
                    Id = player.Id,
                    Name = player.Name,
                    Surname = player.Surname,
                    Place = player.Place,
                    Points = player.Points,
                    PointsWithBonus = player.PointsWithBonus,
                    BirthDate = player.BirthDate == null ? "" : player.BirthDate.ToString()
                })
                .ToList();

            var femalesDb = females.Select(player => new PlayerDB()
                {
                    Gender = "female",
                    Id = player.Id,
                    Name = player.Name,
                    Surname = player.Surname,
                    Place = player.Place,
                    Points = player.Points,
                    PointsWithBonus = player.PointsWithBonus,
                    BirthDate = player.BirthDate == null ? "" : player.BirthDate.ToString()
            })
                .ToList();

            _players = malesDb.Concat(femalesDb)
                .OrderByDescending(x=>x.PointsWithBonus)
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
                Id = 10000,
                Name = "Unranked",
                Surname = "Player",
                Place = 0,
                OverallPlace = 0,
                Points = 0,
                PointsWithBonus = 0
            };
            _players.Add(player);

            return _players;
        }
    }
}

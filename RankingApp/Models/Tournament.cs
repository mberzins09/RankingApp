using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Models
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public List<Game> Games { get; set; } = new List<Game>();

        // Calculated property to sum RatingDifferences from all games in the tournament
        public int RatingDifference => Games.Sum(game => game.RatingDifference);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Models
{
    public class TournamentHistory
    {
        public List<Tournament> Tournaments { get; set; } = new List<Tournament>();

        // Calculated property to sum RatingDifferences from all tournaments in history
        public int RatingDifference => Tournaments.Sum(tournament => tournament.RatingDifference);
    }
}

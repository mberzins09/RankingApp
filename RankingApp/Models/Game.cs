using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Models
{
    public class Game
    {
        public int GameID { get; set; }
        public Player Me { get; set; }
        public Player Opponent { get; set; }
        public int MySets { get; set; }
        public int OpponentSets { get; set; }
        public float Coefficient { get; set; }
        public bool IsWin => MySets > OpponentSets;

        // This property calculates the RatingDifference dynamically using the RatingCalculator
        public int RatingDifference => RatingCalculator.Calculate(Me.Points, Opponent.Points, IsWin, Coefficient);
    }
}

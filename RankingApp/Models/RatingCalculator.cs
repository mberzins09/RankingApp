using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Models
{
    public static class RatingCalculator
    {
        public static int Calculate(int playerPoints, int opponentPoints, bool isWin, float coefficient)
        {
            int ratingDifference = 0;
            var pointsDifference = playerPoints - opponentPoints;
            if (isWin)
            {
                ratingDifference = 1;
            }

            return ratingDifference;
        }
    }
}

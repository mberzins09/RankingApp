namespace RankingApp.Core.Models
{
    public static class GameStatisticsCalculator
    {
        public static GameStatistics Calculate(IEnumerable<IGame> games)
        {
            var list = games.ToList();

            int totalGames = list.Count;
            int wins = list.Count(g => g.IsWin);
            int losses = totalGames - wins;

            int setsWon = list.Sum(g => g.MySets ?? 0);
            int setsLost = list.Sum(g => g.OpponentSets ?? 0);
            int totalSets = setsWon + setsLost;

            var fifthSetGames = list.Where(g => (g.MySets ?? 0) + (g.OpponentSets ?? 0) == 5).ToList();
            int fifthTotal = fifthSetGames.Count;
            int fifthWins = fifthSetGames.Count(g => g.IsWin);

            return new GameStatistics
            {
                TotalGames = totalGames,
                TotalWins = wins,
                TotalLosses = losses,

                TotalSets = totalSets,
                TotalSetsWon = setsWon,
                TotalSetsLost = setsLost,

                FifthSetTotal = fifthTotal,
                FifthSetsWon = fifthWins,
                FifthSetsLost = fifthTotal - fifthWins,

                TotalGamesPercentage = totalGames > 0 ? Math.Round((double)wins / totalGames * 100, 2) : 0,
                TotalSetsPercentage = totalSets > 0 ? Math.Round((double)setsWon / totalSets * 100, 2) : 0,
                FifthSetWinPercentage = fifthTotal > 0 ? Math.Round((double)fifthWins / fifthTotal * 100, 2) : 0
            };
        }
    }
}

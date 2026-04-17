namespace RankingApp.Core.Models
{
    public class GameStatistics
    {
        public int TotalGames { get; init; }
        public int TotalWins { get; init; }
        public int TotalLosses { get; init; }

        public int TotalSets { get; init; }
        public int TotalSetsWon { get; init; }
        public int TotalSetsLost { get; init; }

        public int FifthSetTotal { get; init; }
        public int FifthSetsWon { get; init; }
        public int FifthSetsLost { get; init; }

        public double TotalGamesPercentage { get; init; }
        public double TotalSetsPercentage { get; init; }
        public double FifthSetWinPercentage { get; init; }
    }
}

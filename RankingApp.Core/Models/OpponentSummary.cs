namespace RankingApp.Core.Models
{
    /// <summary>One row of the "top opponents" list on the All Games page.</summary>
    public class OpponentSummary
    {
        public int Rank { get; set; }
        public string KeyName { get; set; } = "";
        public string Name { get; set; } = "";
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }

        public string RankDisplay => $"{Rank}.";
        public string WinsDisplay => $"{Wins} W";
        public string LossesDisplay => $"{Losses} L";
        public string GamesDisplay => Games == 1 ? "1 game" : $"{Games} games";
    }
}

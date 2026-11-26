namespace RankingApp.Core.Models
{
    /// <summary>One row of the "Friends" popup on the rankings page: the app player or a friend in the shown month's ranking.</summary>
    public class FriendRankingRow
    {
        public string KeyName { get; set; } = "";
        public string FullName { get; set; } = "";
        public bool IsMe { get; set; }

        /// <summary>False when the player is not in the shown month's ranking.</summary>
        public bool IsRanked { get; set; }

        public int Place { get; set; }
        public int Points { get; set; }
        public int PointsWithBonus { get; set; }
        public int PointsChanged { get; set; }

        public string PlaceDisplay => IsRanked && Place > 0 ? $"{Place}." : "–";
        public string PointsDisplay => IsRanked ? $"{Points} ({PointsWithBonus})" : "not ranked";
        public string ChangeDisplay => !IsRanked ? "" : PointsChanged > 0 ? $"+{PointsChanged}" : PointsChanged.ToString();
    }
}

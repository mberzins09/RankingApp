namespace RankingApp.Core.Models
{
    /// <summary>
    /// Maps to the games table in lgtf.sqlite after the DbFixer migration.
    /// Removed: score, winner (dropped columns).
    /// Added: player1_keyName, player2_keyName, player1_points, player2_points,
    ///        player1_PointsWithBonus, player2_PointsWithBonus, player1_age, player2_age.
    /// </summary>
    public class LgtfGame
    {
        public int    id            { get; set; }
        public int    competition_id { get; set; }

        public int    player1_id   { get; set; }
        public int    player2_id   { get; set; }

        public int    player1_sets { get; set; }
        public int    player2_sets { get; set; }

        // ── New columns added by DbFixer ─────────────────────────────────────
        public string player1_keyName         { get; set; } = "";
        public string player2_keyName         { get; set; } = "";
        public int    player1_points          { get; set; }
        public int    player2_points          { get; set; }
        public int    player1_PointsWithBonus { get; set; }
        public int    player2_PointsWithBonus { get; set; }
        public int    player1_age             { get; set; }
        public int    player2_age             { get; set; }
        public int    player1_place           { get; set; }
        public int    player2_place           { get; set; }
    }
}

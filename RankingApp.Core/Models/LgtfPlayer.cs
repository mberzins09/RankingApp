namespace RankingApp.Core.Models
{
    /// <summary>
    /// Lightweight read model for the players table in lgtf.sqlite.
    /// Used only for name lookups during GetOldTournamentsAsync — no write operations.
    /// </summary>
    public class LgtfPlayer
    {
        public int    id      { get; set; }
        public string name    { get; set; } = "";
        public string surname { get; set; } = "";
    }
}

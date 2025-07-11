using SQLite;

namespace RankingApp.Models
{
    public class AppData
    {
        [PrimaryKey]
        public int Id { get; set; } = 1; // Always 1, only one row
        public int AppUserPlayerId { get; set; }
        public int CurrentRanking { get; set; }
        public bool GamesIsUpdated { get; set; }
        // Add more properties as needed in the future
    }
}

using SQLite;

namespace RankingApp.Core.Models
{
    /// <summary>
    /// An event (competition-events-for-player) the app player or a friend takes part in.
    /// Stored per player KeyName, so the marks survive app restarts and failed API calls.
    /// </summary>
    public class PlayerEvent : Entity
    {
        [Indexed]
        public string KeyName { get; set; } = "";

        [Indexed]
        public int EventId { get; set; }
    }
}

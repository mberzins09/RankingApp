using SQLite;

namespace RankingApp.Core.Models
{
    /// <summary>A player marked as a friend on the rankings page. Matched by KeyName (ids differ between data sources).</summary>
    public class Friend : Entity
    {
        [Indexed]
        public string KeyName { get; set; } = "";

        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
    }
}

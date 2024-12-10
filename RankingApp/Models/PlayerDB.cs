using SQLite;

namespace RankingApp.Models
{
    public class PlayerDB
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int Place { get; set; }
        public int Points { get; set; }
        public int PointsWithBonus { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Gender { get; set; }
        public int OverallPlace { get; set; }
        public string Display => $"{Place} {Name} {Surname}";
        public string AllDisplay => $"{OverallPlace} {Name} {Surname}";
    }
}

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
        public string BirthDate { get; set; }
        public int Age => BirthDate == "" ? 0 : AgeCalculator.Calculate(BirthDate);
        public string Display => $"{Place}. {Name} {Surname} {Age} g";
        public string AllDisplay => $"{OverallPlace}. {Name} {Surname} {Age} g";
    }
}

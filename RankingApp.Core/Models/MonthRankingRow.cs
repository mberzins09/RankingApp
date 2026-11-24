using SQLite;

namespace RankingApp.Core.Models
{
    /// <summary>
    /// One player in a stored monthly LGTF ranking (table MonthRanking).
    /// A published month never changes, so once downloaded it is kept and read from here instead of the API.
    /// About 100-150 KB per month (~1500 ranked players).
    /// </summary>
    [Table("MonthRanking")]
    public class MonthRankingRow
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "ix_monthranking_month", Order = 1)]
        public int Year { get; set; }

        [Indexed(Name = "ix_monthranking_month", Order = 2)]
        public int Month { get; set; }

        public string KeyName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public string BirthDate { get; set; } = "";
        public string Gender { get; set; } = "";
        public int Place { get; set; }
        public int OverallPlace { get; set; }
        public int Points { get; set; }
        public int PointsWithBonus { get; set; }
        public int NewId { get; set; }

        public static MonthRankingRow From(PlayerDB p, int year, int month) => new()
        {
            Year = year,
            Month = month,
            KeyName = p.KeyName ?? "",
            Name = p.Name ?? "",
            Surname = p.Surname ?? "",
            BirthDate = p.BirthDate ?? "",
            Gender = p.Gender ?? "",
            Place = p.Place,
            OverallPlace = p.OverallPlace,
            Points = p.Points,
            PointsWithBonus = p.PointsWithBonus,
            NewId = p.NewId
        };

        /// <summary>A new PlayerDB every time - callers insert / change them, so they must never be shared.</summary>
        public PlayerDB ToPlayer() => new()
        {
            KeyName = KeyName,
            Name = Name,
            Surname = Surname,
            BirthDate = BirthDate,
            Gender = Gender,
            Place = Place,
            OverallPlace = OverallPlace,
            Points = Points,
            PointsWithBonus = PointsWithBonus,
            NewId = NewId
        };
    }
}

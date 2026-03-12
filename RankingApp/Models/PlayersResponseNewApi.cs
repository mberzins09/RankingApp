using System.Text.Json.Serialization;

namespace RankingApp.Models
{
    public class PlayersResponseNewApi
    {
        [JsonPropertyName("players")]
        public List<PlayerNewApi>? Players { get; set; }
    }

    public class PlayerNewApi
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("points")]
        public string Points { get; set; }

        [JsonPropertyName("points_with_bonus")]
        public string PointsWithBonus { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("surname")]
        public string Surname { get; set; }

        [JsonPropertyName("birth_date")]
        public string BirthDate { get; set; }

        [JsonPropertyName("club_name")]
        public string ClubName { get; set; }
    }
}

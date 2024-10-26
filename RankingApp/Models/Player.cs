using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RankingApp.Models
{
    public class Player
    {
        [JsonPropertyName("player_id")]
        public int PlayerId { get; set; }

        [JsonPropertyName("vieta")]
        public int Place { get; set; }

        [JsonPropertyName("points")]
        public int Points { get; set; }

        [JsonPropertyName("points_with_bonus")]
        public int PointsWithBonus { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("surname")]
        public string Surname { get; set; }

        [JsonPropertyName("dzimsanas_dat")]
        public string BirthDate { get; set; }

        [JsonPropertyName("club_name")]
        public string ClubName { get; set; }

        [JsonPropertyName("licence_end_date")]
        public string LicenceEndDate { get; set; }

        [JsonPropertyName("licence_info")]
        public string LicenceInfo { get; set; }

        [JsonPropertyName("last_played_date")]
        public string LastPlayedDate { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace RankingApp.Core.Models
{
    public class APITournamentsResponse
    {
        [JsonPropertyName("competition_events")]
        public List<APITournament>? CompetitionEvents { get; set; }
    }

    public class APITournament
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("competition")]
        public string Competition { get; set; }

        [JsonPropertyName("event_name")]
        public string EventName { get; set; }

        [JsonPropertyName("coefficient")]
        public string Coefficient { get; set; }

        [JsonPropertyName("is_season")]
        public bool IsSeason { get; set; }
    }
}

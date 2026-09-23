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

        // ── Filled only for teams events (not part of the singles API response) ──

        [JsonIgnore]
        public bool IsTeamsEvent { get; set; }

        [JsonIgnore]
        public string EndDate { get; set; } = "";

        /// <summary>"start" or "start – end" when a (season) event spans more than one day.</summary>
        [JsonIgnore]
        public string DateDisplay =>
            string.IsNullOrWhiteSpace(EndDate) || EndDate == Date ? Date : $"{Date} – {EndDate}";
    }
}

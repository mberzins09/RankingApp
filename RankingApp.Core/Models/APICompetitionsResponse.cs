using System.Text.Json.Serialization;

namespace RankingApp.Core.Models
{
    // ── competitions?year=2026&status=planned ────────────────────────────────

    public class APICompetitionsResponse
    {
        [JsonPropertyName("competitions")]
        public List<APICompetition>? Competitions { get; set; }
    }

    public class APICompetition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("events")]
        public List<APICompetitionEvent>? Events { get; set; }
    }

    public class APICompetitionEvent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = "";

        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [JsonPropertyName("ranking_coef")]
        public string RankingCoef { get; set; } = "";
    }

    /// <summary>One row of the calendar list: a single event with the name of its competition.</summary>
    public class CalendarEventItem
    {
        public int EventId { get; set; }
        public string CompetitionName { get; set; } = "";
        public string EventName { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string RankingCoef { get; set; } = "";

        /// <summary>"Competition((event name))" - as in the list design.</summary>
        public string Title => $"{CompetitionName}({EventName})";

        /// <summary>"date - coefficient".</summary>
        public string Subtitle => $"{StartDate} - {RankingCoef}";
    }

    /// <summary>One numbered row of the participants list on the event page.</summary>
    public class CalendarParticipantItem
    {
        public int Position { get; set; }
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public int Age { get; set; }
        public int PointsWithBonus { get; set; }

        /// <summary>"Name Surname(age)".</summary>
        public string Display => Age > 0 ? $"{Name} {Surname}({Age})" : $"{Name} {Surname}";
    }
}

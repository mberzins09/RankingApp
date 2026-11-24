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

    // ── competition-events?year=2026&month=11 ────────────────────────────────

    public class APICompetitionEventsResponse
    {
        [JsonPropertyName("competition_events")]
        public List<APICompetitionEventListItem>? CompetitionEvents { get; set; }
    }

    public class APICompetitionEventListItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = "";

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }

        /// <summary>Playing date of this instance (season events are listed once per playing date).</summary>
        [JsonPropertyName("effective_date")]
        public string? EffectiveDate { get; set; }

        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [JsonPropertyName("ranking_coef")]
        public string RankingCoef { get; set; } = "";

        [JsonPropertyName("competition")]
        public APICompetitionRef? Competition { get; set; }

        /// <summary>
        /// Teams event spread over more than 3 days (a season) - its results must be asked per playing date.
        /// Shorter teams events (1-3 day tournaments) are loaded whole, without playing_date.
        /// </summary>
        public bool NeedsPlayingDate
        {
            get
            {
                if (Type != "teams")
                    return false;

                var start = Services.CompetitionListBuilder.ParseDate(StartDate);
                var end = Services.CompetitionListBuilder.ParseDate(EndDate);

                return start != null && end != null && (end.Value - start.Value).TotalDays > 3;
            }
        }

        [JsonPropertyName("arena")]
        public APIArena? Arena { get; set; }
    }

    public class APICompetitionRef
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }
    }

    public class APIArena
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }
    }

    // ── List rows (not API contracts) ────────────────────────────────────────

    /// <summary>One event (sub-event of a competition) in the Calendar / Results list.</summary>
    public class CalendarEventItem
    {
        public int EventId { get; set; }
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = "";
        public string EventName { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string StartTime { get; set; } = "";
        public string RankingCoef { get; set; } = "";
        public string City { get; set; } = "";

        /// <summary>Set for multi-day teams events: results are loaded for this playing date only.</summary>
        public string PlayingDate { get; set; } = "";

        /// <summary>"Competition((event name))".</summary>
        public string Title => $"{CompetitionName}({EventName})";

        /// <summary>"date time - coefficient".</summary>
        public string Subtitle => string.IsNullOrWhiteSpace(StartTime)
            ? $"{StartDate} - {RankingCoef}"
            : $"{StartDate} {StartTime} - {RankingCoef}";
    }

    /// <summary>
    /// A competition in the Calendar / Results list. Tapping it shows / hides its events,
    /// which are inserted into the same list right below it.
    /// </summary>
    public partial class CompetitionGroupItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject, IExpandableRow
    {
        public int CompetitionId { get; set; }
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public string FirstDate { get; set; } = "";
        public string LastDate { get; set; } = "";
        public List<CalendarEventItem> Events { get; set; } = [];

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(Chevron))]
        private bool isExpanded;

        /// <summary>Results list: a competition with a single event opens that event directly (no dropdown).</summary>
        public bool OpensDirectly { get; set; }

        public string Chevron => OpensDirectly ? "›" : IsExpanded ? "▴" : "▾";

        public IEnumerable<object> ChildRows => Events;

        /// <summary>"2026-11-21 – 2026-11-22 · Ķekava · 11 events".</summary>
        public string Subtitle
        {
            get
            {
                string dates = string.IsNullOrEmpty(LastDate) || LastDate == FirstDate ? FirstDate : $"{FirstDate} – {LastDate}";
                string events = Events.Count == 1 ? "1 event" : $"{Events.Count} events";
                return string.IsNullOrWhiteSpace(City) ? $"{dates} · {events}" : $"{dates} · {City} · {events}";
            }
        }
    }

    /// <summary>One numbered row of the participants list on the event page.</summary>
    public class CalendarParticipantItem
    {
        public int Position { get; set; }
        public string Name { get; set; } = "";
        public string Surname { get; set; } = "";
        public int Age { get; set; }
        public int PointsWithBonus { get; set; }

        /// <summary>Ranking points without bonus - the rating change is calculated from these.</summary>
        public int Points { get; set; }

        /// <summary>Sum of the rating changes of all this player's games in the event. Null = not calculated (Calendar).</summary>
        public int? RatingChange { get; set; }

        /// <summary>"Name Surname(age)".</summary>
        public string Display => Age > 0 ? $"{Name} {Surname}({Age})" : $"{Name} {Surname}";

        public bool HasRatingChange => RatingChange.HasValue;
        public string RatingChangeText => RatingFormat.Change(RatingChange ?? 0);
        public bool IsRatingPositive => RatingChange > 0;
        public bool IsRatingNegative => RatingChange < 0;
    }

    public static class RatingFormat
    {
        /// <summary>"+5", "-3", "0".</summary>
        public static string Change(int value) => value > 0 ? $"+{value}" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

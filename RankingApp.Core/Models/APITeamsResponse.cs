using System.Text.Json.Serialization;

namespace RankingApp.Core.Models
{
    // ── competition-events?type=teams ─────────────────────────────────────────

    public class APITeamsEventsResponse
    {
        [JsonPropertyName("competition_events")]
        public List<APITeamsEventListItem>? CompetitionEvents { get; set; }
    }

    public class APITeamsEventListItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

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
        public string RankingCoef { get; set; } = "0";

        [JsonPropertyName("arena")]
        public APIArena? Arena { get; set; }

        [JsonPropertyName("is_season_ranking_instance")]
        public bool IsSeasonRankingInstance { get; set; }

        [JsonPropertyName("competition")]
        public APICompetitionInfo? Competition { get; set; }
    }

    public class APICompetitionInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("places")]
        public string Places { get; set; } = "";
    }

    // ── competition-event-results?competition_event_id=X[&playing_date=Y] ────

    public class APIEventResultsResponse
    {
        [JsonPropertyName("competition_event")]
        public APIEventInfo? CompetitionEvent { get; set; }

        [JsonPropertyName("nets")]
        public List<APINet>? Nets { get; set; }

        [JsonPropertyName("participants")]
        public List<APIParticipant>? Participants { get; set; }
    }

    public class APIEventInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = "";

        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [JsonPropertyName("end_date")]
        public string EndDate { get; set; } = "";

        [JsonPropertyName("ranking_coef")]
        public string RankingCoef { get; set; } = "0";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("competition")]
        public APICompetitionInfo? Competition { get; set; }
    }

    public class APIParticipant
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>Null for doubles pairs.</summary>
        [JsonPropertyName("player_id")]
        public int? PlayerId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("surname")]
        public string? Surname { get; set; }

        /// <summary>"player" or "doubles_pair".</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        /// <summary>Doubles events: the pair, e.g. { "id": 4618, "name": "Pavāre Laura / Bokāne Kate" } (surname first).</summary>
        [JsonPropertyName("doubles_pair")]
        public APIDoublesPair? DoublesPair { get; set; }

        [JsonPropertyName("birth_date")]
        public string? BirthDate { get; set; }

        [JsonPropertyName("team_name")]
        public string? TeamName { get; set; }
    }

    public class APINet
    {
        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>"groups", "single_elimination", ...</summary>
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("start_place")]
        public int? StartPlace { get; set; }

        [JsonPropertyName("end_place")]
        public int? EndPlace { get; set; }

        [JsonPropertyName("custom_name")]
        public string? CustomName { get; set; }

        [JsonPropertyName("use_custom_name")]
        public bool UseCustomName { get; set; }

        [JsonPropertyName("groups")]
        public List<APINetGroup>? Groups { get; set; }

        [JsonPropertyName("elimination_trees")]
        public List<APIEliminationTree>? EliminationTrees { get; set; }
    }

    public class APINetGroup
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("group_number")]
        public int GroupNumber { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("use_custom_name")]
        public bool UseCustomName { get; set; }

        [JsonPropertyName("players")]
        public List<APIGroupPlayer>? Players { get; set; }

        [JsonPropertyName("games")]
        public List<APINetGame>? Games { get; set; }
    }

    /// <summary>A group row. In doubles events id / name / surname are null - the pairs are only in the games.</summary>
    public class APIGroupPlayer
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("surname")]
        public string? Surname { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("points")]
        public int? Points { get; set; }
    }

    public class APIEliminationTree
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("start_place")]
        public int? StartPlace { get; set; }

        [JsonPropertyName("end_place")]
        public int? EndPlace { get; set; }

        [JsonPropertyName("rounds")]
        public List<APIRound>? Rounds { get; set; }
    }

    public class APIRound
    {
        [JsonPropertyName("round")]
        public int Round { get; set; }

        [JsonPropertyName("games")]
        public List<APINetGame>? Games { get; set; }
    }

    /// <summary>
    /// A game inside a net. For teams events the top-level game is a team match
    /// (game_type = "teams") and the real singles/doubles games are nested in <see cref="Games"/>.
    /// </summary>
    public class APINetGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("game_type")]
        public string? GameType { get; set; }

        [JsonPropertyName("player1")]
        public APIGamePlayer? Player1 { get; set; }

        [JsonPropertyName("player2")]
        public APIGamePlayer? Player2 { get; set; }

        [JsonPropertyName("player1_score")]
        public string? Player1Score { get; set; }

        [JsonPropertyName("player2_score")]
        public string? Player2Score { get; set; }

        [JsonPropertyName("player1_points")]
        public int? Player1Points { get; set; }

        [JsonPropertyName("player2_points")]
        public int? Player2Points { get; set; }

        [JsonPropertyName("winner")]
        public APIGamePlayer? Winner { get; set; }

        /// <summary>Doubles only, e.g. { "id": 1704, "name": "Nikolajs Golubevs / Anrijs Bergs" }</summary>
        /// <summary>Teams events: the two teams of a team match.</summary>
        [JsonPropertyName("player1_team")]
        public APITeamRef? Player1Team { get; set; }

        [JsonPropertyName("player2_team")]
        public APITeamRef? Player2Team { get; set; }

        [JsonPropertyName("round_nr")]
        public int? RoundNr { get; set; }

        [JsonPropertyName("player1_doubles_pair")]
        public APIDoublesPair? Player1DoublesPair { get; set; }

        [JsonPropertyName("player2_doubles_pair")]
        public APIDoublesPair? Player2DoublesPair { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>UTC, e.g. "2025-12-13T08:00:00.000000Z". Set on team matches, null on nested games.</summary>
        [JsonPropertyName("scheduled_at")]
        public string? ScheduledAt { get; set; }

        [JsonPropertyName("started_at")]
        public string? StartedAt { get; set; }

        [JsonPropertyName("ended_at")]
        public string? EndedAt { get; set; }

        [JsonPropertyName("group_id")]
        public int? GroupId { get; set; }

        [JsonPropertyName("games")]
        public List<APINetGame>? Games { get; set; }
    }

    public class APITeamRef
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class APIDoublesPair
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    // ── Parsed results (not API contracts) ───────────────────────────────────

    /// <summary>A doubles game where the app user played. Pair names are "Name Surname / Name Surname".</summary>
    public class APIDoublesGame
    {
        public int Id { get; set; }
        public string Pair1Name { get; set; } = "";
        public string Pair2Name { get; set; } = "";
        public int Pair1Sets { get; set; }
        public int Pair2Sets { get; set; }
    }

    /// <summary>The app user's games on one playing day of a teams event.</summary>
    public class TeamsDayGames
    {
        public List<APIGame> Singles { get; } = [];
        public List<APIDoublesGame> Doubles { get; } = [];
        public int Count => Singles.Count + Doubles.Count;
    }
}

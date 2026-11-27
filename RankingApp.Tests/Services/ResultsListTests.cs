using RankingApp.Core.Models;
using RankingApp.Core.Services;
using System.Text.Json;

namespace RankingApp.Tests.Services
{
    public class DoublesEventTests
    {
        // Shape of competition-event-results for a doubles event (player_id / name / surname are null)
        private const string DoublesJson = """
        {
          "competition_event": { "id": 1974, "name": "Sieviešu dubultspēles", "type": "doubles", "start_date": "2026-11-28",
            "end_date": null, "start_time": "18:30", "ranking_coef": "0.00", "competition": { "id": 856, "name": "Rīgas čempionāts" } },
          "participants": [
            { "id": 38961, "final_position": null, "name": null, "surname": null, "birth_date": null, "player_id": null,
              "type": "doubles_pair", "doubles_pair": { "id": 4618, "name": "Pavāre Laura / Bokāne Kate" } },
            { "id": 39238, "final_position": null, "name": null, "surname": null, "birth_date": null, "player_id": null,
              "type": "doubles_pair", "doubles_pair": { "id": 4658, "name": "Kučinska Elīza Paula / Kučinska Daniela" } }
          ],
          "nets": [ { "id": 5018, "order": 1, "type": "groups", "start_place": null, "end_place": null, "custom_name": null, "use_custom_name": false } ],
          "filters": { "playing_date": null }
        }
        """;

        [Fact]
        public void Deserializes_DoublesEvent()
        {
            var result = JsonSerializer.Deserialize<APIEventResultsResponse>(DoublesJson);

            Assert.NotNull(result?.CompetitionEvent);
            Assert.Equal(2, result!.Participants!.Count);
            Assert.Null(result.Participants[0].PlayerId);
            Assert.Equal("Pavāre Laura / Bokāne Kate", result.Participants[0].DoublesPair?.Name);
        }

        [Fact]
        public async Task Participants_PairsAreRankedByBothPlayersAndMarked()
        {
            var result = JsonSerializer.Deserialize<APIEventResultsResponse>(DoublesJson)!;

            var ranking = new MonthRanking
            {
                ByKey = new()
                {
                    [NameNormalizer.NormalizeKey("LauraPavāre")] = new PlayerDB { Points = 100, PointsWithBonus = 110 },
                    [NameNormalizer.NormalizeKey("KateBokāne")] = new PlayerDB { Points = 50, PointsWithBonus = 60 },
                    [NameNormalizer.NormalizeKey("Elīza PaulaKučinska")] = new PlayerDB { Points = 300, PointsWithBonus = 300 }
                }
            };

            var marks = new PlayerMarks([], [NameNormalizer.NormalizeKey("DanielaKučinska")]);

            // Pairs never touch the database (it is only used for birth dates of single players)
            var items = await EventResultsBuilder.BuildParticipantsAsync(null!, result.Participants, ranking, marks: marks);

            Assert.Equal("Kučinska Elīza Paula / Kučinska Daniela", items[0].Display);
            Assert.Equal(300, items[0].PointsWithBonus);
            Assert.Equal(PlayerMark.Friend, items[0].Mark);

            Assert.Equal("Pavāre Laura / Bokāne Kate", items[1].Display);
            Assert.Equal(170, items[1].PointsWithBonus);
            Assert.Equal(150, items[1].Points);
            Assert.Equal(PlayerMark.None, items[1].Mark);
        }
    }

    public class DoublesDrawingTests
    {
        [Theory]
        [InlineData("Laura Krūmiņa / Yvonne Fischer", "L.Krūmiņa / Y.Fischer")]
        [InlineData("Nike Reitere / Thi Thanh My Nguyen", "N.Reitere / T.Nguyen")]
        [InlineData("Solo / Anna Liepa", "Solo / A.Liepa")]
        [InlineData(null, "")]
        public void ShortPairName(string? pair, string expected) =>
            Assert.Equal(expected, EventResultsBuilder.ShortPairName(pair));

        // Doubles group as the API sends it: players have only position + points, the pairs are in the games
        private const string GroupJson = """
        {
          "id": 1, "group_number": 1, "name": null, "use_custom_name": false,
          "players": [
            { "id": null, "name": null, "surname": null, "position": 2, "points": 3 },
            { "id": null, "name": null, "surname": null, "position": 1, "points": 4 },
            { "id": null, "name": null, "surname": null, "position": 3, "points": 2 }
          ],
          "games": [
            { "id": 10, "game_type": "doubles", "player1": null, "player2": null, "winner": null,
              "player1_doubles_pair": { "id": 1, "name": "Anna Liepa / Jānis Bērziņš" },
              "player2_doubles_pair": { "id": 2, "name": "Pēteris Kalns / Ieva Ozola" },
              "player1_score": "3", "player2_score": "1", "player1_points": 2, "player2_points": 1 },
            { "id": 11, "game_type": "doubles", "player1": null, "player2": null, "winner": null,
              "player1_doubles_pair": { "id": 3, "name": "Rita Egle / Juris Bite" },
              "player2_doubles_pair": { "id": 1, "name": "Anna Liepa / Jānis Bērziņš" },
              "player1_score": "1", "player2_score": "3", "player1_points": 1, "player2_points": 2 },
            { "id": 12, "game_type": "doubles", "player1": null, "player2": null, "winner": null,
              "player1_doubles_pair": { "id": 2, "name": "Pēteris Kalns / Ieva Ozola" },
              "player2_doubles_pair": { "id": 3, "name": "Rita Egle / Juris Bite" },
              "player1_score": "3", "player2_score": "2", "player1_points": 2, "player2_points": 1 }
          ]
        }
        """;

        [Fact]
        public void GroupTable_DoublesPairsFromGames()
        {
            var group = JsonSerializer.Deserialize<APINetGroup>(GroupJson)!;
            var marks = new PlayerMarks(["janisberzins"], []);

            var table = EventResultsBuilder.BuildGroupTable("Group 1", group, marks: marks);

            Assert.Equal(["A.Liepa / J.Bērziņš", "P.Kalns / I.Ozola", "R.Egle / J.Bite"], table.Rows.Select(r => r.PlayerName));
            Assert.Equal(["4", "3", "2"], table.Rows.Select(r => r.Points));
            Assert.Equal(["1", "2", "3"], table.Rows.Select(r => r.Place));
            Assert.Equal(PlayerMark.Me, table.Rows[0].Mark);

            // Row 1 (Liepa/Bērziņš) against row 3 (Egle/Bite): won 3:1 although they were player 2 of that game
            var cell = table.Rows[0].Cells[2];
            Assert.Equal(CellOutcome.Win, cell.Outcome);
            Assert.Equal("3:1", cell.Score);
            Assert.Equal(CellOutcome.Loss, table.Rows[2].Cells[0].Outcome);
        }

        [Fact]
        public void GroupTable_TiedPairsShareThePlaceRange()
        {
            var group = JsonSerializer.Deserialize<APINetGroup>(GroupJson)!;
            group.Players = [new() { Position = 1, Points = 3 }, new() { Position = 2, Points = 3 }, new() { Position = 3, Points = 3 }];
            // A cycle (1 beats 2, 3 beats 1, 2 beats 3): every first pair wins, so everyone has 2 + 1 = 3 points
            foreach (var g in group.Games!)
                (g.Player1Points, g.Player2Points) = (2, 1);

            var table = EventResultsBuilder.BuildGroupTable("Group 1", group);

            Assert.All(table.Rows, r => Assert.Equal("1-3", r.Place));
        }

        [Fact]
        public void Bracket_ShortPairNames()
        {
            var tree = new APIEliminationTree
            {
                Rounds =
                [
                    new() { Round = 0, Games = [ new() { Id = 1, Player1DoublesPair = new() { Id = 1, Name = "Laura Krūmiņa / Yvonne Fischer" },
                        Player2DoublesPair = new() { Id = 2, Name = "Anna Liepa / Ieva Ozola" }, Player1Score = "3", Player2Score = "0" } ] }
                ]
            };

            var bracket = EventResultsBuilder.BuildBracket("Playoff", tree);

            Assert.Equal("L.Krūmiņa / Y.Fischer", bracket.Rounds[0].Matches[0].Player1);
            Assert.Equal("L.Krūmiņa / Y.Fischer", bracket.WinnerName);
        }
    }

    public class ResultsEventsTests
    {
        private static APITeamsEventListItem Teams(int id, string start, string? end, string effective, bool season, int competitionId) => new()
        {
            Id = id,
            Name = $"Teams {id}",
            Type = "teams",
            StartDate = start,
            EndDate = end,
            EffectiveDate = effective,
            IsSeasonRankingInstance = season,
            Competition = new APICompetitionInfo { Id = competitionId, Name = $"Competition {competitionId}" }
        };

        [Fact]
        public void Merges_Competitions_And_TeamsEvents()
        {
            var competitions = new List<APICompetition>
            {
                new()
                {
                    Id = 731, Name = "Mixed", Arena = new APIArena { City = "Rīga" },
                    Events =
                    [
                        new() { Id = 1, Name = "Singles", Type = "singles", StartDate = "2026-09-20" },
                        new() { Id = 2, Name = "Doubles", Type = "doubles", StartDate = "2026-09-20" },
                        new() { Id = 1684, Name = "Teams (also in teams list)", Type = "teams", StartDate = "2026-09-20" }
                    ]
                }
            };

            var teams = new List<APITeamsEventListItem>
            {
                Teams(1684, "2026-09-20", null, "2026-09-20", false, 731),
                // Season: one item per playing date, the same id
                Teams(662, "2025-12-14", "2026-05-02", "2025-12-14", true, 293),
                Teams(662, "2026-01-31", "2026-05-02", "2026-01-31", true, 293),
                Teams(662, "2026-05-02", "2026-05-02", "2026-05-02", true, 293)
            };

            var events = CompetitionListBuilder.ResultsEvents(2026, competitions, teams);

            // Teams event in both lists comes once (from the teams list), in the same competition
            Assert.Single(events, e => e.EventId == 1684);
            Assert.Equal(731, events.Single(e => e.EventId == 1684).CompetitionId);
            Assert.Contains(events, e => e.EventId == 2 && e.City == "Rīga");

            // Season: only playing dates of 2026, each asked with its playing date (last one too, although start == end)
            var season = events.Where(e => e.EventId == 662).OrderBy(e => e.StartDate).ToList();
            Assert.Equal(["2026-01-31", "2026-05-02"], season.Select(e => e.PlayingDate));
            Assert.All(season, e => Assert.Equal(293, e.CompetitionId));

            var groups = CompetitionListBuilder.Group(events, newestFirst: true);
            Assert.Equal(3, groups.Single(g => g.CompetitionId == 731).Events.Count);   // singles + doubles + teams together
            Assert.Equal(2, groups.Single(g => g.CompetitionId == 293).Events.Count);
        }
    }
}

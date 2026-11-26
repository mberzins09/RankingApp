using RankingApp.Core.Models;
using RankingApp.Core.Services;

namespace RankingApp.Tests.Services
{
    public class PlayerMarksTests
    {
        [Fact]
        public void Of_MatchesByNormalizedName_MeWinsOverFriend()
        {
            var marks = new PlayerMarks(["janisberzins"], ["peteriskalns", "janisberzins"]);

            Assert.Equal(PlayerMark.Me, marks.Of("Jānis", "Bērziņš"));
            Assert.Equal(PlayerMark.Friend, marks.Of("Pēteris", "Kalns"));
            Assert.Equal(PlayerMark.None, marks.Of("Anna", "Liepa"));
        }

        [Fact]
        public void Of_KeyWithNote_MatchesPlainTournamentName()
        {
            var marks = new PlayerMarks(["edgars(r)berzins"], []);

            Assert.Equal(PlayerMark.Me, marks.Of("Edgars", "Berzins"));
        }

        [Fact]
        public void Of_DoublesPair_TakesStrongestMember()
        {
            var marks = new PlayerMarks(["janisberzins"], ["peteriskalns"]);

            var pair = new APIDoublesPair { Name = "Pēteris Kalns / Jānis Bērziņš" };

            Assert.Equal(PlayerMark.Me, marks.Of(null, pair));
            Assert.Equal(PlayerMark.Friend, marks.Of(null, new APIDoublesPair { Name = "Anna Liepa / Pēteris Kalns" }));
        }
    }

    public class EventResultsTableTests
    {
        private static APIGamePlayer P(int id, string name, string surname) => new() { Id = id, Name = name, Surname = surname };

        private static EventLiveInfo Live(params (int GameId, int Table, string Status)[] games)
        {
            var live = new EventLiveInfo();
            foreach (var g in games)
                live.Games[g.GameId] = new LiveGameInfo(g.Table, g.Status);
            return live;
        }

        [Fact]
        public void BuildBracket_ShowsTableOnlyForUnfinishedGames_AndMarksPlayers()
        {
            var tree = new APIEliminationTree
            {
                Rounds =
                [
                    new APIRound
                    {
                        Round = 1,
                        Games =
                        [
                            new APINetGame { Id = 1, Status = "finished", Player1 = P(1, "Jānis", "Bērziņš"), Player2 = P(2, "Anna", "Liepa"),
                                Player1Score = "3", Player2Score = "1", Winner = P(1, "Jānis", "Bērziņš") },
                            new APINetGame { Id = 2, Status = "live", Player1 = P(3, "Pēteris", "Kalns"), Player2 = P(4, "Ivo", "Ozols") },
                            new APINetGame { Id = 3, Status = "pending", Player1 = P(5, "A", "B"), Player2 = P(6, "C", "D") }
                        ]
                    }
                ]
            };

            var live = Live((1, 4, "finished"), (2, 7, "live"), (3, 0, "pending"));
            var marks = new PlayerMarks(["janisberzins"], ["peteriskalns"]);

            var matches = EventResultsBuilder.BuildBracket("Playoff", tree, null, marks, live).Rounds[0].Matches;

            Assert.Null(matches[0].Table);                      // finished - no table any more
            Assert.Equal(PlayerMark.Me, matches[0].Mark1);

            Assert.Equal(7, matches[1].Table);                  // being played at table 7
            Assert.True(matches[1].IsLive);
            Assert.Equal(PlayerMark.Friend, matches[1].Mark1);

            Assert.Null(matches[2].Table);                      // pending, no table given yet (table_nr 0)
        }

        [Fact]
        public void BuildGroupTable_UnplayedCellShowsTable_AndRowColumnAreMarked()
        {
            var group = new APINetGroup
            {
                Players =
                [
                    new APIGroupPlayer { Id = 1, Name = "Jānis", Surname = "Bērziņš", Position = 1 },
                    new APIGroupPlayer { Id = 2, Name = "Anna", Surname = "Liepa", Position = 2 },
                    new APIGroupPlayer { Id = 3, Name = "Ivo", Surname = "Ozols", Position = 3 }
                ],
                Games =
                [
                    new APINetGame { Id = 10, Status = "finished", Player1 = P(1, "Jānis", "Bērziņš"), Player2 = P(2, "Anna", "Liepa"),
                        Player1Score = "3", Player2Score = "0", Winner = P(1, "Jānis", "Bērziņš") },
                    new APINetGame { Id = 11, Status = "pending", Player1 = P(2, "Anna", "Liepa"), Player2 = P(3, "Ivo", "Ozols") }
                ]
            };

            var model = EventResultsBuilder.BuildGroupTable("Group 1", group, null,
                new PlayerMarks(["janisberzins"], []), Live((10, 2, "finished"), (11, 5, "pending")));

            Assert.Equal(PlayerMark.Me, model.Rows[0].Mark);
            Assert.Equal(PlayerMark.Me, model.Rows[1].Cells[0].Mark);   // Anna vs Jānis - Jānis' column
            Assert.Null(model.Rows[0].Cells[1].Table);                  // played game

            var annaVsIvo = model.Rows[1].Cells[2];
            Assert.Equal(5, annaVsIvo.Table);
            Assert.False(annaVsIvo.IsLive);
            Assert.Equal("", annaVsIvo.Score);
            Assert.Equal(PlayerMark.None, annaVsIvo.Mark);
        }

        [Fact]
        public void LiveTablesByPlayer_OnlyGamesBeingPlayed()
        {
            var nets = new List<APINet>
            {
                new()
                {
                    Groups =
                    [
                        new APINetGroup
                        {
                            Games =
                            [
                                new APINetGame { Id = 1, Player1 = P(1, "Jānis", "Bērziņš"), Player2 = P(2, "Anna", "Liepa") },
                                new APINetGame { Id = 2, Player1 = P(3, "Ivo", "Ozols"), Player2 = P(4, "Pēteris", "Kalns") }
                            ]
                        }
                    ]
                }
            };

            var tables = EventResultsBuilder.LiveTablesByPlayer(nets, Live((1, 3, "live"), (2, 6, "pending")));

            Assert.Equal(3, tables["janisberzins"]);
            Assert.Equal(3, tables["annaliepa"]);
            Assert.False(tables.ContainsKey("ivoozols"));
        }

        [Fact]
        public void HasOpenGames_FalseWhenEverythingFinished()
        {
            var finished = new List<APINet> { new() { Groups = [new APINetGroup { Games = [new APINetGame { Status = "finished" }] }] } };
            var ongoing = new List<APINet> { new() { Groups = [new APINetGroup { Games = [new APINetGame { Status = "finished" }, new APINetGame { Status = "pending" }] }] } };

            Assert.False(EventResultsBuilder.HasOpenGames(finished));
            Assert.True(EventResultsBuilder.HasOpenGames(ongoing));
        }
    }
}

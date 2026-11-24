using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using System.Globalization;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Turns the raw competition-event-results response into ready-to-draw models:
    /// participants list, group cross tables and play-off brackets.
    /// </summary>
    public static class EventResultsBuilder
    {
        // ── Participants ────────────────────────────────────────────────

        /// <summary>
        /// Numbered by the ranking points of the given month (0 if the player is not in that ranking).
        /// With <paramref name="ratings"/> every participant also gets the sum of his rating changes in this event.
        /// </summary>
        public static async Task<List<CalendarParticipantItem>> BuildParticipantsAsync(IDatabaseService database,
            IEnumerable<APIParticipant>? participants, MonthRanking? ranking, EventRatingChanges? ratings = null)
        {
            var items = new List<CalendarParticipantItem>();

            foreach (var p in participants ?? [])
            {
                string key = NameNormalizer.NormalizeKey($"{p.Name}{p.Surname}");
                var ranked = ranking?.Find(p.Name, p.Surname);

                // Birth date: event → month ranking → local player list
                string? birth = !string.IsNullOrWhiteSpace(p.BirthDate) ? p.BirthDate : ranked?.BirthDate;

                if (string.IsNullOrWhiteSpace(birth) && key.Length > 0)
                    birth = (await database.GetPlayerByKeyAsync(key))?.BirthDate;

                items.Add(new CalendarParticipantItem
                {
                    Name = p.Name,
                    Surname = p.Surname,
                    Age = string.IsNullOrWhiteSpace(birth) ? 0 : AgeCalculator.Calculate(birth),
                    PointsWithBonus = ranked?.PointsWithBonus ?? 0,
                    Points = ranked?.Points ?? 0,
                    RatingChange = ratings == null ? null : ratings.TotalByKey.GetValueOrDefault(key)
                });
            }

            var sorted = items
                .OrderByDescending(i => i.PointsWithBonus)
                .ThenBy(i => i.Surname)
                .ThenBy(i => i.Name)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].Position = i + 1;

            return sorted;
        }

        // ── Rating changes ──────────────────────────────────────────────

        /// <summary>
        /// Rating change of both players of every finished singles game of the event, calculated with
        /// <see cref="RatingCalculator"/> from the month ranking Points (not PointsWithBonus).
        /// A player without gender (not in the ranking) → 0 for both players of that game.
        /// </summary>
        public static EventRatingChanges CalculateRatingChanges(IEnumerable<APINet>? nets, MonthRanking ranking, string coefficient)
        {
            var result = new EventRatingChanges();

            var games = AllGames(nets)
                .Where(g => g.Player1 != null && g.Player2 != null)
                .GroupBy(g => g.Id)
                .Select(g => g.First());

            foreach (var game in games)
            {
                int winner = WinnerOf(game);
                if (winner == 0)
                    continue;   // not played yet

                var p1 = ranking.Find(game.Player1!.Name, game.Player1.Surname);
                var p2 = ranking.Find(game.Player2!.Name, game.Player2.Surname);

                int change1 = 0, change2 = 0;

                if (!string.IsNullOrWhiteSpace(p1?.Gender) && !string.IsNullOrWhiteSpace(p2?.Gender))
                {
                    change1 = RatingCalculator.Calculate(p1!.Points, p2!.Points, winner == 1, coefficient);
                    change2 = RatingCalculator.Calculate(p2.Points, p1.Points, winner == 2, coefficient);
                }

                result.ByGame[game.Id] = (change1, change2);

                Add(result.TotalByKey, game.Player1, change1);
                Add(result.TotalByKey, game.Player2, change2);
            }

            return result;
        }

        /// <summary>Every game of the event: groups and play-offs, including the games nested inside team matches.</summary>
        private static IEnumerable<APINetGame> AllGames(IEnumerable<APINet>? nets)
        {
            var top = (nets ?? [])
                .SelectMany(n => (n.Groups ?? []).SelectMany(g => g.Games ?? [])
                    .Concat((n.EliminationTrees ?? []).SelectMany(t => (t.Rounds ?? []).SelectMany(r => r.Games ?? []))));

            foreach (var game in top)
            {
                yield return game;

                foreach (var inner in game.Games ?? [])
                    yield return inner;
            }
        }

        // ── Teams events ────────────────────────────────────────────────

        /// <summary>
        /// Teams event: its group "players" are not real players, so instead of a cross table
        /// the team matches are listed, each with its singles / doubles games.
        /// </summary>
        public static bool IsTeams(APIEventInfo ev) => ev.Type == "teams";

        /// <summary>
        /// All team matches of every net (stage) - groups and play-off trees - in playing order:
        /// stage by stage, inside a stage group by group / tree by tree, then round and start time.
        /// The group "players" lists of teams events are ignored (they are not the real players).
        /// </summary>
        public static List<TeamMatchItem> BuildTeamMatches(IEnumerable<APINet>? nets, EventRatingChanges? ratings)
        {
            var netList = (nets ?? []).OrderBy(n => n.Order).ToList();
            bool severalGroupNets = netList.Count(n => n.Groups is { Count: > 0 }) > 1;

            var entries = new List<(APINetGame Game, int Net, int Section, int Round, string Stage, string RoundTitle)>();

            for (int n = 0; n < netList.Count; n++)
            {
                var net = netList[n];
                int section = 0;

                foreach (var group in (net.Groups ?? []).OrderBy(g => g.GroupNumber))
                {
                    string stage = GroupTitle(net, group, severalGroupNets);

                    foreach (var game in group.Games ?? [])
                    {
                        int round = game.RoundNr ?? 0;
                        entries.Add((game, n, section, round, stage, $"Round {round + 1}"));
                    }

                    section++;
                }

                foreach (var tree in net.EliminationTrees ?? [])
                {
                    string stage = TreeTitle(tree);
                    var rounds = (tree.Rounds ?? []).OrderBy(r => r.Round).ToList();

                    for (int r = 0; r < rounds.Count; r++)
                    {
                        string roundTitle = rounds.Count > 1 && r == rounds.Count - 1 ? "Final" : $"Round {r + 1}";

                        foreach (var game in rounds[r].Games ?? [])
                            entries.Add((game, n, section, r, stage, roundTitle));
                    }

                    section++;
                }
            }

            return entries
                .Where(e => e.Game.Player1Team != null || e.Game.Player2Team != null)
                .GroupBy(e => e.Game.Id)
                .Select(g => g.First())
                .OrderBy(e => e.Net)
                .ThenBy(e => e.Section)
                .ThenBy(e => e.Round)
                .ThenBy(e => StartTime(e.Game) ?? DateTime.MaxValue)
                .ThenBy(e => e.Game.Id)
                .Select(e => BuildTeamMatch(e.Game, e.Stage, e.RoundTitle, ratings))
                .ToList();
        }

        /// <summary>Planned start, or the real start when there was no plan (group matches often have no scheduled_at).</summary>
        private static DateTime? StartTime(APINetGame game)
        {
            foreach (var value in new[] { game.ScheduledAt, game.StartedAt })
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at))
                    return at.ToLocalTime();
            }

            return null;
        }

        private static TeamMatchItem BuildTeamMatch(APINetGame match, string stage, string roundTitle, EventRatingChanges? ratings)
        {
            string score1 = match.Player1Score ?? "";
            string score2 = match.Player2Score ?? "";

            // "winner" of a team match is some player, not the team - decide by the score
            int winner = int.TryParse(score1, out int a) && int.TryParse(score2, out int b) && a != b ? (a > b ? 1 : 2) : 0;

            // "Group 1 · Round 2 · Sat 15:36" / "Playoff 1-4 · Final · Sun 13:00"
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(stage))
                parts.Add(stage);

            parts.Add(roundTitle);

            if (StartTime(match) is { } at)
                parts.Add(at.ToString("ddd HH:mm", CultureInfo.CurrentCulture));

            return new TeamMatchItem
            {
                Team1 = match.Player1Team?.Name ?? "",
                Team2 = match.Player2Team?.Name ?? "",
                Score1 = score1,
                Score2 = score2,
                Winner = winner,
                Subtitle = string.Join(" · ", parts),
                Games = (match.Games ?? []).OrderBy(g => g.Id).Select(g => BuildTeamGame(g, ratings)).ToList()
            };
        }

        private static TeamGameItem BuildTeamGame(APINetGame game, EventRatingChanges? ratings)
        {
            int winner = WinnerOf(game);

            var item = new TeamGameItem
            {
                Player1 = SideName(game.Player1, game.Player1DoublesPair),
                Player2 = SideName(game.Player2, game.Player2DoublesPair),
                IsDoubles = game.Player1 == null || game.Player2 == null,
                Winner = winner,
                Score = winner == 0 ? "–" : $"{game.Player1Score}:{game.Player2Score}"
            };

            if (ratings != null && ratings.ByGame.TryGetValue(game.Id, out var change))
            {
                item.RatingChange1 = change.Player1;
                item.RatingChange2 = change.Player2;
            }

            return item;
        }

        private static void Add(Dictionary<string, int> totals, APIGamePlayer player, int change)
        {
            string key = NameNormalizer.NormalizeKey($"{player.Name}{player.Surname}");
            if (key.Length > 0)
                totals[key] = totals.GetValueOrDefault(key) + change;
        }

        /// <summary>0 = not decided, 1 = first player won, 2 = second player won.</summary>
        private static int WinnerOf(APINetGame game)
        {
            if (game.Winner != null)
            {
                if (game.Player1 != null && game.Winner.Id == game.Player1.Id) return 1;
                if (game.Player2 != null && game.Winner.Id == game.Player2.Id) return 2;
            }

            if (int.TryParse(game.Player1Score, out int a) && int.TryParse(game.Player2Score, out int b) && a != b)
                return a > b ? 1 : 2;

            return 0;
        }

        // ── Picker options ──────────────────────────────────────────────

        public static string GroupKey(APINet net, APINetGroup group) => $"g:{net.Id}:{group.Id}";

        public static string TreeKey(APINet net, APIEliminationTree tree) => $"t:{net.Id}:{tree.Id}";

        public static string GroupTitle(APINet net, APINetGroup group, bool severalGroupNets)
        {
            string title = group.UseCustomName && !string.IsNullOrWhiteSpace(group.Name)
                ? group.Name!
                : $"Group {group.GroupNumber}";

            if (!severalGroupNets)
                return title;

            string stage = net.UseCustomName && !string.IsNullOrWhiteSpace(net.CustomName)
                ? net.CustomName!
                : $"Stage {net.Order}";

            return $"{stage} - {title}";
        }

        /// <summary>Title of one stage (= one net): "Stage 3 - Groups, places 15-19".</summary>
        public static string StageTitle(APINet net, int number, bool hasGroups, bool hasTrees)
        {
            string name = net.UseCustomName && !string.IsNullOrWhiteSpace(net.CustomName)
                ? net.CustomName!
                : $"Stage {number} - {(hasGroups && hasTrees ? "Groups + Play-offs" : hasTrees ? "Play-offs" : "Groups")}";

            string places = PlacesText(net.StartPlace, net.EndPlace);

            return places.Length > 0 ? $"{name}, places {places}" : name;
        }

        public static string TreeTitle(APIEliminationTree tree)
        {
            if (!string.IsNullOrWhiteSpace(tree.Name))
                return tree.Name!;

            return PlacesText(tree.StartPlace, tree.EndPlace) is { Length: > 0 } places
                ? $"Playoff {places}"
                : "Playoff";
        }

        public static string PlacesText(int? start, int? end)
        {
            if (start is null || start == 0)
                return "";

            return end is null || end == start ? $"{start}" : $"{start}-{end}";
        }

        // ── Group table ─────────────────────────────────────────────────

        public static GroupTableModel BuildGroupTable(string title, APINetGroup group, EventRatingChanges? ratings = null)
        {
            var games = (group.Games ?? []).Where(g => g.Player1 != null && g.Player2 != null).ToList();

            // Order of players = their place in the group; without a players list - from the games
            var players = (group.Players ?? [])
                .OrderBy(p => p.Position)
                .Select(p => (p.Id, Name: FullName(p.Name, p.Surname), Points: p.Points?.ToString() ?? "", Place: p.Position > 0 ? p.Position.ToString() : ""))
                .ToList();

            if (players.Count == 0)
            {
                players = games
                    .SelectMany(g => new[] { g.Player1!, g.Player2! })
                    .GroupBy(p => p.Id)
                    .Select(g => (g.Key, Name: FullName(g.First().Name, g.First().Surname), Points: "", Place: ""))
                    .ToList();
            }

            var model = new GroupTableModel { Title = title };

            for (int i = 0; i < players.Count; i++)
            {
                var row = new GroupTableRow
                {
                    Number = i + 1,
                    PlayerName = players[i].Name,
                    Points = players[i].Points,
                    Place = players[i].Place
                };

                for (int j = 0; j < players.Count; j++)
                {
                    if (i == j)
                    {
                        row.Cells.Add(new GroupTableCell { IsSelf = true });
                        continue;
                    }

                    row.Cells.Add(BuildCell(games, players[i].Id, players[j].Id, ratings));
                }

                model.Rows.Add(row);
            }

            return model;
        }

        /// <summary>Result of the game "me" against "opponent" as seen by "me".</summary>
        private static GroupTableCell BuildCell(List<APINetGame> games, int me, int opponent, EventRatingChanges? ratings)
        {
            var game = games.FirstOrDefault(g =>
                (g.Player1!.Id == me && g.Player2!.Id == opponent) ||
                (g.Player2!.Id == me && g.Player1!.Id == opponent));

            if (game == null)
                return new GroupTableCell();

            bool meIsFirst = game.Player1!.Id == me;

            string myScore = (meIsFirst ? game.Player1Score : game.Player2Score) ?? "";
            string oppScore = (meIsFirst ? game.Player2Score : game.Player1Score) ?? "";
            int? myPoints = meIsFirst ? game.Player1Points : game.Player2Points;

            if (string.IsNullOrEmpty(myScore) && string.IsNullOrEmpty(oppScore))
                return new GroupTableCell();

            CellOutcome outcome = CellOutcome.None;

            if (game.Winner != null)
                outcome = game.Winner.Id == me ? CellOutcome.Win : CellOutcome.Loss;
            else if (int.TryParse(myScore, out int a) && int.TryParse(oppScore, out int b) && a != b)
                outcome = a > b ? CellOutcome.Win : CellOutcome.Loss;

            int? ratingChange = null;

            if (ratings != null && ratings.ByGame.TryGetValue(game.Id, out var change))
                ratingChange = meIsFirst ? change.Player1 : change.Player2;

            return new GroupTableCell
            {
                Outcome = outcome,
                Points = myPoints?.ToString() ?? "",
                Score = $"{myScore}:{oppScore}",
                RatingChange = ratingChange
            };
        }

        // ── Play-off bracket ────────────────────────────────────────────

        public static BracketModel BuildBracket(string title, APIEliminationTree tree, EventRatingChanges? ratings = null)
        {
            var model = new BracketModel { Title = title };

            var rounds = (tree.Rounds ?? []).OrderBy(r => r.Round).ToList();

            for (int r = 0; r < rounds.Count; r++)
            {
                var round = new BracketRound
                {
                    Title = rounds.Count > 1 && r == rounds.Count - 1 ? "Final" : $"Round {r + 1}"
                };

                // Games are created in bracket order, so the id order is the drawing order
                foreach (var game in (rounds[r].Games ?? []).OrderBy(g => g.Id))
                    round.Matches.Add(BuildMatch(game, ratings));

                model.Rounds.Add(round);
            }

            var last = model.Rounds.LastOrDefault()?.Matches.LastOrDefault();

            if (last != null && last.Winner != 0)
                model.WinnerName = last.Winner == 1 ? last.Player1 : last.Player2;

            return model;
        }

        private static BracketMatch BuildMatch(APINetGame game, EventRatingChanges? ratings)
        {
            var match = new BracketMatch
            {
                Player1 = SideName(game.Player1, game.Player1DoublesPair),
                Player2 = SideName(game.Player2, game.Player2DoublesPair),
                Score1 = game.Player1Score ?? "",
                Score2 = game.Player2Score ?? "",
                Winner = WinnerOf(game)
            };

            if (ratings != null && ratings.ByGame.TryGetValue(game.Id, out var change))
            {
                match.RatingChange1 = change.Player1;
                match.RatingChange2 = change.Player2;
            }

            return match;
        }

        private static string SideName(APIGamePlayer? player, APIDoublesPair? pair)
        {
            if (player != null)
                return FullName(player.Name, player.Surname);

            return pair?.Name ?? "";
        }

        private static string FullName(string? name, string? surname) => $"{name} {surname}".Trim();
    }

    /// <summary>Rating changes of one event: per game (both players) and summed per player KeyName.</summary>
    public sealed class EventRatingChanges
    {
        public Dictionary<int, (int Player1, int Player2)> ByGame { get; } = [];
        public Dictionary<string, int> TotalByKey { get; } = [];
    }
}

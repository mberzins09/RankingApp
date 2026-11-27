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
        /// With <paramref name="ratings"/> every participant also gets the sum of his rating changes in this event,
        /// with <paramref name="marks"/> the app player / friends are highlighted and with <paramref name="liveTables"/>
        /// (player key → table) players playing right now show their table.
        /// </summary>
        public static async Task<List<CalendarParticipantItem>> BuildParticipantsAsync(IDatabaseService database,
            IEnumerable<APIParticipant>? participants, MonthRanking? ranking, EventRatingChanges? ratings = null,
            PlayerMarks? marks = null, Dictionary<string, int>? liveTables = null)
        {
            var items = new List<CalendarParticipantItem>();

            foreach (var p in participants ?? [])
            {
                // Doubles event: the participant is a pair, numbered by the sum of both players' points
                if (p.DoublesPair != null && string.IsNullOrWhiteSpace(p.Name) && string.IsNullOrWhiteSpace(p.Surname))
                {
                    items.Add(BuildPair(p.DoublesPair.Name, ranking, marks));
                    continue;
                }

                string key = NameNormalizer.NormalizeKey($"{p.Name}{p.Surname}");
                var ranked = ranking?.Find(p.Name, p.Surname);

                // Birth date: event → month ranking → local player list
                string? birth = !string.IsNullOrWhiteSpace(p.BirthDate) ? p.BirthDate : ranked?.BirthDate;

                if (string.IsNullOrWhiteSpace(birth) && key.Length > 0)
                    birth = (await database.GetPlayerByKeyAsync(key))?.BirthDate;

                items.Add(new CalendarParticipantItem
                {
                    Name = p.Name ?? "",
                    Surname = p.Surname ?? "",
                    Age = string.IsNullOrWhiteSpace(birth) ? 0 : AgeCalculator.Calculate(birth),
                    PointsWithBonus = ranked?.PointsWithBonus ?? 0,
                    Points = ranked?.Points ?? 0,
                    RatingChange = ratings == null ? null : ratings.TotalByKey.GetValueOrDefault(key),
                    Mark = marks?.OfKey(key) ?? PlayerMark.None,
                    TableText = liveTables != null && liveTables.TryGetValue(key, out int table) ? $"T{table}" : ""
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

        /// <summary>
        /// A doubles pair "Pavāre Laura / Bokāne Kate". The order of name and surname is not known (participants list
        /// has surname first, games have name first) and names can have several words, so every split is tried.
        /// </summary>
        private static CalendarParticipantItem BuildPair(string pairName, MonthRanking? ranking, PlayerMarks? marks)
        {
            var item = new CalendarParticipantItem { PairName = pairName.Trim(), Surname = pairName.Trim() };

            foreach (var part in pairName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                PlayerDB? ranked = null;
                var mark = PlayerMark.None;

                foreach (var (name, surname) in NameSplits(part))
                {
                    ranked ??= ranking?.Find(name, surname);

                    var m = marks?.OfKey(NameNormalizer.NormalizeKey($"{name}{surname}")) ?? PlayerMark.None;
                    if (m > mark)
                        mark = m;
                }

                item.PointsWithBonus += ranked?.PointsWithBonus ?? 0;
                item.Points += ranked?.Points ?? 0;

                if (mark > item.Mark)
                    item.Mark = mark;
            }

            return item;
        }

        /// <summary>"Kučinska Elīza Paula" → (Elīza Paula, Kučinska), (Kučinska, Elīza Paula), (Paula, Kučinska Elīza), ...</summary>
        private static IEnumerable<(string Name, string Surname)> NameSplits(string fullName)
        {
            var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < words.Length; i++)
            {
                string first = string.Join(' ', words[..i]);
                string rest = string.Join(' ', words[i..]);

                yield return (rest, first);   // surname first (participants list)
                yield return (first, rest);   // name first (games)
            }
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

        // ── Table numbers (ongoing event) ───────────────────────────────

        /// <summary>True when some game is not finished yet - only then table numbers are worth loading.</summary>
        public static bool HasOpenGames(IEnumerable<APINet>? nets) =>
            AllGames(nets).Any(g => g.Status is not ("finished" or "cancelled" or "walkover"));

        /// <summary>Player KeyName → table of the game he is playing right now (also each member of a doubles pair).</summary>
        public static Dictionary<string, int> LiveTablesByPlayer(IEnumerable<APINet>? nets, EventLiveInfo? live)
        {
            var result = new Dictionary<string, int>();

            if (live == null || live.Games.Count == 0)
                return result;

            foreach (var game in AllGames(nets))
            {
                if (live.OpenGame(game.Id) is not { IsLive: true } info)
                    continue;

                foreach (string key in SideKeys(game.Player1, game.Player1DoublesPair).Concat(SideKeys(game.Player2, game.Player2DoublesPair)))
                    result[key] = info.Table;
            }

            return result;
        }

        private static IEnumerable<string> SideKeys(APIGamePlayer? player, APIDoublesPair? pair)
        {
            string[] names = player != null ? [$"{player.Name}{player.Surname}"] : (pair?.Name ?? "").Split('/');

            foreach (var name in names)
            {
                string key = NameNormalizer.NormalizeKey(name);
                if (key.Length > 0)
                    yield return key;
            }
        }

        /// <summary>Table of a game that is still to be played / being played; nothing for a decided game.</summary>
        private static (int? Table, bool IsLive) TableOf(APINetGame game, EventLiveInfo? live, bool decided)
        {
            if (decided || live?.OpenGame(game.Id) is not { } info)
                return (null, false);

            return (info.Table, info.IsLive);
        }

        private static string TableText(int? table, bool isLive) =>
            table is int t ? (isLive ? $"● T{t}" : $"T{t}") : "";

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
        public static List<TeamMatchItem> BuildTeamMatches(IEnumerable<APINet>? nets, EventRatingChanges? ratings,
            PlayerMarks? marks = null, EventLiveInfo? live = null)
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
                .Select(e => BuildTeamMatch(e.Game, e.Stage, e.RoundTitle, ratings, marks ?? PlayerMarks.Empty, live))
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

        private static TeamMatchItem BuildTeamMatch(APINetGame match, string stage, string roundTitle, EventRatingChanges? ratings,
            PlayerMarks marks, EventLiveInfo? live)
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

            var (table, isLive) = TableOf(match, live, winner != 0 && match.Status == "finished");

            return new TeamMatchItem
            {
                TableText = TableText(table, isLive),
                IsLive = isLive,
                Team1 = match.Player1Team?.Name ?? "",
                Team2 = match.Player2Team?.Name ?? "",
                Score1 = score1,
                Score2 = score2,
                Winner = winner,
                Subtitle = string.Join(" · ", parts),
                Games = (match.Games ?? []).OrderBy(g => g.Id).Select(g => BuildTeamGame(g, ratings, marks, live)).ToList()
            };
        }

        private static TeamGameItem BuildTeamGame(APINetGame game, EventRatingChanges? ratings, PlayerMarks marks, EventLiveInfo? live)
        {
            int winner = WinnerOf(game);
            var (table, isLive) = TableOf(game, live, game.Winner != null || game.Status == "finished");

            var item = new TeamGameItem
            {
                Player1 = SideName(game.Player1, game.Player1DoublesPair),
                Player2 = SideName(game.Player2, game.Player2DoublesPair),
                IsDoubles = game.Player1 == null || game.Player2 == null,
                Winner = winner,
                Score = table != null ? TableText(table, isLive) : winner != 0 ? $"{game.Player1Score}:{game.Player2Score}" : "–",
                Mark1 = marks.Of(game.Player1, game.Player1DoublesPair),
                Mark2 = marks.Of(game.Player2, game.Player2DoublesPair)
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

        public static GroupTableModel BuildGroupTable(string title, APINetGroup group, EventRatingChanges? ratings = null,
            PlayerMarks? marks = null, EventLiveInfo? live = null)
        {
            marks ??= PlayerMarks.Empty;

            var games = (group.Games ?? []).Where(g => Side1Key(g) != null && Side2Key(g) != null).ToList();

            // Order of players = their place in the group; doubles - pairs from the games; no players list - from the games
            var listed = (group.Players ?? []).Where(p => p.Id != null).ToList();
            List<GroupSide> sides;

            if (listed.Count > 0)
            {
                sides = listed
                    .OrderBy(p => p.Position)
                    .Select(p => new GroupSide($"p:{p.Id}", FullName(p.Name, p.Surname), p.Points?.ToString() ?? "",
                        p.Position > 0 ? p.Position.ToString() : "", marks.Of(p.Name, p.Surname)))
                    .ToList();
            }
            else if (games.Any(g => g.Player1DoublesPair != null || g.Player2DoublesPair != null))
            {
                sides = DoublesSides(group, games, marks);
            }
            else
            {
                sides = games
                    .SelectMany(g => new[] { g.Player1!, g.Player2! })
                    .GroupBy(p => p.Id)
                    .Select(g => new GroupSide($"p:{g.Key}", FullName(g.First().Name, g.First().Surname), "", "", marks.Of(g.First())))
                    .ToList();
            }

            var model = new GroupTableModel { Title = title };

            for (int i = 0; i < sides.Count; i++)
            {
                var row = new GroupTableRow
                {
                    Number = i + 1,
                    PlayerName = sides[i].Name,
                    Points = sides[i].Points,
                    Place = sides[i].Place,
                    Mark = sides[i].Mark
                };

                for (int j = 0; j < sides.Count; j++)
                {
                    if (i == j)
                    {
                        row.Cells.Add(new GroupTableCell { IsSelf = true });
                        continue;
                    }

                    // Every game of a highlighted player is tinted - his row and his column
                    var cell = BuildCell(games, sides[i].Key, sides[j].Key, ratings, live);
                    cell.Mark = sides[i].Mark > sides[j].Mark ? sides[i].Mark : sides[j].Mark;
                    row.Cells.Add(cell);
                }

                model.Rows.Add(row);
            }

            return model;
        }

        /// <summary>A row of the group table: a player ("p:id") or a doubles pair ("d:id").</summary>
        private sealed record GroupSide(string Key, string Name, string Points, string Place, PlayerMark Mark);

        private static string? SideKey(APIGamePlayer? player, APIDoublesPair? pair) =>
            player != null ? $"p:{player.Id}" : pair != null ? $"d:{pair.Id}" : null;

        private static string? Side1Key(APINetGame game) => SideKey(game.Player1, game.Player1DoublesPair);
        private static string? Side2Key(APINetGame game) => SideKey(game.Player2, game.Player2DoublesPair);

        /// <summary>
        /// Doubles group: the API rows have only position + points (no pair), so the pairs are taken from the games and their
        /// points summed from the games. A pair gets the place of the API row with the same points; on a tie the shared range ("1-3").
        /// </summary>
        private static List<GroupSide> DoublesSides(APINetGroup group, List<APINetGame> games, PlayerMarks marks)
        {
            var order = new List<APIDoublesPair>();
            var points = new Dictionary<int, int>();

            foreach (var game in games.OrderBy(g => g.Id))
            {
                foreach (var (pair, pts) in new[] { (game.Player1DoublesPair, game.Player1Points), (game.Player2DoublesPair, game.Player2Points) })
                {
                    if (pair == null)
                        continue;

                    if (!points.ContainsKey(pair.Id))
                        order.Add(pair);

                    points[pair.Id] = points.GetValueOrDefault(pair.Id) + (pts ?? 0);
                }
            }

            var apiRows = (group.Players ?? []).Where(p => p.Position > 0 && p.Points != null).ToList();
            bool hasPoints = apiRows.Count > 0 || points.Values.Any(v => v > 0);

            string PlaceOf(int pts)
            {
                var places = apiRows.Where(r => r.Points == pts).Select(r => r.Position).Order().ToList();
                return places.Count == 0 ? "" : places.Count == 1 ? $"{places[0]}" : $"{places[0]}-{places[^1]}";
            }

            return order
                .OrderByDescending(p => points[p.Id])   // stable: ties keep the order of the games
                .Select(p => new GroupSide($"d:{p.Id}", ShortPairName(p.Name), hasPoints ? $"{points[p.Id]}" : "",
                    PlaceOf(points[p.Id]), marks.Of(null, p)))
                .ToList();
        }

        /// <summary>Result of the game "me" against "opponent" (side keys) as seen by "me".</summary>
        private static GroupTableCell BuildCell(List<APINetGame> games, string me, string opponent, EventRatingChanges? ratings, EventLiveInfo? live)
        {
            var game = games.FirstOrDefault(g =>
                (Side1Key(g) == me && Side2Key(g) == opponent) ||
                (Side2Key(g) == me && Side1Key(g) == opponent));

            if (game == null)
                return new GroupTableCell();

            bool meIsFirst = Side1Key(game) == me;

            string myScore = (meIsFirst ? game.Player1Score : game.Player2Score) ?? "";
            string oppScore = (meIsFirst ? game.Player2Score : game.Player1Score) ?? "";
            int? myPoints = meIsFirst ? game.Player1Points : game.Player2Points;

            int winner = WinnerOf(game);
            var outcome = winner == 0 ? CellOutcome.None : (winner == 1) == meIsFirst ? CellOutcome.Win : CellOutcome.Loss;

            // Live scores can already show a leader - the game is decided only when it is finished
            var (table, isLive) = TableOf(game, live, game.Winner != null);

            // Not played yet: only the table (when one is given already)
            if (string.IsNullOrEmpty(myScore) && string.IsNullOrEmpty(oppScore))
                return new GroupTableCell { Table = table, IsLive = isLive };

            int? ratingChange = null;

            if (ratings != null && ratings.ByGame.TryGetValue(game.Id, out var change))
                ratingChange = meIsFirst ? change.Player1 : change.Player2;

            return new GroupTableCell
            {
                Outcome = outcome,
                Points = myPoints?.ToString() ?? "",
                Score = $"{myScore}:{oppScore}",
                RatingChange = ratingChange,
                Table = table,
                IsLive = isLive
            };
        }

        // ── Play-off bracket ────────────────────────────────────────────

        public static BracketModel BuildBracket(string title, APIEliminationTree tree, EventRatingChanges? ratings = null,
            PlayerMarks? marks = null, EventLiveInfo? live = null)
        {
            marks ??= PlayerMarks.Empty;

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
                    round.Matches.Add(BuildMatch(game, ratings, marks, live));

                model.Rounds.Add(round);
            }

            var last = model.Rounds.LastOrDefault()?.Matches.LastOrDefault();

            if (last != null && last.Winner != 0)
                model.WinnerName = last.Winner == 1 ? last.Player1 : last.Player2;

            return model;
        }

        private static BracketMatch BuildMatch(APINetGame game, EventRatingChanges? ratings, PlayerMarks marks, EventLiveInfo? live)
        {
            var (table, isLive) = TableOf(game, live, game.Winner != null);

            var match = new BracketMatch
            {
                Player1 = SideName(game.Player1, game.Player1DoublesPair),
                Player2 = SideName(game.Player2, game.Player2DoublesPair),
                Score1 = game.Player1Score ?? "",
                Score2 = game.Player2Score ?? "",
                Winner = WinnerOf(game),
                Mark1 = marks.Of(game.Player1, game.Player1DoublesPair),
                Mark2 = marks.Of(game.Player2, game.Player2DoublesPair),
                Table = table,
                IsLive = isLive
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

            return ShortPairName(pair?.Name);
        }

        /// <summary>
        /// Drawn doubles: "Laura Krūmiņa / Yvonne Fischer" → "L.Krūmiņa / Y.Fischer" (games have the name first;
        /// with more words the first is taken as the name and the last as the surname).
        /// </summary>
        public static string ShortPairName(string? pairName)
        {
            var players = (pairName ?? "")
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part =>
                {
                    var words = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return words.Length < 2 ? part : $"{words[0][0]}.{words[^1]}";
                });

            return string.Join(" / ", players);
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

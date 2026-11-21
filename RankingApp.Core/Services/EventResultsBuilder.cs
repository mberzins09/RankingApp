using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Turns the raw competition-event-results response into ready-to-draw models:
    /// participants list, group cross tables and play-off brackets.
    /// </summary>
    public static class EventResultsBuilder
    {
        // ── Participants ────────────────────────────────────────────────

        /// <summary>Numbered by ranking points (PlayerDB, 0 if the player is not found).</summary>
        public static async Task<List<CalendarParticipantItem>> BuildParticipantsAsync(IDatabaseService database, IEnumerable<APIParticipant>? participants)
        {
            var items = new List<CalendarParticipantItem>();

            foreach (var p in participants ?? [])
            {
                string key = NameNormalizer.NormalizeKey($"{p.Name}{p.Surname}");
                var dbPlayer = string.IsNullOrEmpty(key) ? null : await database.GetPlayerByKeyAsync(key);

                string? birth = !string.IsNullOrWhiteSpace(p.BirthDate) ? p.BirthDate : dbPlayer?.BirthDate;

                items.Add(new CalendarParticipantItem
                {
                    Name = p.Name,
                    Surname = p.Surname,
                    Age = string.IsNullOrWhiteSpace(birth) ? 0 : AgeCalculator.Calculate(birth),
                    PointsWithBonus = dbPlayer?.PointsWithBonus ?? 0
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

        public static GroupTableModel BuildGroupTable(string title, APINetGroup group)
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

                    row.Cells.Add(BuildCell(games, players[i].Id, players[j].Id));
                }

                model.Rows.Add(row);
            }

            return model;
        }

        /// <summary>Result of the game "me" against "opponent" as seen by "me".</summary>
        private static GroupTableCell BuildCell(List<APINetGame> games, int me, int opponent)
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

            return new GroupTableCell
            {
                Outcome = outcome,
                Points = myPoints?.ToString() ?? "",
                Score = $"{myScore}:{oppScore}"
            };
        }

        // ── Play-off bracket ────────────────────────────────────────────

        public static BracketModel BuildBracket(string title, APIEliminationTree tree)
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
                    round.Matches.Add(BuildMatch(game));

                model.Rounds.Add(round);
            }

            var last = model.Rounds.LastOrDefault()?.Matches.LastOrDefault();

            if (last != null && last.Winner != 0)
                model.WinnerName = last.Winner == 1 ? last.Player1 : last.Player2;

            return model;
        }

        private static BracketMatch BuildMatch(APINetGame game)
        {
            var match = new BracketMatch
            {
                Player1 = SideName(game.Player1, game.Player1DoublesPair),
                Player2 = SideName(game.Player2, game.Player2DoublesPair),
                Score1 = game.Player1Score ?? "",
                Score2 = game.Player2Score ?? ""
            };

            if (game.Winner != null)
            {
                if (game.Player1 != null && game.Winner.Id == game.Player1.Id)
                    match.Winner = 1;
                else if (game.Player2 != null && game.Winner.Id == game.Player2.Id)
                    match.Winner = 2;
            }
            else if (int.TryParse(match.Score1, out int a) && int.TryParse(match.Score2, out int b) && a != b)
            {
                match.Winner = a > b ? 1 : 2;
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
}

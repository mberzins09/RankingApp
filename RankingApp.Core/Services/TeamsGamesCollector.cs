using RankingApp.Core.Models;
using System.Globalization;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Reads competition-event-results of a teams event and finds the app user's games.
    /// Used by the Import page (whole event, split by day) and the Tournament page (one day).
    /// </summary>
    public static class TeamsGamesCollector
    {
        private static readonly TimeZoneInfo RigaTimeZone = FindRigaTimeZone();

        /// <summary>
        /// Checks the participants list (player_id or name). If the API returns no participants,
        /// falls back to looking for the user in the event games.
        /// </summary>
        public static bool IsMeListed(APIEventResultsResponse result, int myApiId, string myKey)
        {
            if (result.Participants is { Count: > 0 })
            {
                return result.Participants.Any(p =>
                    (myApiId > 0 && p.PlayerId == myApiId) ||
                    NameNormalizer.NormalizeKey($"{p.Name}{p.Surname}") == myKey);
            }

            return CollectMyGamesByDate(result, myApiId, myKey, DateTime.Today).Count > 0;
        }

        /// <summary>
        /// All finished singles and doubles games of the user - top-level games and games nested inside
        /// team matches (game_type = "teams") - grouped by playing day (Riga time). Nested games have no
        /// date of their own, so they take the date of their team match (scheduled_at → started_at → ended_at).
        /// Walkovers ("W"), unfinished games and games with a missing player/pair are skipped.
        /// </summary>
        public static Dictionary<DateTime, TeamsDayGames> CollectMyGamesByDate(
            APIEventResultsResponse result, int myApiId, string myKey, DateTime fallbackDate)
        {
            var byDate = new Dictionary<DateTime, TeamsDayGames>();
            var seenIds = new HashSet<int>();
            var singles = new List<(DateTime Date, APIGame Game)>();

            bool IsMe(APIGamePlayer? p) =>
                p != null &&
                ((myApiId > 0 && p.Id == myApiId) ||
                 NameNormalizer.NormalizeKey($"{p.Name}{p.Surname}") == myKey);

            TeamsDayGames Day(DateTime date)
            {
                if (!byDate.TryGetValue(date, out var day))
                {
                    day = new TeamsDayGames();
                    byDate[date] = day;
                }

                return day;
            }

            void Visit(APINetGame? g, DateTime parentDate)
            {
                if (g == null)
                    return;

                DateTime date = GameDate(g) ?? parentDate;

                bool finished = string.IsNullOrEmpty(g.Status) || g.Status == "finished";
                bool hasScore = int.TryParse(g.Player1Score, out int s1) & int.TryParse(g.Player2Score, out int s2);

                if (finished && hasScore)
                {
                    if (g.GameType == "singles" &&
                        g.Player1 != null && g.Player2 != null &&
                        (IsMe(g.Player1) || IsMe(g.Player2)) &&
                        seenIds.Add(g.Id))
                    {
                        singles.Add((date, new APIGame
                        {
                            Id = g.Id,
                            Player1 = g.Player1,
                            Player2 = g.Player2,
                            Player1Score = g.Player1Score,
                            Player2Score = g.Player2Score,
                            StartedAt = g.StartedAt,
                            EndedAt = g.EndedAt,
                            GroupId = g.GroupId
                        }));
                    }
                    else if (g.GameType == "doubles" &&
                             g.Player1DoublesPair != null && g.Player2DoublesPair != null &&
                             (PairHasKey(g.Player1DoublesPair.Name, myKey) || PairHasKey(g.Player2DoublesPair.Name, myKey)) &&
                             seenIds.Add(g.Id))
                    {
                        Day(date).Doubles.Add(new APIDoublesGame
                        {
                            Id = g.Id,
                            Pair1Name = g.Player1DoublesPair.Name,
                            Pair2Name = g.Player2DoublesPair.Name,
                            Pair1Sets = s1,
                            Pair2Sets = s2
                        });
                    }
                }

                foreach (var inner in g.Games ?? [])
                    Visit(inner, date);
            }

            foreach (var net in (result.Nets ?? []).OrderBy(n => n.Order))
            {
                foreach (var g in net.Groups?.SelectMany(gr => gr.Games ?? []) ?? [])
                    Visit(g, fallbackDate);

                foreach (var g in net.EliminationTrees?.SelectMany(t => t.Rounds ?? []).SelectMany(r => r.Games ?? []) ?? [])
                    Visit(g, fallbackDate);
            }

            var realGames = RemoveCarriedOverGroupGames(singles.Select(x => x.Game).ToList()).ToHashSet();

            foreach (var (date, game) in singles)
            {
                if (realGames.Contains(game))
                    Day(date).Singles.Add(game);
            }

            // A day could be left with nothing after the filter
            foreach (var empty in byDate.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList())
                byDate.Remove(empty);

            return byDate;
        }

        /// <summary>
        /// When two players meet in a group stage and land in the same group again in a later group stage,
        /// they do not play again - the API shows the earlier result in the new group too. Such a copy has
        /// no started_at / ended_at. So for every pair with several group games, games without timestamps
        /// are dropped - but only if at least one of the pair's games has timestamps (old, manually entered
        /// events and nested teams games have no timestamps at all and are kept as they are).
        /// </summary>
        public static List<APIGame> RemoveCarriedOverGroupGames(List<APIGame> games)
        {
            static bool HasTime(APIGame g) =>
                !string.IsNullOrEmpty(g.StartedAt) || !string.IsNullOrEmpty(g.EndedAt);

            static string PlayerKey(APIGamePlayer? p) =>
                p == null ? "" : p.Id > 0 ? p.Id.ToString() : NameNormalizer.NormalizeKey($"{p.Name}{p.Surname}");

            static string PairKey(APIGame g)
            {
                string a = PlayerKey(g.Player1), b = PlayerKey(g.Player2);
                return string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";
            }

            var drop = new HashSet<APIGame>();

            foreach (var pair in games.Where(g => g.GroupId != null).GroupBy(PairKey))
            {
                if (pair.Count() < 2 || !pair.Any(HasTime))
                    continue;

                foreach (var copy in pair.Where(g => !HasTime(g)))
                    drop.Add(copy);
            }

            return games.Where(g => !drop.Contains(g)).ToList();
        }



        /// <summary>
        /// "Nikolajs Golubevs / Anrijs Bergs" → [("Nikolajs Golubevs", key), ("Anrijs Bergs", key)].
        /// </summary>
        public static List<(string FullName, string Key)> SplitPair(string? pairName)
        {
            return (pairName ?? "")
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(n => (n, NameNormalizer.NormalizeKey(n)))
                .ToList();
        }

        /// <summary>
        /// Fallback when a player is not in PlayerDB: last word is the surname, the rest is the name
        /// ("Rodrigo Ritvars Ļaudams" → "Rodrigo Ritvars" + "Ļaudams").
        /// </summary>
        public static (string Name, string Surname) SplitFullName(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return parts.Length switch
            {
                0 => ("?", "?"),
                1 => (parts[0], ""),
                _ => (string.Join(' ', parts[..^1]), parts[^1])
            };
        }

        private static bool PairHasKey(string? pairName, string myKey) =>
            !string.IsNullOrEmpty(myKey) && SplitPair(pairName).Any(p => p.Key == myKey);

        /// <summary>Local (Riga) calendar day of a game, from its UTC timestamps.</summary>
        private static DateTime? GameDate(APINetGame g)
        {
            foreach (var raw in new[] { g.ScheduledAt, g.StartedAt, g.EndedAt })
            {
                if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var dto))
                {
                    return TimeZoneInfo.ConvertTime(dto, RigaTimeZone).Date;
                }
            }

            return null;
        }

        private static TimeZoneInfo FindRigaTimeZone()
        {
            foreach (var id in new[] { "Europe/Riga", "FLE Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch { /* try next id */ }
            }

            return TimeZoneInfo.Local;
        }
    }
}

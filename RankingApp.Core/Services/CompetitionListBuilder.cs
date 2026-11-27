using RankingApp.Core.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Builds the Calendar / Results list: one row per competition (grouped by competition id).
    /// Expanding a competition inserts its events right below it in the same flat list, so the
    /// CollectionView never has to resize an existing row.
    /// </summary>
    public static class CompetitionListBuilder
    {
        public static List<CompetitionGroupItem> Group(IEnumerable<CalendarEventItem> events, bool newestFirst)
        {
            var groups = events
                .GroupBy(e => e.CompetitionId)
                .Select(g =>
                {
                    var ordered = g
                        .OrderBy(e => ParseDate(e.StartDate) ?? DateTime.MaxValue)
                        .ThenBy(e => e.StartTime)
                        .ThenBy(e => e.EventName)
                        .ToList();

                    var first = ordered[0];

                    return new CompetitionGroupItem
                    {
                        CompetitionId = g.Key,
                        Name = first.CompetitionName,
                        City = ordered.Select(e => e.City).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "",
                        FirstDate = first.StartDate,
                        LastDate = ordered[^1].StartDate,
                        Events = ordered
                    };
                });

            return newestFirst
                ? groups.OrderByDescending(g => ParseDate(g.FirstDate) ?? DateTime.MinValue).ThenBy(g => g.Name).ToList()
                : groups.OrderBy(g => ParseDate(g.FirstDate) ?? DateTime.MaxValue).ThenBy(g => g.Name).ToList();
        }

        /// <summary>Replaces the rows with collapsed header rows (competitions, team matches).</summary>
        public static void Fill(ObservableCollection<object> rows, IEnumerable<IExpandableRow> headers)
        {
            rows.Clear();

            foreach (var h in headers)
            {
                h.IsExpanded = false;
                rows.Add(h);
            }
        }

        /// <summary>Shows or hides the child rows (events of a competition, games of a team match) right below the header.</summary>
        public static void Toggle(ObservableCollection<object> rows, IExpandableRow header)
        {
            int index = rows.IndexOf(header);
            if (index < 0)
                return;

            var children = header.ChildRows.ToList();

            if (header.IsExpanded)
            {
                foreach (var child in children)
                    rows.Remove(child);

                header.IsExpanded = false;
            }
            else if (children.Count > 0)
            {
                for (int i = 0; i < children.Count; i++)
                    rows.Insert(index + 1 + i, children[i]);

                header.IsExpanded = true;
            }
        }

        /// <summary>
        /// Results list events of one year: singles / doubles (and some teams) events from competitions?year=..,
        /// plus teams events from competition-events?type=teams. A teams event that is in both lists is taken from the
        /// teams list, where a season is listed once per playing date (results are then asked for that date only).
        /// </summary>
        public static List<CalendarEventItem> ResultsEvents(int year, IEnumerable<APICompetition> competitions,
            IEnumerable<APITeamsEventListItem> teamsEvents)
        {
            var teamsList = teamsEvents.ToList();
            var teamsIds = teamsList.Select(e => e.Id).ToHashSet();

            var fromCompetitions = competitions
                .SelectMany(c => (c.Events ?? [])
                    .Where(e => !teamsIds.Contains(e.Id))
                    .Select(e => new CalendarEventItem
                    {
                        EventId = e.Id,
                        CompetitionId = c.Id,
                        CompetitionName = c.Name,
                        EventName = e.Name,
                        StartDate = e.StartDate,
                        StartTime = e.StartTime ?? "",
                        RankingCoef = e.RankingCoef,
                        City = c.Arena?.City ?? ""
                    }));

            var fromTeams = teamsList
                .Select(FromTeamsEvent)
                .Where(e => ParseDate(e.StartDate)?.Year == year)
                .DistinctBy(e => (e.EventId, e.PlayingDate));

            return [.. fromCompetitions.Concat(fromTeams)];
        }

        private static CalendarEventItem FromTeamsEvent(APITeamsEventListItem e)
        {
            var start = ParseDate(e.StartDate);
            var end = ParseDate(e.EndDate);

            // Season (or a teams event longer than 3 days): one row per playing date
            bool perDate = e.IsSeasonRankingInstance || (start != null && end != null && (end.Value - start.Value).TotalDays > 3);
            string playingDate = string.IsNullOrWhiteSpace(e.EffectiveDate) ? e.StartDate : e.EffectiveDate;

            return new CalendarEventItem
            {
                EventId = e.Id,
                CompetitionId = e.Competition?.Id is > 0 ? e.Competition.Id : -e.Id,   // no competition - the event is its own group
                CompetitionName = string.IsNullOrWhiteSpace(e.Competition?.Name) ? e.Name : e.Competition.Name,
                EventName = e.Name,
                StartDate = perDate ? playingDate : e.StartDate,
                StartTime = e.StartTime ?? "",
                RankingCoef = e.RankingCoef,
                City = e.Arena?.City ?? "",
                PlayingDate = perDate ? playingDate : ""
            };
        }

        /// <summary>Marks events the app player / friends take part in; a competition gets the strongest mark of its events.</summary>
        public static void ApplyMarks(IEnumerable<CompetitionGroupItem> groups, IReadOnlyDictionary<int, PlayerMark> marks)
        {
            foreach (var group in groups)
            {
                var groupMark = PlayerMark.None;

                foreach (var e in group.Events)
                {
                    e.Mark = marks.GetValueOrDefault(e.EventId);

                    if (e.Mark > groupMark)
                        groupMark = e.Mark;
                }

                group.Mark = groupMark;
            }
        }

        public static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}

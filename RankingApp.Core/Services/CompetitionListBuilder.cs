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

        public static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}

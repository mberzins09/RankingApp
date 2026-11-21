namespace RankingApp.Core.Models
{
    /// <summary>
    /// Names of the parameters passed between pages, and a safe reader for them.
    /// The values are also mirrored into <see cref="Data"/> as a fallback.
    /// </summary>
    public static class NavParams
    {
        public const string TournamentId = "tournamentId";
        public const string GameId = "gameId";
        public const string EventId = "eventId";

        /// <summary>Shell route of the calendar event page (same as nameof(CalendarEventPage) registered in AppShell).</summary>
        public const string CalendarEventRoute = "CalendarEventPage";

        /// <summary>Reads an int parameter - accepts int or numeric string, ignores 0 / missing.</summary>
        public static bool TryGetInt(IDictionary<string, object> query, string key, out int value)
        {
            value = 0;

            if (!query.TryGetValue(key, out var raw) || raw == null)
                return false;

            value = raw switch
            {
                int i => i,
                long l => (int)l,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => 0
            };

            return value > 0;
        }
    }
}

using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using System.Globalization;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Monthly ranking lists with a permanent local copy. Wraps the API repository, so every place that asks for a
    /// month (All Player Ranking, tournament / game pages, import, results) gets it from:
    ///   1. memory (this app session) → 2. the MonthRanking table → 3. the ranking API, and then stores it.
    /// A month is stored only when the API really returned players for it (a month not published yet is not stored,
    /// so it is asked again later). The returned list has the same shape as the API repository's:
    /// sorted by PointsWithBonus with OverallPlace set, plus the "Unranked Player" entry at the end.
    /// </summary>
    public class CachedPlayerRepositoryWithDate(PlayerReposotoryWithDate inner, IDatabaseService database) : IPlayerRepositoryWithDate
    {
        private const string UnrankedKey = "unrankedplayer";

        private readonly PlayerReposotoryWithDate _inner = inner;
        private readonly IDatabaseService _database = database;

        private readonly Dictionary<(int Year, int Month), List<MonthRankingRow>> _memory = [];
        private readonly object _memoryLock = new();

        // The API repository keeps state in a field and the API has a rate limit - one month at a time
        private readonly SemaphoreSlim _loadLock = new(1, 1);

        public async Task<List<PlayerDB>> GetPlayersAsync(string date, bool isOldAPIBody)
        {
            // Accepts "yyyy-MM" and "yyyy-MM-dd"; anything else goes straight to the API as before
            if (!TryParseMonth(date, out int year, out int month))
                return await _inner.GetPlayersAsync(date, isOldAPIBody);

            if (TryGetMemory(year, month, out var cached))
                return ToPlayers(cached);

            await _loadLock.WaitAsync();
            try
            {
                // Another caller may have loaded it while this one waited
                if (TryGetMemory(year, month, out cached))
                    return ToPlayers(cached);

                var stored = await _database.GetMonthRankingAsync(year, month);

                if (stored.Count > 0)
                {
                    SetMemory(year, month, stored);
                    return ToPlayers(stored);
                }

                var downloaded = await _inner.GetPlayersAsync($"{year:0000}-{month:00}", isOldAPIBody);

                var rows = downloaded
                    .Where(p => !string.IsNullOrEmpty(p.KeyName) && p.KeyName != UnrankedKey)
                    .Select(p => MonthRankingRow.From(p, year, month))
                    .ToList();

                if (rows.Count > 0)
                {
                    await _database.SaveMonthRankingAsync(year, month, rows);
                    SetMemory(year, month, rows);
                    return ToPlayers(rows);
                }

                // Nothing published for this month (or no internet) - same answer as the API repository, not stored
                return downloaded;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private bool TryGetMemory(int year, int month, out List<MonthRankingRow> rows)
        {
            lock (_memoryLock)
                return _memory.TryGetValue((year, month), out rows!);
        }

        private void SetMemory(int year, int month, List<MonthRankingRow> rows)
        {
            lock (_memoryLock)
                _memory[(year, month)] = rows;
        }

        private static List<PlayerDB> ToPlayers(List<MonthRankingRow> rows)
        {
            var players = rows
                .OrderBy(r => r.OverallPlace == 0 ? int.MaxValue : r.OverallPlace)
                .ThenByDescending(r => r.PointsWithBonus)
                .Select(r => r.ToPlayer())
                .ToList();

            players.Add(new PlayerDB
            {
                Gender = "Unknown",
                Name = "Unranked",
                Surname = "Player",
                Place = 10000,
                OverallPlace = 10000,
                Points = 0,
                PointsWithBonus = 0,
                KeyName = UnrankedKey,
            });

            return players;
        }

        private static bool TryParseMonth(string? date, out int year, out int month)
        {
            year = month = 0;

            if (DateTime.TryParseExact(date, ["yyyy-MM", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                year = d.Year;
                month = d.Month;
                return true;
            }

            return false;
        }
    }
}

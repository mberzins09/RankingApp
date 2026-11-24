using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;

namespace RankingApp.Core.Services
{
    /// <summary>The LGTF ranking list of one month, looked up by player KeyName (normalized name + surname).</summary>
    public sealed class MonthRanking
    {
        public int Year { get; init; }
        public int Month { get; init; }
        public Dictionary<string, PlayerDB> ByKey { get; init; } = [];

        public string Label => $"{Year:0000}-{Month:00}";

        public PlayerDB? Find(string? name, string? surname)
        {
            string key = NameNormalizer.NormalizeKey($"{name}{surname}");
            return key.Length > 0 && ByKey.TryGetValue(key, out var player) ? player : null;
        }
    }

    /// <summary>
    /// Ranking lists by month, kept in memory for the whole app session (singleton).
    /// * The month that is stored in the local PlayerDB (AppData.CurrentYear / CurrentMonth) is read from the database.
    /// * Any other month is downloaded once from the ranking API.
    /// * If a month is not published yet (empty), the previous month is used.
    /// Concurrent requests for the same month share one download.
    /// </summary>
    public class MonthRankingService(IPlayerRepositoryWithDate repository, IDatabaseService database)
    {
        private readonly IPlayerRepositoryWithDate _repository = repository;
        private readonly IDatabaseService _database = database;

        private readonly Dictionary<(int Year, int Month), Task<MonthRanking?>> _cache = [];
        private readonly object _cacheLock = new();

        // The ranking repository is not safe for parallel calls (and the API has a rate limit) - one download at a time
        private readonly SemaphoreSlim _downloadLock = new(1, 1);

        /// <summary>Ranking used for the given month, or null if it could not be loaded (no internet etc.).</summary>
        public Task<MonthRanking?> GetAsync(int year, int month)
        {
            var key = (year, month);

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var existing))
                    return existing;

                var task = LoadWithFallbackAsync(year, month);
                _cache[key] = task;

                // Failed / empty results are not kept, so they are tried again next time
                _ = task.ContinueWith(t =>
                {
                    if (t.IsFaulted || t.IsCanceled || t.Result == null)
                    {
                        lock (_cacheLock)
                        {
                            if (_cache.TryGetValue(key, out var current) && current == t)
                                _cache.Remove(key);
                        }
                    }
                }, TaskScheduler.Default);

                return task;
            }
        }

        /// <summary>Starts loading in the background (when a page with that month opens). Errors are ignored here.</summary>
        public void Prefetch(int year, int month)
        {
            if (year <= 0 || month is < 1 or > 12)
                return;

            _ = GetAsync(year, month).ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task<MonthRanking?> LoadWithFallbackAsync(int year, int month)
        {
            var ranking = await LoadAsync(year, month);
            if (ranking != null)
                return ranking;

            // Ranking of this month not published yet - the previous one is still valid
            var previous = new DateTime(year, month, 1).AddMonths(-1);
            return await LoadAsync(previous.Year, previous.Month);
        }

        private async Task<MonthRanking?> LoadAsync(int year, int month)
        {
            List<PlayerDB> players;

            var appData = await _database.GetAppDataAsync();

            if (appData.CurrentYear == year && appData.CurrentMonth == month)
            {
                // Same month as the stored ranking - active PlayerDB players are exactly that list
                players = (await _database.GetAllRecordsAsync<PlayerDB>()).Where(p => p.IsActive).ToList();
            }
            else
            {
                await _downloadLock.WaitAsync();
                try
                {
                    players = await _repository.GetPlayersAsync($"{year:0000}-{month:00}", false) ?? [];
                }
                finally
                {
                    _downloadLock.Release();
                }
            }

            var byKey = new Dictionary<string, PlayerDB>();

            foreach (var p in players)
            {
                if (string.IsNullOrEmpty(p.KeyName) || p.KeyName == "unrankedplayer")
                    continue;

                byKey.TryAdd(p.KeyName, p);
            }

            if (byKey.Count == 0)
                return null;

            return new MonthRanking { Year = year, Month = month, ByKey = byKey };
        }
    }
}

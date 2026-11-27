using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Which events the app player (gold) and friends (blue) take part in, for the Calendar and Results lists.
    /// Event ids come from competition-events-for-player (by PlayerDB.NewId) and are stored in table PlayerEvent.
    /// Syncing runs in the background; a failed API call keeps the previously stored ids of that player.
    /// </summary>
    public class PlayerEventsService(IDatabaseService database, TournamentService tournamentService, FriendService friendService)
    {
        private readonly IDatabaseService _database = database;
        private readonly TournamentService _tournamentService = tournamentService;
        private readonly FriendService _friends = friendService;

        // One sync at a time (startup, friend added / removed, app player changed)
        private readonly SemaphoreSlim _syncLock = new(1, 1);

        private IReadOnlyDictionary<int, PlayerMark>? _marks;
        private bool _startupSyncStarted;

        /// <summary>Raised (on a background thread) after the stored marks changed.</summary>
        public event EventHandler? Changed;

        /// <summary>Event id → strongest mark (Me over Friend). Read from the database the first time.</summary>
        public async Task<IReadOnlyDictionary<int, PlayerMark>> GetMarksAsync() =>
            _marks ??= await BuildMarksAsync();

        /// <summary>App start: refreshes the events of the app player and all friends, once per app run.</summary>
        public void SyncOnStartup()
        {
            if (_startupSyncStarted)
                return;

            _startupSyncStarted = true;
            RunInBackground(refreshAll: true);
        }

        /// <summary>
        /// After a friend was added / removed or the app player was changed: drops ids of players no longer
        /// tracked and downloads ids of newly tracked ones.
        /// </summary>
        public void SyncAfterChange() => RunInBackground(refreshAll: false);

        private void RunInBackground(bool refreshAll) =>
            _ = Task.Run(async () =>
            {
                try
                {
                    await SyncAsync(refreshAll);
                }
                catch (Exception ex)
                {
                    // Never let a background sync crash the app - the stored ids stay as they were
                    System.Diagnostics.Debug.WriteLine($"Player events sync failed: {ex.Message}");
                }
            });

        private async Task SyncAsync(bool refreshAll)
        {
            await _syncLock.WaitAsync();
            try
            {
                string meKey = await _friends.GetAppPlayerKeyAsync();
                var tracked = await _friends.GetKeysAsync();

                if (!string.IsNullOrEmpty(meKey))
                    tracked.Add(meKey);

                var stored = (await _database.GetAllRecordsAsync<PlayerEvent>())
                    .GroupBy(r => r.KeyName)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.EventId).ToHashSet());

                bool changed = false;

                // Removed friends / previous app player
                foreach (var key in stored.Keys.Where(k => !tracked.Contains(k)))
                {
                    await _database.DeletePlayerEventsAsync(key);
                    changed = true;
                }

                foreach (var key in tracked)
                {
                    if (!refreshAll && stored.ContainsKey(key))
                        continue;

                    int newId = await GetApiPlayerIdAsync(key, meKey);
                    if (newId <= 0)
                        continue;

                    HashSet<int> ids;
                    try
                    {
                        ids = [.. await _tournamentService.GetPlayerEventIdsAsync(newId)];
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
                    {
                        // No internet / server down / rate limit - keep what is stored for this player
                        System.Diagnostics.Debug.WriteLine($"Events of player {newId} not loaded: {ex.Message}");
                        continue;
                    }

                    if (stored.TryGetValue(key, out var old) && old.SetEquals(ids))
                        continue;

                    await _database.ReplacePlayerEventsAsync(key, ids);
                    changed = true;
                }

                // The marks depend on who is the app player / a friend, even when no ids changed
                var marks = await BuildMarksAsync();

                if (changed || _marks == null || !SameMarks(_marks, marks))
                {
                    _marks = marks;
                    Changed?.Invoke(this, EventArgs.Empty);
                }
            }
            finally
            {
                _syncLock.Release();
            }
        }

        /// <summary>The API player id (PlayerDB.NewId). For the app player AppData.AppUserNewId is the fallback.</summary>
        private async Task<int> GetApiPlayerIdAsync(string key, string meKey)
        {
            var player = await _database.GetPlayerByKeyAsync(key);

            if (player?.NewId > 0)
                return player.NewId;

            return key == meKey ? (await _database.GetAppDataAsync()).AppUserNewId : 0;
        }

        private async Task<IReadOnlyDictionary<int, PlayerMark>> BuildMarksAsync()
        {
            string meKey = await _friends.GetAppPlayerKeyAsync();
            var friendKeys = await _friends.GetKeysAsync();
            var marks = new Dictionary<int, PlayerMark>();

            foreach (var row in await _database.GetAllRecordsAsync<PlayerEvent>())
            {
                var mark = !string.IsNullOrEmpty(meKey) && row.KeyName == meKey ? PlayerMark.Me
                    : friendKeys.Contains(row.KeyName) ? PlayerMark.Friend
                    : PlayerMark.None;

                if (mark > marks.GetValueOrDefault(row.EventId))
                    marks[row.EventId] = mark;
            }

            return marks;
        }

        private static bool SameMarks(IReadOnlyDictionary<int, PlayerMark> a, IReadOnlyDictionary<int, PlayerMark> b) =>
            a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var m) && m == kv.Value);
    }
}

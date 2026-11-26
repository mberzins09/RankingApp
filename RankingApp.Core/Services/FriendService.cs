using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Friends (table Friend) and the app player, used to highlight them in event results.
    /// <see cref="Version"/> grows on every change of either, so pages can tell when their highlights are out of date.
    /// </summary>
    public class FriendService(IDatabaseService database)
    {
        private readonly IDatabaseService _database = database;
        private List<Friend>? _friends;

        public int Version { get; private set; }

        public async Task<List<Friend>> GetAllAsync()
        {
            _friends ??= await _database.GetAllRecordsAsync<Friend>();
            return _friends;
        }

        public async Task<HashSet<string>> GetKeysAsync() =>
            [.. (await GetAllAsync()).Select(f => f.KeyName).Where(k => !string.IsNullOrEmpty(k))];

        public async Task<bool> IsFriendAsync(string? keyName) =>
            !string.IsNullOrEmpty(keyName) && (await GetAllAsync()).Any(f => f.KeyName == keyName);

        public async Task AddAsync(PlayerDB player)
        {
            if (string.IsNullOrEmpty(player.KeyName) || await IsFriendAsync(player.KeyName))
                return;

            var friend = new Friend { KeyName = player.KeyName, Name = player.Name ?? "", Surname = player.Surname ?? "" };
            await _database.SaveAsync(friend);

            (await GetAllAsync()).Add(friend);
            Version++;
        }

        public async Task RemoveAsync(string? keyName)
        {
            var list = await GetAllAsync();

            foreach (var friend in list.Where(f => f.KeyName == keyName).ToList())
            {
                await _database.DeleteAsync(friend);
                list.Remove(friend);
            }

            Version++;
        }

        /// <summary>Call after the app player is changed, so highlights are rebuilt.</summary>
        public void NotifyAppPlayerChanged() => Version++;

        /// <summary>The app player's KeyName: stored in AppData, or read from his PlayerDB row.</summary>
        public async Task<string> GetAppPlayerKeyAsync()
        {
            var appData = await _database.GetAppDataAsync();

            if (!string.IsNullOrEmpty(appData.AppUserKeyName))
                return appData.AppUserKeyName;

            if (appData.AppUserPlayerId != 0)
                return (await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId))?.KeyName ?? "";

            return "";
        }

        public async Task<PlayerMarks> GetMarksAsync() =>
            new([await GetAppPlayerKeyAsync()], await GetKeysAsync());
    }
}

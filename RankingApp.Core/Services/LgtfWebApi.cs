using RankingApp.Core.Models;
using System.Net;
using System.Net.Http.Json;

namespace RankingApp.Core.Services
{
    /// <summary>
    /// Client for the public RankingApp endpoints on MartinsWeb (no authentication).
    /// Serves the historical LGTF data that used to ship inside the app as lgtf.sqlite.
    /// </summary>
    public static class LgtfWebApi
    {
        private static readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri(Data.WebApiUrl),
            Timeout     = TimeSpan.FromSeconds(60)
        };

        /// <summary>All players, used to seed the local PlayerDB table on a fresh install.</summary>
        public static async Task<List<LgtfApiPlayer>> GetPlayersAsync(CancellationToken ct = default)
        {
            return await _httpClient.GetFromJsonAsync<List<LgtfApiPlayer>>("players", ct) ?? [];
        }

        /// <summary>
        /// All competitions and games of the player with the given KeyName.
        /// Returns null when the server does not know that player.
        /// </summary>
        public static async Task<LgtfApiPlayerHistory?> GetPlayerHistoryAsync(string keyName, CancellationToken ct = default)
        {
            using var response = await _httpClient.GetAsync($"players/{Uri.EscapeDataString(keyName)}/history", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LgtfApiPlayerHistory>(ct);
        }
    }
}

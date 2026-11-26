using Microsoft.Maui.Storage;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RankingApp.Core.Services
{
    /// <summary>Table number and website status of one game (status "live" = being played now).</summary>
    public readonly record struct LiveGameInfo(int Table, string Status)
    {
        public bool IsLive => Status is "live" or "ongoing";
        public bool IsOpen => Status is not ("finished" or "cancelled" or "walkover");
    }

    /// <summary>Table numbers of the games of one event, by game id (the same ids as the tournament API).</summary>
    public sealed class EventLiveInfo
    {
        public static EventLiveInfo Empty { get; } = new();

        public Dictionary<int, LiveGameInfo> Games { get; } = [];

        /// <summary>Table of a game that is not finished yet (null = finished / no table given).</summary>
        public LiveGameInfo? OpenGame(int gameId) =>
            Games.TryGetValue(gameId, out var g) && g.Table > 0 && g.IsOpen ? g : null;
    }

    /// <summary>
    /// The public API (api/v1) has no table numbers. The tournament website shows them ("T5") from its own page data:
    /// every game of the page events/{event}/net/{net} has "table_nr". That page is a Laravel + Inertia page, so
    /// an Inertia partial request returns only its "games" prop as JSON (≈1.5 KB per game) instead of the 4 MB page.
    /// Inertia needs the site's asset version in a header: it is kept in Preferences and, when the site is
    /// redeployed (409 Conflict), read again from a page's html.
    /// </summary>
    public partial class LgtfWebSiteService
    {
        private const string SiteUrl = "https://turniri.lgtf.lv/";
        private const string VersionPreferenceKey = "lgtf_site_inertia_version";
        private const int MaxVersionSearchBytes = 6 * 1024 * 1024;

        private static readonly HttpClient _http = new() { BaseAddress = new Uri(SiteUrl), Timeout = TimeSpan.FromSeconds(40) };

        private readonly SemaphoreSlim _versionLock = new(1, 1);
        private string? _version;

        /// <summary>Table numbers of every game in the given nets. Never throws - what could not be read is just missing.</summary>
        public async Task<EventLiveInfo> GetLiveInfoAsync(int eventId, IEnumerable<int> netIds)
        {
            var info = new EventLiveInfo();

            foreach (int netId in netIds.Distinct())
            {
                try
                {
                    await LoadNetAsync(eventId, netId, info);
                }
                catch (Exception ex)
                {
                    // Table numbers are extra information - the results are shown without them
                    System.Diagnostics.Debug.WriteLine($"Table numbers of net {netId} failed: {ex.Message}");
                }
            }

            return info;
        }

        private async Task LoadNetAsync(int eventId, int netId, EventLiveInfo info)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string? version = await GetVersionAsync(refresh: attempt > 0);
                if (string.IsNullOrEmpty(version))
                    return;

                using var request = new HttpRequestMessage(HttpMethod.Get, $"events/{eventId}/net/{netId}");
                request.Headers.Add("X-Inertia", "true");
                request.Headers.Add("X-Inertia-Version", version);
                request.Headers.Add("X-Inertia-Partial-Component", "Competitions/NetView");
                request.Headers.Add("X-Inertia-Partial-Data", "games");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.Headers.Add("Accept", "text/html, application/xhtml+xml");

                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                // Site redeployed - its version changed
                if (response.StatusCode == HttpStatusCode.Conflict)
                    continue;

                if (!response.IsSuccessStatusCode)
                    return;

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                if (!doc.RootElement.TryGetProperty("props", out var props) ||
                    !props.TryGetProperty("games", out var games) ||
                    games.ValueKind != JsonValueKind.Object)
                    return;

                foreach (var entry in games.EnumerateObject())
                {
                    var game = entry.Value;

                    if (!game.TryGetProperty("id", out var idElement) || !idElement.TryGetInt32(out int id))
                        continue;

                    int table = game.TryGetProperty("table_nr", out var t) && t.ValueKind == JsonValueKind.Number && t.TryGetInt32(out int nr) ? nr : 0;
                    string status = game.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() ?? "" : "";

                    info.Games[id] = new LiveGameInfo(table, status);
                }

                return;
            }
        }

        private async Task<string?> GetVersionAsync(bool refresh)
        {
            await _versionLock.WaitAsync();
            try
            {
                if (!refresh)
                {
                    _version ??= Preferences.Default.Get(VersionPreferenceKey, "");

                    if (!string.IsNullOrEmpty(_version))
                        return _version;
                }

                string? found = await ReadVersionFromPageAsync();

                if (!string.IsNullOrEmpty(found))
                {
                    _version = found;
                    Preferences.Default.Set(VersionPreferenceKey, found);
                }

                return found;
            }
            finally
            {
                _versionLock.Release();
            }
        }

        /// <summary>
        /// The version is in the page's data-page attribute (near the end of the ~2.4 MB login page). Read in blocks and
        /// stop at the first match. Needed only on the first use and after the site is redeployed.
        /// </summary>
        private static async Task<string?> ReadVersionFromPageAsync()
        {
            using var response = await _http.GetAsync("login", HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var buffer = new char[64 * 1024];
            string tail = "";
            int total = 0;

            while (total < MaxVersionSearchBytes)
            {
                int read = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                    break;

                total += read;

                // Search the new text plus the end of the previous block (the marker may be split between them)
                string text = tail + new string(buffer, 0, read);
                var match = VersionRegex().Match(text);

                if (match.Success)
                    return match.Groups[1].Value;

                tail = text.Length > 200 ? text[^200..] : text;
            }

            return null;
        }

        // data-page="{&quot;component&quot;:...,&quot;version&quot;:&quot;f8050f36...&quot;,...}" (or plain quotes in a script tag)
        [GeneratedRegex("(?:&quot;|\")version(?:&quot;|\")\\s*:\\s*(?:&quot;|\")([0-9a-f]{16,64})(?:&quot;|\")")]
        private static partial Regex VersionRegex();
    }
}

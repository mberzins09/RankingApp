using RankingApp.Core.Models;
using System.Net.Http.Json;

namespace RankingApp.Core.Services
{
    public class TournamentService
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<List<APITournament>> GetPlayerTournamentsAsync(int playerId)
        {
            try
            {
                string url = $"{Data.ApiUrl}competition-events-for-player?player_id={playerId}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Add("x-api-key", Data.ApiKey);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Tournament API failed for player {playerId}");
                    return [];
                }

                var result = await response.Content.ReadFromJsonAsync<APITournamentsResponse>();

                return result?.CompetitionEvents ?? [];
            }
            catch
            {
                return [];
            }
        }

        public async Task<List<APIGame>> GetTournamentGames(int playerId, int tournamentId, string tournamentDate, bool isSeason)
        {
            try
            {
                string url = $"{Data.ApiUrl}competition-event-singles-games?player_id={playerId}&competition_event_id={tournamentId}";
                
                if (isSeason)
                {
                    url += $"&playing_date={tournamentDate}";
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                request.Headers.Add("x-api-key", Data.ApiKey);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Games API failed for tournament {tournamentId}");
                    return [];
                }

                var result = await response.Content.ReadFromJsonAsync<APISinglesResponse>();

                return result?.Games ?? [];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// All teams events (not filtered by player - the player API endpoint returns only singles).
        /// </summary>
        public async Task<List<APITeamsEventListItem>> GetTeamsEventsAsync()
        {
            try
            {
                string url = $"{Data.ApiUrl}competition-events?type=teams";

                var response = await SendWithRetryAsync(url);

                if (response == null || !response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Teams events API failed");
                    return [];
                }

                var result = await response.Content.ReadFromJsonAsync<APITeamsEventsResponse>();

                return result?.CompetitionEvents ?? [];
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Full results of one event (participants + nets with games).
        /// For season events pass playingDate (yyyy-MM-dd) to get only that round.
        /// </summary>
        public async Task<APIEventResultsResponse?> GetEventResultsAsync(int eventId, string? playingDate = null)
        {
            try
            {
                string url = $"{Data.ApiUrl}competition-event-results?competition_event_id={eventId}";

                if (!string.IsNullOrWhiteSpace(playingDate))
                {
                    url += $"&playing_date={playingDate}";
                }

                var response = await SendWithRetryAsync(url);

                if (response == null || !response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Event results API failed for event {eventId}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<APIEventResultsResponse>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Sends GET with API headers. On 429 (rate limit) waits a minute and tries once more.</summary>
        private static async Task<HttpResponseMessage?> SendWithRetryAsync(string url)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", Data.ApiKey);
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                {
                    return response;
                }

                await Task.Delay(61_000);
            }

            return null;
        }
    }
}
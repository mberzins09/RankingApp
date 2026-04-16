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
    }
}
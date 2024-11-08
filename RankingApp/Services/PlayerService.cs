using RankingApp.Models;
using System.Net.Http.Json;

namespace RankingApp.Services
{
    public class PlayerService
    {
        private readonly HttpClient _httpClient;

        public PlayerService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<Player>> GetPlayersAsync(string gender)
        {
            var response = await _httpClient.PostAsJsonAsync("https://www.lgtf.lv/api/getRanking", new { date = (string)null, gender });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlayersResponse>();
                return result?.Players ?? new List<Player>(); ;
            }
            
            return null;
        }
    }

    public class PlayersResponse
    {
        public List<Player> Players { get; set; }
    }
}

using RankingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace RankingApp.Controllers
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
            var response = await _httpClient.PostAsJsonAsync("https://www.lgtf.lv/api/getRanking", new { date = (string)null, gender = gender });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlayersResponse>();
                return result?.Players ?? new List<Player>(); ;  // Ensure you have a proper response model defined
            }

            return null;  // Handle error or return an empty list if needed
        }
    }

    public class PlayersResponse
    {
        public List<Player> Players { get; set; }
    }
}

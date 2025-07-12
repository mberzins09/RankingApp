using RankingApp.Models;
using System.Net.Http.Json;

namespace RankingApp.Services
{
    public class PlayerServiceWithDate
    {
        private readonly HttpClient _httpClient;

        public PlayerServiceWithDate()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<Player>?> GetPlayersAsync(string gender, string date)
        {
            var response = await _httpClient.PostAsJsonAsync("https://www.lgtf.lv/api/getRanking", new { date, gender });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlayersResponseDates>();
                return result?.Players ?? new List<Player>(); ;
            }

            return null;
        }
    }

    public class PlayersResponseDates
    {
        public List<Player>? Players { get; set; }
    }
}

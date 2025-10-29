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
            if (DateTime.TryParseExact(date, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var parsed))
            {
                date = parsed.ToString("yyyy-MM-01");
            }

            string year = date.Split('-')[0];

            var requestBody = new { date, gender, year };

            var response = await _httpClient.PostAsJsonAsync("https://www.lgtf.lv/api/getRanking", requestBody);

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

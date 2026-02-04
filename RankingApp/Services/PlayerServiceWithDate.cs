using RankingApp.Models;
using System.Net.Http.Json;

namespace RankingApp.Services
{
    public class PlayerServiceWithDate
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<List<Player>?> GetPlayersAsync(string gender, string date, bool isOldAPIBody)
        {
            try
            {
                if (DateTime.TryParseExact(date, "yyyy-MM", null,
                    System.Globalization.DateTimeStyles.None, out var parsed))
                {
                    date = isOldAPIBody
                        ? parsed.ToString("yyyy-MM")
                        : parsed.ToString("yyyy-MM-01");
                }

                string year = date.Split('-')[0];

                object requestBody = isOldAPIBody ? new { date, gender } : new { date, gender, year };

                var response = await _httpClient.PostAsJsonAsync("https://www.lgtf.lv/api/getRanking", requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API failed: {date} {gender} {response.StatusCode}");
                    return [];
                }

                var result = await response.Content.ReadFromJsonAsync<PlayersResponseDates>();

                return result?.Players ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public class PlayersResponseDates
    {
        public List<Player>? Players { get; set; }
    }
}

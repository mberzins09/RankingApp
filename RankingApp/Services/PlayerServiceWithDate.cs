using RankingApp.Models;
using System.Net.Http.Json;

namespace RankingApp.Services
{
    public class PlayerServiceWithDate
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<List<Player>> GetPlayersAsync(string gender, string date, bool isOldAPIBody)
        {
            var newApiPlayers = await GetPlayersNewAPI(gender, date);

            if (newApiPlayers != null && newApiPlayers.Count > 0)
                return newApiPlayers;

            var oldApiPlayers = await GetPlayersOldAPI(gender, date, isOldAPIBody);

            if (oldApiPlayers != null && oldApiPlayers.Count > 0)
                return oldApiPlayers;

            return [];
        }

        public async Task<List<Player>?> GetPlayersOldAPI(string gender, string date, bool isOldAPIBody)
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

        public async Task<List<Player>> GetPlayersNewAPI(string gender, string date)
        {
            try
            {
                if (!DateTime.TryParseExact(date, "yyyy-MM", null,
                    System.Globalization.DateTimeStyles.None, out var parsed))
                    return [];

                int year = parsed.Year;
                int month = parsed.Month;

                gender = gender switch
                {
                    "virietis" => "male",
                    "sieviete" => "female",
                    _ => gender
                };

                string url = $"{Data.ApiUrl}ranking-list?ranking_id=2&gender={gender}&year={year}&month={month}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", Data.ApiKey);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"New API failed: {year}-{month} {gender}");
                    return [];
                }

                var result = await response.Content.ReadFromJsonAsync<PlayersResponseNewApi>();

                if (result?.Players == null)
                    return [];

                return result.Players.Select(p => new Player
                {
                    Id = p.Id,
                    PlayerId = p.Id,
                    Place = p.Rank,
                    Points = int.TryParse(p.Points, out var pts) ? pts : 0,
                    PointsWithBonus = int.TryParse(p.PointsWithBonus, out var ptsb) ? ptsb : 0,
                    Name = p.Name,
                    Surname = p.Surname,
                    BirthDate = p.BirthDate,
                    ClubName = p.ClubName,
                    LicenceEndDate = "",
                    LicenceInfo = "",
                    LastPlayedDate = ""
                }).ToList();
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

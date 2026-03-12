using System.Text.Json.Serialization;

namespace RankingApp.Models
{
    public class APISinglesResponse
    {
        [JsonPropertyName("games")]
        public List<APIGame>? Games { get; set; }
    }

    public class APIGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("player1")]
        public APIGamePlayer? Player1 { get; set; }

        [JsonPropertyName("player2")]
        public APIGamePlayer? Player2 { get; set; }

        [JsonPropertyName("winner")]
        public APIGamePlayer? Winner { get; set; }

        [JsonPropertyName("player1_score")]
        public string? Player1Score { get; set; }

        [JsonPropertyName("player2_score")]
        public string? Player2Score { get; set; }
    }

    public class APIGamePlayer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("surname")]
        public string Surname { get; set; }
    }
}

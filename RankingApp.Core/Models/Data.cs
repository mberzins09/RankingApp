namespace RankingApp.Core.Models
{
    public static class Data
    {
        public const string ApiKey = "org_trJaxebjAq9bQjdkPb1PJONCO1Im8befEFv7w8Jr";
        public const string ApiUrl = "https://turniri.lgtf.lv/api/v1/";
        public static int TournamentId { get; set; }
        public static int GameId { get; set; }
        public static DateTime TournamentDate { get; set; }
        public static string DateToString => TournamentDate.ToString("d MMM yyyy");
    }
}

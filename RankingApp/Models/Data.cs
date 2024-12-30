namespace RankingApp.Models
{
    public static class Data
    {
        public static int TournamentId { get; set; }
        public static int GameId { get; set; }
        public static DateTime TournamentDate { get; set; }
        public static string DateToString => TournamentDate.ToString("d MMM yyyy");
    }
}

namespace RankingApp.Models
{
    public class LgtfGame
    {
        public int id { get; set; }
        public int competition_id { get; set; }

        public int player1_id { get; set; }
        public int player2_id { get; set; }

        public int player1_sets { get; set; }
        public int player2_sets { get; set; }

        public int winner { get; set; }
    }
}

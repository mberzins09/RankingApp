using SQLite;

namespace RankingApp.Models
{
    public class Tournament
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Coefficient { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string DateToString => Date.ToString("d MMM yyyy");
        public string TournamentPlayerName { get; set; }
        public string TournamentPlayerSurname { get; set; }
        public int TournamentPlayerPoints { get; set; }
        public int PointsDifference { get; set; }
        public int TournamentPlayerId { get; set; }
        public string TournamentDisplay => 
            $"{TournamentPlayerName} {TournamentPlayerSurname} - {DateToString} : {PointsDifference}";
    }
}

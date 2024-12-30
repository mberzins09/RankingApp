using SQLite;

namespace RankingApp.Models
{
    public class Game
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int MyPoints { get; set; }
        public string MyName { get; set; }
        public string MySurname { get; set; }
        public string MyFullName => $"{MyName} {MySurname}";
        public int OpponentPoints { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public int? MySets { get; set; }
        public int? OpponentSets { get; set; }
        public string? OpponentName => $"{Name} {Surname}";
        public bool IsWin => MySets > OpponentSets;
        public int TournamentId { get; set; }
        public string? TournamentName { get; set; }
        public DateTime TournamentDate { get; set; }
        public string DateToString => TournamentDate.ToString("d MMM yyyy");
        public string GameCoefficient { get; set; }
        public string GameScore => $"{MySets} : {OpponentSets}";
        public string GameName => $"{TournamentName} - {DateToString}";
        public string GameDisplayPlayers => $"{MyFullName} - {OpponentName} {GameScore}";
        public string GameDisplayDetails => $"{GameName} : {RatingDifference}";
        public bool IsOpponentForeign {  get; set; }
        public int RatingDifference => IsOpponentForeign ? 0 : RatingCalculator.Calculate(MyPoints, OpponentPoints, IsWin, GameCoefficient);
    }
}

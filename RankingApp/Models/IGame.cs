namespace RankingApp.Models
{
    public interface IGame
    {
        int Id { get; }
        int TournamentId { get; }
        string GameDisplayMe { get; }
        string GameDisplayOpp { get; }
        string Gamescore { get; }
        DateTime TournamentDate { get; }
        string TournamentName { get; }
        string GameDate { get; }
        int RatingDifference { get; }
        int MyPoints { get; }
        int OpponentPoints { get; }
        bool IsWin { get; }
        int? MySets { get; }
        int? OpponentSets { get; }
    }
}

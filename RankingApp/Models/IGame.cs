namespace RankingApp.Models
{
    public interface IGame
    {
        int Id { get; }
        int TournamentId { get; }
        string GameDisplayPlayers { get; }
        string Gamescore { get; }
        DateTime TournamentDate { get; }
        string TournamentName { get; }
        string GameDate { get; }
        int RatingDifference { get; }
        bool IsWin { get; }
        int? MySets { get; }
        int? OpponentSets { get; }
    }
}

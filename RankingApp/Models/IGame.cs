namespace RankingApp.Models
{
    public interface IGame
    {
        int Id { get; }
        int TournamentId { get; }
        string GameDisplayPlayers { get; }
        string GameDisplayDetails { get; }
        string GameName { get; }
        string Gamescore { get; }
        DateTime TournamentDate { get; }
        int RatingDifference { get; }
    }
}

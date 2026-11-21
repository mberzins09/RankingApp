using RankingApp.Core.Models;

namespace RankingApp.Core.Interfaces
{
    /// <summary>
    /// All page-to-page navigation. Ids are passed as real navigation parameters (read by the
    /// view models through IQueryAttributable) and also written to <see cref="Data"/> as a fallback.
    /// "Go to X" goes BACK when X is the previous page, so the back stack does not keep growing
    /// and list pages keep their filters.
    /// </summary>
    public interface INavigationService
    {
        Task GoToAsync(string route);

        Task GoToAsync(string route, IDictionary<string, object> parameters);

        Task GoBackAsync();

        /// <summary>Opens a tournament (tournamentId is passed to TournamentView).</summary>
        Task GoToTournamentAsync(int tournamentId);

        /// <summary>Opens a singles or doubles game (gameId + tournamentId are passed).</summary>
        Task GoToGameAsync(IGame game);

        /// <summary>Opens the "change tournament player" page for a tournament.</summary>
        Task GoToEditTournamentPlayerAsync(int tournamentId);

        /// <summary>
        /// Leaves the current page (game / edit player) for its tournament: goes back if the
        /// previous page is TournamentView, otherwise replaces the current page with it.
        /// </summary>
        Task ReturnToTournamentAsync(int tournamentId);

        /// <summary>Goes back to the tournament list if it is the previous page, otherwise opens it.</summary>
        Task GoToTournamentListAsync();
    }
}

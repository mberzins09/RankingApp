using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Views;

namespace RankingApp.Services
{
    /// <summary>
    /// Shell navigation with parameters. Uses ShellNavigationQueryParameters: they are applied once,
    /// so coming back to a page later does not re-apply old ids. Every id is also written to Data
    /// (the old mechanism), so a page that gets no parameter still knows what to show.
    /// </summary>
    public class NavigationService : INavigationService
    {
        public Task GoToAsync(string route)
        {
            return Shell.Current.GoToAsync(route);
        }

        public Task GoToAsync(string route, IDictionary<string, object> parameters)
        {
            return Shell.Current.GoToAsync(route, ToShellParameters(parameters));
        }

        public Task GoBackAsync()
        {
            return Shell.Current.GoToAsync("..");
        }

        public Task GoToTournamentAsync(int tournamentId)
        {
            Data.TournamentId = tournamentId;

            var parameters = Parameters((NavParams.TournamentId, tournamentId));

            // Already came from that page (e.g. Tournament → Game → "Tournament") → go back to it
            return PreviousPageIs<TournamentView>()
                ? Shell.Current.GoToAsync("..", parameters)
                : Shell.Current.GoToAsync(nameof(TournamentView), parameters);
        }

        public Task GoToGameAsync(IGame game)
        {
            Data.TournamentId = game.TournamentId;
            Data.GameId = game.Id;

            var parameters = Parameters(
                (NavParams.GameId, game.Id),
                (NavParams.TournamentId, game.TournamentId));

            string route = game is DoublesGame ? nameof(DoublesGameView) : nameof(GameView);
            return Shell.Current.GoToAsync(route, parameters);
        }

        public Task GoToEditTournamentPlayerAsync(int tournamentId)
        {
            Data.TournamentId = tournamentId;

            return Shell.Current.GoToAsync(nameof(EditTournamentPlayer),
                Parameters((NavParams.TournamentId, tournamentId)));
        }

        public Task ReturnToTournamentAsync(int tournamentId)
        {
            Data.TournamentId = tournamentId;

            var parameters = Parameters((NavParams.TournamentId, tournamentId));

            // Opened from the tournament → simply go back to it.
            // Opened from somewhere else (All games, Rankings, tournament list) → replace this page
            // with the tournament, so "back" afterwards returns to that list with its filters.
            return PreviousPageIs<TournamentView>()
                ? Shell.Current.GoToAsync("..", parameters)
                : Shell.Current.GoToAsync($"../{nameof(TournamentView)}", parameters);
        }

        public Task GoToTournamentListAsync()
        {
            return PreviousPageIs<AllTournaments>()
                ? Shell.Current.GoToAsync("..")
                : Shell.Current.GoToAsync(nameof(AllTournaments));
        }

        // ── helpers ──────────────────────────────────────────────────────

        private static bool PreviousPageIs<TPage>() where TPage : Page
        {
            var stack = Shell.Current?.Navigation?.NavigationStack;

            return stack is { Count: >= 2 } && stack[^2] is TPage;
        }

        private static ShellNavigationQueryParameters Parameters(params (string Key, object Value)[] values)
        {
            var parameters = new ShellNavigationQueryParameters();

            foreach (var (key, value) in values)
                parameters[key] = value;

            return parameters;
        }

        private static ShellNavigationQueryParameters ToShellParameters(IDictionary<string, object> values)
        {
            var parameters = new ShellNavigationQueryParameters();

            foreach (var pair in values)
                parameters[pair.Key] = pair.Value;

            return parameters;
        }
    }
}

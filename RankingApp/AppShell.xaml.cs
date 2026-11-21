using RankingApp.Views;

namespace RankingApp
{

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(AllPlayerRanking), typeof(AllPlayerRanking));
            Routing.RegisterRoute(nameof(TournamentView), typeof(TournamentView));
            Routing.RegisterRoute(nameof(GameView), typeof(GameView));
            Routing.RegisterRoute(nameof(AllGames), typeof(AllGames));
            Routing.RegisterRoute(nameof(EditTournamentPlayer), typeof(EditTournamentPlayer));
            Routing.RegisterRoute(nameof(AllTournaments), typeof(AllTournaments));
            Routing.RegisterRoute(nameof(DoublesGameView), typeof(DoublesGameView));
            Routing.RegisterRoute(nameof(ImportTournament), typeof(ImportTournament));
            Routing.RegisterRoute(nameof(CalendarPage), typeof(CalendarPage));
            Routing.RegisterRoute(nameof(CalendarEventPage), typeof(CalendarEventPage));
            Routing.RegisterRoute(nameof(ResultsPage), typeof(ResultsPage));
            Routing.RegisterRoute(nameof(EventResultsPage), typeof(EventResultsPage));
        }
    }
}

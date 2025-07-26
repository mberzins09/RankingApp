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
        }
    }
}

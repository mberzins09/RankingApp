using RankingApp.Views;

namespace RankingApp
{

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(WomensRanking), typeof(WomensRanking));
            Routing.RegisterRoute(nameof(MensRanking), typeof(MensRanking));
            Routing.RegisterRoute(nameof(AllPlayerRanking), typeof(AllPlayerRanking));
            Routing.RegisterRoute(nameof(Tournaments), typeof(Tournaments));
            Routing.RegisterRoute(nameof(Games), typeof(Games));
            Routing.RegisterRoute(nameof(AllGames), typeof(AllGames));
            Routing.RegisterRoute(nameof(AddTournament), typeof(AddTournament));
            Routing.RegisterRoute(nameof(InactivePlayers), typeof(InactivePlayers));
            Routing.RegisterRoute(nameof(AllTournaments), typeof(AllTournaments));
        }
    }
}

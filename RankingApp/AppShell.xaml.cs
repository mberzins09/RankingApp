using RankingApp.Views;

namespace RankingApp
{

    public partial class AppShell : Shell
    {

        public AppShell()
        {
            InitializeComponent();
            
            Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
            Routing.RegisterRoute(nameof(WomensRanking), typeof(WomensRanking));
            Routing.RegisterRoute(nameof(MensRanking), typeof(MensRanking));
            Routing.RegisterRoute(nameof(AddTournament), typeof(AddTournament));
            Routing.RegisterRoute(nameof(AddGame), typeof(AddGame));
            Routing.RegisterRoute(nameof(EditGame), typeof(EditGame));
            Routing.RegisterRoute(nameof(EditTournament), typeof(EditTournament));
            Routing.RegisterRoute(nameof(AllTournaments), typeof(AllTournaments));
            
        }

    }
}

using RankingApp.Data_Storage;
using RankingApp.Services;
using RankingApp.ViewModels;

namespace RankingApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            _ = CheckAndFillPlayersAsync();
        }

        private async Task CheckAndFillPlayersAsync()
        {
            var dbService = new DatabaseService();
            var playerRepo = new PlayerReposotoryWithDate(new PlayerServiceWithDate());
            var playerViewModel = new PlayerViewModel(dbService, playerRepo);

            var allPlayers = await dbService.GetPlayersAsync();
            var count = allPlayers.Count;
            var over5999 = allPlayers.Count(p => p.OverallPlace > 5999);

            if (count == 0 || over5999 < 100)
            {
                await playerViewModel.FillDatabaseWithOldRankings();
            }
        }
    }
}

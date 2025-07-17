using RankingApp.Data_Storage;
using RankingApp.Services;
using RankingApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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
            var playerService = new PlayerService(new DatabaseService(), new PlayerReposotoryWithDate(new PlayerServiceWithDate()));

            var allPlayers = await playerService.GetPlayersFromDbAsync();
            var count = allPlayers.Count;
            var over5999 = allPlayers.Count(p => p.OverallPlace > 5999);

            if (count == 0 || over5999 < 100)
            {
                await playerService.FillDatabaseWithOldRankingsAsync();
            }
        }
    }
}

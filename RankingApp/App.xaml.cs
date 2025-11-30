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
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using RankingApp.Data_Storage;
using RankingApp.Services;
using RankingApp.ViewModels;
using RankingApp.Views;

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

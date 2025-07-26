using Microsoft.Extensions.Logging;
using RankingApp.Data_Storage;
using RankingApp.Services;
using RankingApp.ViewModels;
using RankingApp.Views;

namespace RankingApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<PlayerReposotoryWithDate>();
            builder.Services.AddSingleton<PlayerServiceWithDate>();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<PlayerService>();

            builder.Services.AddSingleton<PlayerViewModel>();
            builder.Services.AddSingleton<GameViewModel>();
            builder.Services.AddSingleton<TournamentViewModel>();
            builder.Services.AddSingleton<AllTournamentsViewModel>();
            builder.Services.AddSingleton<AllGamesViewModel>();
            builder.Services.AddSingleton<EditTournamentPlayerViewModel>();

            builder.Services.AddTransient<AllPlayerRanking>();
            builder.Services.AddTransient<TournamentView>();
            builder.Services.AddTransient<GameView>();
            builder.Services.AddTransient<AllTournaments>();
            builder.Services.AddTransient<AllGames>();
            builder.Services.AddTransient<EditTournamentPlayer>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

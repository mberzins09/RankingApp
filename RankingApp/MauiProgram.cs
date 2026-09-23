using CommunityToolkit.Maui;
using Maui.Android.InAppUpdates;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using RankingApp.Core.ViewModels;
using RankingApp.Services;
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
                .UseMauiCommunityToolkit()
                .UseAndroidInAppUpdates()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IPlayerRepositoryWithDate, PlayerReposotoryWithDate>();
            builder.Services.AddSingleton<IProgressDialogService, ProgressDialogService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<PlayerServiceWithDate>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IPlayerService, PlayerService>();
            builder.Services.AddSingleton<IMonthPlayersService, MonthPlayersService>();
            builder.Services.AddSingleton<TournamentImportService>();
            builder.Services.AddSingleton<TournamentService>();
            builder.Services.AddSingleton<ApiGameImporterService>();

            builder.Services.AddSingleton<PlayerViewModel>();
            builder.Services.AddSingleton<GameViewModel>();
            builder.Services.AddSingleton<TournamentViewModel>();
            builder.Services.AddSingleton<AllTournamentsViewModel>();
            builder.Services.AddSingleton<AllGamesViewModel>();
            builder.Services.AddSingleton<EditTournamentPlayerViewModel>();
            builder.Services.AddSingleton<DoublesGameViewModel>();
            builder.Services.AddSingleton<ImportTournamentViewModel>();
            builder.Services.AddSingleton<HomeViewModel>();

            builder.Services.AddTransient<AllPlayerRanking>();
            builder.Services.AddTransient<TournamentView>();
            builder.Services.AddTransient<GameView>();
            builder.Services.AddTransient<AllTournaments>();
            builder.Services.AddTransient<AllGames>();
            builder.Services.AddTransient<EditTournamentPlayer>();
            builder.Services.AddTransient<DoublesGameView>();
            builder.Services.AddTransient<ImportTournament>();
            builder.Services.AddTransient<HomePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<Entry, EntryHandler>();
                EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    handler.PlatformView.Background = null; // removes underline
                    handler.PlatformView.SetPadding(0, 0, 0, 0);
                });

                PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    handler.PlatformView.Background = null;
                    handler.PlatformView.SetPadding(0, 0, 0, 0);
                });

                DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    handler.PlatformView.Background = null;
                    handler.PlatformView.SetPadding(0, 0, 0, 0);
                });

                EditorHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    handler.PlatformView.Background = null;
                });

                // SearchView draws its own underline ("search_plate") - hide it, the glass border replaces it
                SearchBarHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                {
                    var context = handler.PlatformView.Context;
                    if (context?.Resources == null)
                        return;

                    int plateId = context.Resources.GetIdentifier("search_plate", "id", context.PackageName);
                    if (plateId == 0)
                        plateId = context.Resources.GetIdentifier("android:id/search_plate", null, null);

                    var plate = plateId != 0 ? handler.PlatformView.FindViewById(plateId) : null;
                    plate?.SetBackgroundColor(Android.Graphics.Color.Transparent);
                });
#endif
            });

            return builder.Build();
        }
    }
}

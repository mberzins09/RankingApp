using System.Reflection;
using RankingApp.ViewModels;
using RankingApp.Models;
using RankingApp.Services;

namespace RankingApp.Views
{
    public class BaseContentPage : ContentPage
    {
        private readonly ToolbarItem _homeToolbar;
        private readonly ToolbarItem _rankingsToolbar;
        private readonly ToolbarItem _gamesToolbar;
        private readonly ToolbarItem _addToolbar;
        private readonly ToolbarItem _importToolbar;
        public BaseContentPage()
        {
            _homeToolbar = new ToolbarItem
            {
                Text = "Home",
                IconImageSource = "home.png",
                Order = ToolbarItemOrder.Primary,
                Priority = 0
            };
            _homeToolbar.Command = new Command(async () => await SaveAndNavigateAsync(nameof(AllTournaments), typeof(AllTournaments)));

            _rankingsToolbar = new ToolbarItem
            {
                Text = "Rankings",
                IconImageSource = "ranksnoborder.png",
                Order = ToolbarItemOrder.Primary,
                Priority = 1
            };
            _rankingsToolbar.Command = new Command(async () => await SaveAndNavigateAsync(nameof(AllPlayerRanking), typeof(AllPlayerRanking)));

            _gamesToolbar = new ToolbarItem
            {
                Text = "Tournament",
                IconImageSource = "gamesnoborder.png",
                Order = ToolbarItemOrder.Primary,
                Priority = 2
            };
            _gamesToolbar.Command = new Command(async () => await EnsureTournamentIdAndNavigateAsync(nameof(TournamentView), typeof(TournamentView)));

            _addToolbar = new ToolbarItem
            {
                Text = "Add",
                IconImageSource = "plusnoborder.png",
                Order = ToolbarItemOrder.Primary,
                Priority = 3
            };
            _addToolbar.Command = new Command(async () => await AddActionAsync());

            _importToolbar = new ToolbarItem
            {
                Text = "Import",
                IconImageSource = "importfromapi.png",
                Order = ToolbarItemOrder.Primary,
                Priority = 4
            };
            _importToolbar.Command = new Command(async () => await ImportActionAsync());

            ToolbarItems.Add(_homeToolbar);
            ToolbarItems.Add(_rankingsToolbar);
            ToolbarItems.Add(_gamesToolbar);
            ToolbarItems.Add(_addToolbar);
            ToolbarItems.Add(_importToolbar);

            if (Shell.Current != null)
                Shell.Current.Navigated += Shell_Navigated;

            UpdateToolbarState();
        }

        private void Shell_Navigated(object? sender, ShellNavigatedEventArgs e) => UpdateToolbarState();

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (Shell.Current != null)
                Shell.Current.Navigated -= Shell_Navigated;
        }

        private void UpdateToolbarState()
        {
            try
            {
                var current = Shell.Current?.CurrentPage;
                var currentType = current?.GetType();

                _homeToolbar.IsEnabled = currentType != typeof(AllTournaments);
                _rankingsToolbar.IsEnabled = currentType != typeof(AllPlayerRanking);
                _gamesToolbar.IsEnabled = currentType != typeof(TournamentView);
                _addToolbar.IsEnabled = currentType == typeof(AllTournaments) || currentType == typeof(TournamentView);
                _importToolbar.IsEnabled = currentType != typeof(ImportTournament);
            }
            catch
            {
            }
        }

        private async Task SaveInterfaceAsync()
        {
            var vm = BindingContext;

            if (vm is ISaveBeforeNavigate saver)
            {
                var proceed = await saver.SaveBeforeNavigateAsync();
                if (!proceed) return;
            }
        }

        private async Task SaveAndNavigateAsync(string routeName, Type destinationType)
        {
            try
            {
                await SaveInterfaceAsync();
            }
            catch
            {
            }

            var currentType = Shell.Current?.CurrentPage?.GetType();
            if (currentType == destinationType)
                return;

            await Shell.Current.GoToAsync(routeName);
        }

        private async Task EnsureTournamentIdAndNavigateAsync(string routeName, Type destinationType)
        {
            var db = new DatabaseService();
            var tournaments = await db.GetAllRecordsAsync<Tournament>();

            var deletedTournament = await db.GetByIdAsync<Tournament>(Data.TournamentId);
            if (deletedTournament == null)
            {
                Data.TournamentId = 0;
            }

            if (Data.TournamentId == 0)
            {
                try
                {
                    if (db != null)
                    {
                        var last = tournaments.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).FirstOrDefault();
                        if (last != null)
                            Data.TournamentId = last.Id;
                    }
                }
                catch
                {
                }
            }

            await SaveInterfaceAsync();

            var currentType = Shell.Current?.CurrentPage?.GetType();
            if (currentType == destinationType)
                return;

            await Shell.Current.GoToAsync(routeName);
        }

        private async Task AddActionAsync()
        {
            await SaveInterfaceAsync();

            var current = Shell.Current?.CurrentPage;
            var vm = current?.BindingContext;

            if (current == null) return;

            try
            {
                if (current.GetType() == typeof(AllTournaments))
                {
                    if (vm is AllTournamentsViewModel allTournamentsvm)
                    {
                        await allTournamentsvm.CreateNewTournamentSave();
                    }

                    await Shell.Current.GoToAsync(nameof(TournamentView));
                    return;
                }

                if (current.GetType() == typeof(TournamentView))
                {
                    if (vm is TournamentViewModel tournamentvm)
                    {
                        await tournamentvm.CreateNewGameAsync(false);
                    }

                    await Shell.Current.GoToAsync(nameof(GameView));
                    return;
                }
            }
            catch
            {
            }
        }

        private async Task ImportActionAsync()
        {
            var current = Shell.Current?.CurrentPage;

            if (current?.BindingContext is TournamentViewModel vm)
            {
                await vm.ImportGamesForTournamentAsync();
                return;
            }

            await SaveAndNavigateAsync(nameof(ImportTournament), typeof(ImportTournament));
        }
    }
}
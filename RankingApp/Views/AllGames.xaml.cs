using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;

namespace RankingApp.Views;

public partial class AllGames : ContentPage
{
    private readonly AllGamesViewModel _viewModel;
    public AllGames(AllGamesViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        GamesSearchBar.Text = String.Empty;
    }

    private void GamesSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;

        var games = new ObservableCollection<Game>
            (_viewModel.SearchTournaments(_viewModel.GamesList, search));

        ListViewGames.ItemsSource = games;
    }

    private async void MenuItem_OnClicked(object? sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        if (menuItem != null) 
        {
            var game = menuItem.CommandParameter as Game;
            if (game != null)
            {
                await _viewModel.DeleteGame(game);
            }
        }

        ListViewGames.ItemsSource = await _viewModel.GetGames();
    }
}
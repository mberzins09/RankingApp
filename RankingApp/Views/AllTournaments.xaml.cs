using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;

namespace RankingApp.Views;

public partial class AllTournaments : ContentPage
{
    private readonly AllTournamentsViewModel _viewModel;
    public AllTournaments(AllTournamentsViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        TournamentsSearchBar.Text = String.Empty;
    }

    private void TournamentsSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;
        var tournaments = new ObservableCollection<Tournament>
            (_viewModel.SearchTournaments(_viewModel.TournamentsList, search));

        ListViewTournaments.ItemsSource = tournaments;
    }

    private void ListViewTournaments_OnItemSelected(object? sender, SelectedItemChangedEventArgs e)
    {
        var tournament = ListViewTournaments.SelectedItem as Tournament;
        if (tournament != null)
        {
            Data.TournamentId = tournament.Id;
        }

        Shell.Current.GoToAsync(nameof(AddTournament));
    }

    private async void MenuItem_OnClicked(object? sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        if (menuItem != null) 
        {
            var tournament = menuItem.CommandParameter as Tournament;
            if (tournament != null) 
            {
                var games = await _viewModel.GetGames(tournament.Id);
                for (int i = 0; i < games.Count; i++)
                {
                    await _viewModel.DeleteGame(games[i]);
                }
                await _viewModel.DeleteTournament(tournament);
            }
        }

        ListViewTournaments.ItemsSource = await _viewModel.GetTournaments();
    }
}
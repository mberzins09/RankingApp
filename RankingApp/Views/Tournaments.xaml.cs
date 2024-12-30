using RankingApp.Models;
using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class Tournaments : ContentPage
{
    private readonly TournamentViewModel _viewModel;
    private readonly RankingAppsDatabase _database;

    public Tournaments(TournamentViewModel viewModel, RankingAppsDatabase database)
	{
		InitializeComponent();
        _viewModel = viewModel;
        _database = database;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadGamesAsync();
        ListViewTournamentGames.ItemsSource = _viewModel.Games;
        LabelTournamentDate.Text = _viewModel.Tournament.Date.ToString("d MMM yyyy");
        LabelTournamentCoefficient.Text = _viewModel.Tournament.Coefficient;
        LabelTournamentName.Text = _viewModel.Tournament.Name;
        LabelTournamentPlayer.Text = $"{_viewModel.Tournament.TournamentPlayerName} {_viewModel.Tournament.TournamentPlayerSurname}";
        LabelPointsDifference.Text = _viewModel.Tournament.PointsDifference.ToString();
    }

    private async void BtnAddGame_OnClicked(object? sender, EventArgs e)
    {
        var game = new Game()
        {
            MyName = _viewModel.Tournament.TournamentPlayerName,
            MySurname = _viewModel.Tournament.TournamentPlayerSurname,
            MyPoints = _viewModel.Tournament.TournamentPlayerPoints,
            GameCoefficient = _viewModel.Tournament.Coefficient,
            TournamentDate = _viewModel.Tournament.Date,
            IsOpponentForeign = false,
            OpponentPoints = 0,
            TournamentId = _viewModel.Tournament.Id,
            TournamentName = _viewModel.Tournament.Name
        };
        await _database.SaveGameAsync(game);
        Data.GameId = game.Id;
        await Shell.Current.GoToAsync(nameof(Games));
    }

    private async void BtnSave_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveTournamentAsync();
        await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
    }

    private async void MenuItem_OnClicked(object? sender, EventArgs e)
    {
        var menuItem = sender as MenuItem;
        if (menuItem != null) 
        {
            var game = menuItem.CommandParameter as Game;
            if (game != null) 
            {
                await _database.DeleteGameAsync(game);
            }
        }

        await _viewModel.LoadGamesAsync();
        ListViewTournamentGames.ItemsSource = _viewModel.Games;
        LabelPointsDifference.Text = _viewModel.Tournament.PointsDifference.ToString();
    }

    private async void ListViewTournamentGames_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        var game = ListViewTournamentGames.SelectedItem as Game;
        if (game != null) { 
        Data.GameId = game.Id;
        }
        await Shell.Current.GoToAsync(nameof(Games));
    }
}
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
        await _viewModel.LoadTournamentDifference();
        await _viewModel.LoadGamesAsync();
        LabelTournamentDate.Text = Data.TournamentDate.ToString("d MMM yyyy");
        LabelTournamentCoefficient.Text = Data.Coefficient.ToString();
        LabelTournamentName.Text = Data.TournamentName;
        LabelTournamentPlayer.Text = $"{Data.TournamentPlayerName} {Data.TournamentPlayerSurname}";
        LabelPointsDifference.Text = _viewModel.TournamentDifference.ToString();
    }

    private async void BtnAddGame_OnClicked(object? sender, EventArgs e)
    {
        var game = new Models.Game();
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
        var game = menuItem.CommandParameter as Game;
        await _database.DeleteGameAsync(game);
        await _viewModel.LoadGamesAsync();
        await _viewModel.LoadTournamentDifference();
        LabelPointsDifference.Text = _viewModel.TournamentDifference.ToString();
    }
}
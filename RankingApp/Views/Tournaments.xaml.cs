using RankingApp.Models;
using RankingApp.Services;
using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class Tournaments : ContentPage
{
    private readonly TournamentViewModel _viewModel;

    public Tournaments(TournamentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadGamesAsync();
    }

    private async void BtnAddGame_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.CreateNewGameSave();
        await Shell.Current.GoToAsync(nameof(Games));
    }

    private async void BtnSave_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveTournamentAsync();
        await Shell.Current.GoToAsync(nameof(AllTournaments));
    }
}
using RankingApp.Models;
using RankingApp.ViewModels;
using System.Collections.ObjectModel;

namespace RankingApp.Views;

public partial class EditTournamentPlayer : ContentPage
{
    private readonly EditTournamentPlayerViewModel _viewModel;
    public EditTournamentPlayer(EditTournamentPlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
        _viewModel.SearchText = String.Empty;
    }

    private async void ButtonSave_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TournamentView));
    }
}
using RankingApp.Models;
using RankingApp.ViewModels;
using System.Collections.ObjectModel;

namespace RankingApp.Views;

public partial class AddTournament : ContentPage
{
    private readonly AddTournamentViewModel _viewModel;
    public AddTournament(AddTournamentViewModel viewModel)
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

    private async void ButtonAdd_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Tournaments));
    }
}
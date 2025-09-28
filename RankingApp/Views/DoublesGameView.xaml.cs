using RankingApp.Models;
using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class DoublesGameView : ContentPage
{
    private readonly DoublesGameViewModel _viewModel;

    public DoublesGameView(DoublesGameViewModel viewModel)
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

    private async void ButtonGameSave_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveDoublesGameAsync();
        await Shell.Current.GoToAsync(nameof(TournamentView));
    }
}
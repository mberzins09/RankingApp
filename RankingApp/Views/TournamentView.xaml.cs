using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class TournamentView : ContentPage
{
    private readonly TournamentViewModel _viewModel;

    public TournamentView(TournamentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }

    private async void BtnAddGame_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.CreateNewGameSave();
        await Shell.Current.GoToAsync(nameof(GameView));
    }

    private async void BtnAddDoublesGame_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.CreateNewDoublesGameSave();
        await Shell.Current.GoToAsync(nameof(DoublesGameView));
    }

    private async void BtnSave_OnClicked(object? sender, EventArgs e)
    {
        await _viewModel.SaveTournamentAsync();
        await Shell.Current.GoToAsync(nameof(AllTournaments));
    }
}
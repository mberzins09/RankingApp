using RankingApp.Core.ViewModels;

namespace RankingApp.Views;

public partial class DoublesGameView : BaseContentPage
{
    private readonly DoublesGameViewModel _viewModel;

    public DoublesGameView(DoublesGameViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Last safety net: whatever way the page is left, the game is stored
        await _viewModel.SaveDoublesGameAsync();
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
        await _viewModel.ReturnToTournamentAsync();
    }
}
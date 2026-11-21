using RankingApp.Core.ViewModels;

namespace RankingApp.Views;

public partial class AllGames : BaseContentPage
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
        // Search text is kept, like the other filters, when coming back to this page
    }
}
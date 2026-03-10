using RankingApp.ViewModels;

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
        _viewModel.SearchText = String.Empty;
    }
}
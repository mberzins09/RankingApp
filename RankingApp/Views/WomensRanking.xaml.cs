using RankingApp.Controllers;

namespace RankingApp.Views;

public partial class WomensRanking : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    public WomensRanking(PlayerViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Womens.Count == 0)
        {
            await _viewModel.LoadDataWomens(); // Load data when page appears
        }
    }
}
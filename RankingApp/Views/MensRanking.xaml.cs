using RankingApp.Controllers;

namespace RankingApp.Views;

public partial class MensRanking : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    public MensRanking(PlayerViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Mens.Count == 0)
        {
            await _viewModel.LoadDataMens(); // Load data when page appears
        }
    }
}
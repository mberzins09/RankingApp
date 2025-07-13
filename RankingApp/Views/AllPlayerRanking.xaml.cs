using RankingApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace RankingApp.Views;

public partial class AllPlayerRanking : ContentPage
{
    private readonly PlayerViewModel _viewModel;

    public AllPlayerRanking(PlayerViewModel viewModel)
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
}
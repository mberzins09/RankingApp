using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;

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

        WomensSearchBar.Text = String.Empty;

        Womens.ItemsSource = await _viewModel.GetWomenPlayers();
    }

    private void WomensSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;

        var players = new ObservableCollection<PlayerDB>
            (_viewModel.SearchPlayers(_viewModel.WomensList, search));

        Womens.ItemsSource = players;
    }
}
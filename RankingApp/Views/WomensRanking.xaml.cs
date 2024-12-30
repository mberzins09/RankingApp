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
        await _viewModel.LoadPlayersAsyncFromDatabase();
        WomensSearchBar.Text = String.Empty;
    }

    private async void WomensSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;
        var list = await _viewModel.GetWomenPlayers();
        var players = new ObservableCollection<PlayerDB>
            (_viewModel.SearchPlayers(list, search));

        Womens.ItemsSource = players;
    }
}
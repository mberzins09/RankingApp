using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;

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
        await _viewModel.LoadPlayersAsyncFromDatabase();
        AllPlayerSearchBar.Text = String.Empty;
    }

    private async void AllPlayerSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;
        var list = await _viewModel.GetAllPlayers();
        var players = new ObservableCollection<PlayerDB>(_viewModel.SearchPlayersAllRanking(list, search));
        ListViewPlayers.ItemsSource = players;
    }
}
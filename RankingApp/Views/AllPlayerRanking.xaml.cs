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

        AllPlayerSearchBar.Text = String.Empty;

        ListViewPlayers.ItemsSource = await _viewModel.GetAllPlayers();
    }

    private void AllPlayerSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;

        var players = new ObservableCollection<PlayerDB>
            (_viewModel.SearchPlayers(_viewModel.PlayersList, search));

        ListViewPlayers.ItemsSource = players;
    }
}
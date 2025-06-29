using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;

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
        await _viewModel.LoadPlayersAsyncFromDatabase();
        MensSearchBar.Text = String.Empty;
    }

    private async void MensSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;
        var list = await _viewModel.GetMenPlayers();
        var players = new ObservableCollection<PlayerDB>
            (_viewModel.SearchPlayers(list, search));

        Mens.ItemsSource = players;
    }
}
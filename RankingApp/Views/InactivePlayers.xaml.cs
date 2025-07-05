using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;

namespace RankingApp.Views;

public partial class InactivePlayers : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    public InactivePlayers(PlayerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPlayersAsyncFromDatabase();
        InactivePlayersSearchBar.Text = String.Empty;
    }

    private async void InactivePlayersSearchBar_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var search = sender == null ? String.Empty : ((SearchBar)sender).Text;
        var list = await _viewModel.GetAllInactivePlayers();
        var players = new ObservableCollection<PlayerDB>
            (_viewModel.SearchPlayers(list, search));

        Inactives.ItemsSource = players;
    }
}
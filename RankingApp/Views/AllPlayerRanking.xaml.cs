using RankingApp.ViewModels;
using System.Collections.ObjectModel;
using RankingApp.Models;
using RankingApp.Data_Storage;
using RankingApp.Services;

namespace RankingApp.Views;

public partial class AllPlayerRanking : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private readonly PlayerRepository _repository;
    private readonly DatabaseService _databaseService;
    public AllPlayerRanking(PlayerViewModel viewModel, PlayerRepository repository, DatabaseService databaseService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _repository = repository;
        _databaseService = databaseService;
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

    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await GetLastestRankings(); // Your optimized sync function
            await Application.Current.MainPage.DisplayAlert("Success", "Done", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong:\n{ex.Message}", "OK");
        }
    }

    private async Task GetLastestRankings()
    {
        var apiPlayers = await _repository.GetPlayersAsync();
        List<PlayerDB> dbPlayers = await _databaseService.GetPlayersAsync();

        var apiPlayerIds = new HashSet<int>(apiPlayers.Select(p => p.Id));

        var missingPlayers = dbPlayers.Where(dbPlayer => !apiPlayerIds.Contains(dbPlayer.Id)).ToList();

        foreach (var player in missingPlayers)
        {
            await _databaseService.UpdatePlayerAsync(player);
        }

        await _databaseService.UpsertPlayersAsync(apiPlayers);
    }
}
using RankingApp.Data_Storage;
using RankingApp.Models;
using RankingApp.Services;
using RankingApp.ViewModels;

namespace RankingApp.Views;

public partial class HomePage : ContentPage
{
    private readonly PlayerRepository _repository;
    private readonly RankingAppsDatabase _database;
    private readonly DatabaseService _databaseService;

    public HomePage(PlayerRepository repository, RankingAppsDatabase database, DatabaseService databaseService)
    {
        InitializeComponent();
        _repository = repository;
        _database = database;
        _databaseService = databaseService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    private async void BtnWomensRank_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(WomensRanking));
    }

    private async void BtnMensRank_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MensRanking));
    }

    private async void BtnAddTournament_OnClicked(object? sender, EventArgs e)
    {
        var tournament = new Models.Tournament();
        await _database.SaveTournamentAsync(tournament);
        Data.TournamentId = tournament.Id;
        await Shell.Current.GoToAsync(nameof(AddTournament));
    }

    private async void BtnEditTournament_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllTournaments));
    }

    private async void BtnLoadRankings_OnClicked(object? sender, EventArgs e)
    {
        var list = await _repository.GetPlayersAsync();

        await _databaseService.UpsertPlayersAsync(list);
    }

    private async void BtnGames_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllGames));
    }

    private async void BtnAllRank_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllPlayerRanking));
    }
}
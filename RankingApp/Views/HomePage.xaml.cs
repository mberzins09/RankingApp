using RankingApp.Data_Storage;
using RankingApp.Models;
using RankingApp.Services;
using SQLite;

namespace RankingApp.Views;

public partial class HomePage : ContentPage
{
    private readonly DatabaseService _databaseService;


    public HomePage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }



    private async void BtnLoadRankings_OnClicked(object? sender, EventArgs e)
    {
        try
        {
            //await GetLastestRankings(); // Your optimized sync function
            await Application.Current.MainPage.DisplayAlert("Success", "Done", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong:\n{ex.Message}", "OK");
        }
    }


    private async void BtnGetAllRankings_Clicked(object sender, EventArgs e)
    {
        try
        {
            //await FillDatabaseWithOldRankings(); // Your optimized sync function
            await Application.Current.MainPage.DisplayAlert("Success", "Done", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong:\n{ex.Message}", "OK");
        }
    }

    // Moved to Player viewModel
    //private async Task GetLastestRankings() 
    //{
    //    var apiPlayers = await _repository.GetPlayersAsync();
    //    List<PlayerDB> dbPlayers = await _databaseService.GetPlayersAsync();

    //    var apiPlayerIds = new HashSet<int>(apiPlayers.Select(p => p.Id));

    //    var missingPlayers = dbPlayers.Where(dbPlayer => !apiPlayerIds.Contains(dbPlayer.Id)).ToList();

    //    foreach (var player in missingPlayers)
    //    {
    //        await _databaseService.UpdatePlayerAsync(player);
    //    }

    //    await _databaseService.UpsertPlayersAsync(apiPlayers);
    //}

    //private async Task FillDatabaseWithOldRankings()
    //{
    //    int startYear = 2014;
    //    int currentYear = DateTime.UtcNow.Year;
    //    int currentMonth = DateTime.UtcNow.Month;
    //    int endYear = (currentMonth == 1) ? currentYear - 1 : currentYear;
    //    var tasks = new List<Task<List<(PlayerDB Player, int SyncYear)>>>();

    //    for (int year = startYear; year <= endYear; year++)
    //    {
    //        string dateString = $"{year}-01";
    //        var task = _reposotoryWithDate.GetPlayersAsync(dateString)
    //    .ContinueWith(t =>
    //    {
    //        var players = t.Result;
    //        return players.Select(p => (Player: p, SyncYear: year)).ToList();
    //    });

    //        tasks.Add(task);
    //    }

    //    var results = await Task.WhenAll(tasks);

    //    var allPlayerTuples = results.SelectMany(x => x).ToList();

    //    var distinctPlayers = allPlayerTuples
    //                            .OrderBy(x => x.SyncYear)
    //                            .GroupBy(x => x.Player.Id)
    //                            .Select(g => g.Last().Player)
    //                            .ToList();

    //    await _databaseService.UpsertPlayersAsync(distinctPlayers);

    //    await GetLastestRankings();
    //}
}
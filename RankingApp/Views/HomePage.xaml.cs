using RankingApp.Data_Storage;
using RankingApp.Models;
using RankingApp.Services;
using SQLite;

namespace RankingApp.Views;

public partial class HomePage : ContentPage
{
    private readonly PlayerRepository _repository;
    private readonly DatabaseService _databaseService;
    private readonly PlayerReposotoryWithDate _reposotoryWithDate;


    public HomePage(PlayerRepository repository, DatabaseService databaseService, PlayerReposotoryWithDate reposotoryWithDate)
    {
        InitializeComponent();
        _repository = repository;
        _databaseService = databaseService;
        _reposotoryWithDate = reposotoryWithDate;
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
        var player = new PlayerDB()
        {
            Id = 10000,
            Place = 10000,
            Points = 0,
            PointsWithBonus = 0,
            Name = "Vārds",
            Surname = "Uzvārds",
            Gender = "male",
            OverallPlace = 10000,
            BirthDate = ""
        };
        var p = await _databaseService.GetPlayerAsync(694);
        if (p != null)
        {
            player.Id = 1977;
            player.Place = p.Place;
            player.Points = p.Points;
            player.PointsWithBonus = p.PointsWithBonus;
            player.Name = p.Name;
            player.Surname = p.Surname;
            player.Gender = p.Gender;
            player.OverallPlace = p.OverallPlace;
            player.BirthDate = p.BirthDate;
        }
        var tournament = new Tournament()
        {
            Coefficient = "0.5",
            Name = "Jauns turnīrs",
            Date = DateTime.Now,
            TournamentPlayerName = player.Name,
            TournamentPlayerSurname = player.Surname,
            TournamentPlayerPoints = player.Points,
            TournamentPlayerId = player.Id
        };
        await _databaseService.SaveTournamentAsync(tournament);
        Data.TournamentId = tournament.Id;
        await Shell.Current.GoToAsync(nameof(AddTournament));
    }

    private async void BtnEditTournament_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllTournaments));
    }

    private async void BtnLoadRankings_OnClicked(object? sender, EventArgs e)
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

    private async void BtnGames_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllGames));
    }

    private async void BtnAllRank_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AllPlayerRanking));
    }

    private async void BtnInactiveRank_OnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(InactivePlayers));
    }

    private async void BtnGetAllRankings_Clicked(object sender, EventArgs e)
    {
        try
        {
            await FillDatabaseWithOldRankings(); // Your optimized sync function
            await Application.Current.MainPage.DisplayAlert("Success", "Done", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Something went wrong:\n{ex.Message}", "OK");
        }
    }

    private async void BtnData_OnClicked(object sender, EventArgs e)
    {
        await MigrateOldDatabaseAsync();
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

    private async Task FillDatabaseWithOldRankings()
    {
        int startYear = 2014;
        int currentYear = DateTime.UtcNow.Year;
        int currentMonth = DateTime.UtcNow.Month;
        int endYear = (currentMonth == 1) ? currentYear - 1 : currentYear;
        var tasks = new List<Task<List<(PlayerDB Player, int SyncYear)>>>();

        for (int year = startYear; year <= endYear; year++)
        {
            string dateString = $"{year}-01";
            var task = _reposotoryWithDate.GetPlayersAsync(dateString)
        .ContinueWith(t =>
        {
            var players = t.Result;
            return players.Select(p => (Player: p, SyncYear: year)).ToList();
        });

            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        var allPlayerTuples = results.SelectMany(x => x).ToList();

        var distinctPlayers = allPlayerTuples
                                .OrderBy(x => x.SyncYear)
                                .GroupBy(x => x.Player.Id)
                                .Select(g => g.Last().Player)
                                .ToList();

        await _databaseService.UpsertPlayersAsync(distinctPlayers);

        await GetLastestRankings();
    }

    private async Task MigrateOldDatabaseAsync()
    {
        var appDataDir = FileSystem.AppDataDirectory;

        var mainDbPath = Path.Combine(appDataDir, "AllP.db3");
        var oldDbPath = Path.Combine(appDataDir, "Data3.db3");

        var mainDb = new SQLiteAsyncConnection(mainDbPath);
        await mainDb.CreateTableAsync<Game>();
        await mainDb.CreateTableAsync<Tournament>();

        if (File.Exists(oldDbPath))
        {
            var oldDb = new SQLiteAsyncConnection(oldDbPath);

            // Ensure old DB has tables
            await oldDb.CreateTableAsync<Game>();
            await oldDb.CreateTableAsync<Tournament>();

            var oldGames = await oldDb.Table<Game>().ToListAsync();
            var oldTournaments = await oldDb.Table<Tournament>().ToListAsync();

            foreach (var game in oldGames)
                await mainDb.InsertOrReplaceAsync(game);

            foreach (var tournament in oldTournaments)
                await mainDb.InsertOrReplaceAsync(tournament);

            // Optional: delete old DB
            File.Delete(oldDbPath);

            // Optional: show success message
            await Application.Current.MainPage.DisplayAlert("Migration Complete", "Game and Tournament data migrated. Old Databse deleted", "OK");
        }
        else
        {
            // Optional: show info message
            await Application.Current.MainPage.DisplayAlert("No Migration Needed", "old database not found.", "OK");
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Models;
using RankingApp.Services;
using RankingApp.Views;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels;

public partial class AllTournamentsViewModel(DatabaseService database) : BaseViewModel
{
    private readonly DatabaseService _database = database;
    
    private List<Tournament> _allTournaments = [];
    
    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private ObservableCollection<Tournament>? tournaments;

    [ObservableProperty]
    private int selectedYear = DateTime.Now.Year;

    partial void OnSelectedYearChanged(int value) => ApplyAllFilters();
    partial void OnSearchTextChanged(string? value) => ApplyAllFilters();

    public ObservableCollection<int> Years { get; } = new ObservableCollection<int>(Enumerable.Range(2015, DateTime.Now.Year - 2015 + 1));

    [RelayCommand]
    private async Task TournamentSelectedAsync(Tournament tournament)
    {
        if (tournament == null) return;
        Data.TournamentId = tournament.Id;
        await Shell.Current.GoToAsync(nameof(TournamentView));
    }

    [RelayCommand]
    private async Task DeleteTournamentAsync(Tournament tournament)
    {
        if (tournament == null)
            return;

        var Games = await GetGames(tournament.Id);
        foreach (var game in Games)
        {
            await _database.DeleteGameAsync(game);
        }

        var Doubles = await GetDoubles(tournament.Id);
        foreach (var doubleG in Doubles)
        {
            await _database.DeleteDoublesGameAsync(doubleG);
        }

        await DeleteTournament(tournament);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditTournamentAsync(Tournament tournament)
    {
        if (tournament == null)
            return;

        Data.TournamentId = tournament.Id;
        await Shell.Current.GoToAsync(nameof(EditTournamentPlayer));
    }

    public async Task Migrate()
    {
        await _database.RunAllMigrationsAsync();
    }

    public async Task AddPlayerDBTable()
    {
        await _database.MigratePlayerTableAsync();
    }

    private void FilterTournamentsByYear(int year)
    {
        var filtered = _allTournaments
                       .Where(t => t.Date.Year == year)
                       .OrderByDescending(t => t.Date)
                       .ToList();

        Tournaments = new ObservableCollection<Tournament>(filtered);
    }

    public async Task LoadDataAsync()
    {
        var tournaments = await _database.GetTournamentsAsync();
        _allTournaments = tournaments.OrderByDescending(x => x.Date).ToList();
        var allGames = await _database.GetGamesAsync();
        foreach (var tournament in _allTournaments) 
        {
            tournament.PointsDifference = allGames
                                          .Where(game => game.TournamentId == tournament.Id)
                                          .Sum(game => game.RatingDifference);
        }

        ApplyAllFilters();
    }

    private void ApplyAllFilters()
    {
        IEnumerable<Tournament> filtered = _allTournaments;

        // Apply year filter
        if (SelectedYear > 0)
        {
            filtered = filtered.Where(t => t.Date.Year == SelectedYear);
        }

        // Apply search text filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Name) &&
                 x.Name.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                x.Date.ToString("d MMM yyyy").StartsWith(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        Tournaments = new ObservableCollection<Tournament>(filtered.OrderByDescending(t => t.Date));
    }

    public async Task<List<Game>> GetGames(int id)
    {
        var Games = await _database.GetGamesAsync();
        var list = Games.Where(x => x.TournamentId == id).ToList();

        return list;
    }

    public async Task<List<DoublesGame>> GetDoubles(int id)
    {
        var Doubles = await _database.GetDoublesGamesAsync();
        var list = Doubles.Where(x => x.TournamentId == id).ToList();

        return list;
    }

    public void FilterTournaments(string searchText)
    {
        IEnumerable<Tournament> filtered = _allTournaments;

        if (SelectedYear > 0)
        {
            filtered = filtered.Where(t => t.Date.Year == SelectedYear);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(x =>
            (!string.IsNullOrWhiteSpace(x.Name) && x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
            x.Date.ToString("d MMM yyyy").StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
        }

        Tournaments = new ObservableCollection<Tournament>(filtered);
    }

    public async Task DeleteTournament(Tournament tournament)
    {
        await _database.DeleteTournamentAsync(tournament);
    }

    public async Task CreateNewTournamentSave()
    {
        var appData = await _database.GetAppDataAsync();
        PlayerDB? playerDB = null;

        if (appData.AppUserPlayerId != 0)
            playerDB = await _database.GetPlayerAsync(appData.AppUserPlayerId);

        playerDB ??= await _database.GetPlayerAsync(694);

        var player = new PlayerDB()
        {
            Id = 10000, Place = 10000, Points = 0, PointsWithBonus = 0, Name = "Name", Surname = "Surname", Gender = "male", OverallPlace = 10000, BirthDate = ""
        };

        if (playerDB != null)
        {
            player.Id = playerDB.Id;
            player.Place = playerDB.Place;
            player.Points = playerDB.Points;
            player.PointsWithBonus = playerDB.PointsWithBonus;
            player.Name = playerDB.Name;
            player.Surname = playerDB.Surname;
            player.Gender = playerDB.Gender;
            player.OverallPlace = playerDB.OverallPlace;
            player.BirthDate = playerDB.BirthDate;
        }
        var tournament = new Tournament()
        {
            Coefficient = "0.5",
            Name = "New",
            Date = DateTime.Now,
            TournamentPlayerName = player.Name == "Edgars(R)" ? "Edgars" : player.Name,
            TournamentPlayerSurname = player.Surname,
            TournamentPlayerPoints = player.Points,
            TournamentPlayerId = player.Id
        };
        
        await _database.SaveTournamentAsync(tournament);
        Data.TournamentId = tournament.Id;
    }
}
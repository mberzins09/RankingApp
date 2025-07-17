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
    private Tournament? selectedTournament;

    [ObservableProperty]
    private ObservableCollection<Tournament>? tournaments;

    partial void OnSearchTextChanged(string? value)
    {
        FilterTournaments(value ?? string.Empty);
    }

    partial void OnSelectedTournamentChanged(Tournament? value)
    {
        if (value != null)
        {
            Data.TournamentId = value.Id;
            Shell.Current.GoToAsync(nameof(Tournaments));
        }
    }

    [RelayCommand]
    private async Task DeleteTournamentAsync(Tournament tournament)
    {
        if (tournament == null)
            return;

        var games = await GetGames(tournament.Id);
        foreach (var game in games)
        {
            await _database.DeleteGameAsync(game);
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
        await Shell.Current.GoToAsync(nameof(AddTournament));
    }

    public async Task Migrate()
    {
        await _database.RunAllMigrationsAsync();
    }

    public async Task AddPlayerDBTable()
    {
        await _database.MigratePlayerTableAsync();
    }

    public async Task LoadDataAsync()
    {
        var tournaments = await _database.GetTournamentsAsync();
        _allTournaments = tournaments.OrderByDescending(x => x.Date).ToList();
        Tournaments = new ObservableCollection<Tournament>(_allTournaments);
    }

    public async Task<List<Game>> GetGames(int id)
    {
        var games = await _database.GetGamesAsync();
        var list = games.Where(x => x.TournamentId == id).ToList();

        return list;
    }

    public void FilterTournaments(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            Tournaments = new ObservableCollection<Tournament>(_allTournaments);
            return;
        }

        var filtered = _allTournaments.Where(x =>(!string.IsNullOrWhiteSpace(x.Name) &&
                                             x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                             x.Date.ToString("d MMM yyyy").StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                                             .ToList();

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

        if (playerDB == null)
            playerDB = await _database.GetPlayerAsync(694);

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
            Name = "New Tournament",
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
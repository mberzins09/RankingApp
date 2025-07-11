using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels;

public partial class AllTournamentsViewModel(DatabaseService database) : BaseViewModel
{
    private readonly DatabaseService _database = database;
    private List<Tournament> _allTournaments = new();
    
    [ObservableProperty]
    private ObservableCollection<Tournament> tournaments;

    public async Task LoadDataAsync()
    {
        await _database.RunAllMigrationsAsync();
        var tournaments = await _database.GetTournamentsAsync();
        _allTournaments = tournaments.OrderByDescending(x => x.Date).ToList();
        Tournaments = new ObservableCollection<Tournament>(_allTournaments);
    }

    public async Task<List<Tournament>> GetTournaments()
    {
        var tournaments = await _database.GetTournamentsAsync();
        var list = tournaments.OrderByDescending(x => x.Date).ToList();

        return list;
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

        var filtered = _allTournaments
            .Where(x =>
                (!string.IsNullOrWhiteSpace(x.Name) &&
                 x.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) ||
                x.Date.ToString("d MMM yyyy").StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Tournaments = new ObservableCollection<Tournament>(filtered);
    }

    public async Task DeleteTournament(Tournament tournament)
    {
        await _database.DeleteTournamentAsync(tournament);
    }

    public async Task DeleteGame(Game game)
    {
        await _database.DeleteGameAsync(game);
    }
}
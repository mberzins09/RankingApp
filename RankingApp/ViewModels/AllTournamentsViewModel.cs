using System.Collections.ObjectModel;
using RankingApp.Models;

namespace RankingApp.ViewModels;

public class AllTournamentsViewModel(RankingAppsDatabase database) : BaseViewModel
{
    private readonly RankingAppsDatabase _database = database;
    public ObservableCollection<Tournament>? Tournaments { get; set; }
    public List<Tournament> TournamentsList { get; set; } = new List<Tournament>();

    public async Task LoadDataAsync()
    {
        var tournaments = await _database.GetTournamentsAsync();
        TournamentsList = tournaments.OrderByDescending(x => x.Date).ToList();

        Tournaments = new ObservableCollection<Tournament>(TournamentsList);

        OnPropertyChanged();
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

    public List<Tournament> SearchTournaments(List<Tournament> tournaments, string filterText)
    {
        var searchedTournaments = tournaments
            .Where(x => (!string.IsNullOrWhiteSpace(x.Name) &&
                         x.Name.StartsWith(filterText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.Date.ToString("d MMM yyyy")) &&
                         x.Date.ToString("d MMM yyyy").StartsWith(filterText, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return searchedTournaments;
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
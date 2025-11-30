using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Models;
using RankingApp.Services;
using RankingApp.Views;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels;

public partial class AllTournamentsViewModel(DatabaseService database, PlayerService playerService) : BaseViewModel
{
    private readonly DatabaseService _database = database;
    private readonly PlayerService _playerService = playerService;
    
    private List<Tournament> _allTournaments = [];
    
    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private ObservableCollection<Tournament>? tournaments;

    [ObservableProperty]
    private int selectedYear = 0;

    partial void OnSelectedYearChanged(int value) => ApplyAllFilters();
    partial void OnSearchTextChanged(string? value) => ApplyAllFilters();
    public async Task Migrate() => await _database.RunAllMigrationsAsync();
    public async Task AddPlayerDBTable() => await _database.MigratePlayerTableAsync();

    public ObservableCollection<int> Years { get; } = [];

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


    public async Task LoadDataAsync()
    {
        await _playerService.EnsureAppUserOldAndNewIdsAsync();
        var tournaments = await _database.GetTournamentsAsync();
        _allTournaments = tournaments.OrderByDescending(x => x.Date).ToList();
        var allGames = await _database.GetGamesAsync();
        foreach (var tournament in _allTournaments) 
        {
            tournament.PointsDifference = allGames
                                          .Where(game => game.TournamentId == tournament.Id)
                                          .Sum(game => game.RatingDifference);
        }

        var uniqueYears = _allTournaments.Select(t => t.Date.Year).Distinct().OrderByDescending(y => y).ToList();
        Years.Clear();
        Years.Add(0);
        foreach (var year  in uniqueYears)
        {
            Years.Add(year);
        }

        SelectedYear = 0;
        OnPropertyChanged(nameof(SelectedYear));

        ApplyAllFilters();
    }

    private void ApplyAllFilters()
    {
        IEnumerable<Tournament> filtered = _allTournaments;

        if (SelectedYear > 0)
        {
            filtered = filtered.Where(t => t.Date.Year == SelectedYear);
        }

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

    public async Task DeleteTournament(Tournament tournament)
    {
        await _database.DeleteTournamentAsync(tournament);
    }

    public async Task CreateNewTournamentSave()
    {
        var appData = await _database.GetAppDataAsync();
        PlayerDB? playerDB = null;

        if (appData.AppUserPlayerId != 0)
        {
            playerDB = await _database.GetPlayerAsync(appData.AppUserPlayerId);

            if (playerDB == null)
            {
                if (appData.AppUserOldId != 0 && appData.AppUserOldId != appData.AppUserPlayerId)
                {
                    playerDB = await _database.GetPlayerAsync(appData.AppUserOldId);
                }

                if (playerDB == null && appData.AppUserNewId != 0 && appData.AppUserNewId != appData.AppUserPlayerId)
                {
                    playerDB = await _database.GetPlayerAsync(appData.AppUserNewId);
                }
            }
        }

        var player = playerDB ?? new PlayerDB()
        {
            Id = 10000,
            Place = 10000,
            Points = 0,
            PointsWithBonus = 0,
            Name = "Not Set",
            Surname = "Default Player",
            Gender = "male",
            OverallPlace = 10000,
            BirthDate = ""
        };

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

    public async Task CheckRankingUpdateAsync(DateTime systemDate)
    {
        var appData = await _database.GetAppDataAsync();

        if (appData == null)
            return;

        if (appData.CurrentYear < systemDate.Year ||
            (appData.CurrentYear == systemDate.Year && appData.CurrentMonth < systemDate.Month))
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Update Rankings",
                "You haven't updated to the newest ranking. Would you like to update it?",
                "Yes",
                "No");

            if (!confirm)
                return;

            var popup = new ProcessingPopup { Message = "Loading players..." };
            _ = Application.Current.MainPage.ShowPopupAsync(popup);

            try
            {
                await _playerService.LoadPlayersFromApiOrDbAsync(systemDate, status =>
                {
                    MainThread.BeginInvokeOnMainThread(() => popup.Message = status);
                });

                appData.CurrentYear = systemDate.Year;
                appData.CurrentMonth = systemDate.Month;
                await _database.SaveAppDataAsync(appData);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    popup.Message = "✅ Rankings updated successfully!";
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    popup.Message = $"❌ Error: {ex.Message}";
                });
            }
            finally
            {
                await Task.Delay(1000);
                await popup.CloseAsync();
            }
        }
    }
}
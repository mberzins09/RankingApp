using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels;

public partial class AllTournamentsViewModel(IDatabaseService database, IPlayerService playerService, TournamentImportService importService, INavigationService navigation, IProgressDialogService progressDialog) : BaseViewModel
{
    private readonly IProgressDialogService _progressDialog = progressDialog;
    private readonly INavigationService _navigation = navigation;
    private readonly IDatabaseService _database = database;
    private readonly IPlayerService _playerService = playerService;
    private readonly TournamentImportService _importService = importService;

    private List<Tournament> _allTournaments = [];

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private ObservableCollection<Tournament>? tournaments;

    [ObservableProperty]
    private int selectedYear = 0;

    partial void OnSelectedYearChanged(int value) => ApplyAllFilters();
    partial void OnSearchTextChanged(string? value) => ApplyAllFilters();
    public async Task Migrate() => await _database.MigrateDatabaseAsync();
    public async Task MigrateId() => await _database.MigrateExternalIdsAsync();

    public ObservableCollection<int> Years { get; } = [];

    [RelayCommand]
    private async Task TournamentSelectedAsync(Tournament tournament)
    {
        if (tournament == null) return;
        Data.TournamentId = tournament.Id;
        await _navigation.GoToAsync("TournamentView");
    }

    [RelayCommand]
    private async Task DeleteTournamentAsync(Tournament tournament)
    {
        if (tournament == null)
            return;

        await _database.DeleteGamesForTournamentAsync(tournament.Id);
        await _database.DeleteDoublesForTournamentAsync(tournament.Id);
        await DeleteTournament(tournament);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteTournamentsAsync()
    {
        await _database.DeleteAllAsync<Game>();
        await _database.DeleteAllAsync<Tournament>();
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task EditTournamentAsync(Tournament tournament)
    {
        if (tournament == null)
            return;

        Data.TournamentId = tournament.Id;
        await _navigation.GoToAsync("EditTournamentPlayer");
    }

    [RelayCommand]
    private async Task GetOldTournamentsAsync()
    {
        _progressDialog.Show("Loading old tournaments...");

        await Task.Yield();

        System.ComponentModel.PropertyChangedEventHandler? handler = null;
        handler = (sender, args) =>
        {
            if (args.PropertyName == nameof(TournamentImportService.Message) ||
                args.PropertyName == nameof(TournamentImportService.IsRunning))
            {
                _progressDialog.Update(_importService.Message);
            }
        };

        _importService.PropertyChanged += handler;

        try
        {
            await _importService.StartImport(progress => _playerService.GetOldTournamentsAsync(progress));
        }
        finally
        {
            _importService.PropertyChanged -= handler;
            await Task.Delay(300);
            await _progressDialog.HideAsync();
        }

        await LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        await MigrateId();
        await Migrate();
        var tournaments = await _database.GetAllRecordsAsync<Tournament>();
        _allTournaments = [.. tournaments.OrderByDescending(x => x.Date)];
        if (_allTournaments.Any(t => t.TournamentPlayerName == "Edgars(R)"))
        {
            foreach (var t in _allTournaments.Where(t => t.TournamentPlayerName == "Edgars(R)"))
            {
                t.TournamentPlayerName = "Edgars";
                await _database.SaveAsync<Tournament>(t);
            }
        }

        var pointSums = await _database.GetGamePointSumsAsync();
        foreach (var t in _allTournaments)
        {
            t.PointsDifference = pointSums.TryGetValue(t.Id, out var sum) ? sum : 0;
        }

        var uniqueYears = _allTournaments.Select(t => t.Date.Year).Distinct().OrderByDescending(y => y).ToList();
        Years.Clear();
        Years.Add(0);
        foreach (var year in uniqueYears)
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
        var Games = await _database.GetAllRecordsAsync<Game>();
        var list = Games.Where(x => x.TournamentId == id).ToList();

        return list;
    }

    public async Task<List<DoublesGame>> GetDoubles(int id)
    {
        var Doubles = await _database.GetAllRecordsAsync<DoublesGame>();
        var list = Doubles.Where(x => x.TournamentId == id).ToList();

        return list;
    }

    public async Task DeleteTournament(Tournament tournament)
    {
        await _database.DeleteAsync<Tournament>(tournament);
    }

    public async Task CreateNewTournamentSave()
    {
        var appData = await _database.GetAppDataAsync();
        PlayerDB? playerDB = null;

        if (appData.AppUserPlayerId != 0)
        {
            playerDB = await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId);

            if (playerDB == null)
            {
                if (appData.AppUserOldId != 0 && appData.AppUserOldId != appData.AppUserPlayerId)
                {
                    playerDB = await _database.GetByIdAsync<PlayerDB>(appData.AppUserOldId);
                }

                if (playerDB == null && appData.AppUserNewId != 0 && appData.AppUserNewId != appData.AppUserPlayerId)
                {
                    playerDB = await _database.GetByIdAsync<PlayerDB>(appData.AppUserNewId);
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
            Name = "Enter Tournament Name",
            Date = DateTime.Now,
            TournamentPlayerName = player.Name == "Edgars(R)" ? "Edgars" : player.Name,
            TournamentPlayerSurname = player.Surname,
            TournamentPlayerPoints = player.Points,
            TournamentPlayerId = player.Id
        };

        await _database.SaveAsync<Tournament>(tournament);
        Data.TournamentId = tournament.Id;
    }

    public async Task CheckRankingUpdateAsync(DateTime systemDate)
    {
        var appData = await _database.GetAppDataAsync();

        if (appData == null)
            return;

        if (!appData.RemindRankingUpdate)
            return;

        if (appData.CurrentYear < systemDate.Year || (appData.CurrentYear == systemDate.Year && appData.CurrentMonth < systemDate.Month))
        {
            bool confirm = await Shell.Current.DisplayAlert("Update Rankings","You haven't updated to the newest ranking. Would you like to update it?","Yes","No");

            if (!confirm)
                return;

            _progressDialog.Show("Loading players...");

            try
            {
                await _playerService.LoadPlayersFromApiOrDbAsync(systemDate, status => _progressDialog.Update(status));

                appData.CurrentYear = systemDate.Year;
                appData.CurrentMonth = systemDate.Month;
                await _database.SaveAppDataAsync(appData);

                _progressDialog.Update("✅ Rankings updated successfully!");
            }
            catch (Exception ex)
            {
                _progressDialog.Update($"❌ Error: {ex.Message}");
            }
            finally
            {
                await Task.Delay(400);
                await _progressDialog.HideAsync();
            }
        }
    }
}
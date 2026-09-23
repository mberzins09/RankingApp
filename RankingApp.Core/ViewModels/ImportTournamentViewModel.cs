using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Core.Interfaces;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    public partial class ImportTournamentViewModel(TournamentService tournamentService, IDatabaseService databaseService, IPlayerRepositoryWithDate playerReposotoryWithDate, ApiGameImporterService importer, INavigationService navigation) : BaseViewModel
    {
        private readonly INavigationService _navigation = navigation;
        private readonly TournamentService _tournamentService = tournamentService;
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly IPlayerRepositoryWithDate _playerReposotoryWithDate = playerReposotoryWithDate;
        private readonly ApiGameImporterService _importer = importer; 
        private readonly Dictionary<string, List<PlayerDB>> playersCacheByMonth = [];
        private List<PlayerDB> players = [];
        private List<PlayerDB> databasePlayers = [];
        private AppData? _appData;

        [ObservableProperty]
        private string statusText = "";

        public ObservableCollection<APITournament> Tournaments { get; set; } = [];

        public ObservableCollection<object> SelectedTournaments { get; set; } = [];

        public async Task LoadTournaments()
        {
            SelectedTournaments.Clear();
            StatusText = "";
            _appData = await _databaseService.GetAppDataAsync();
            databasePlayers = await _databaseService.GetAllRecordsAsync<PlayerDB>();

            var tournaments = await _tournamentService.GetPlayerTournamentsAsync(_appData.AppUserNewId);

            Tournaments.Clear();

            foreach (var t in tournaments)
                Tournaments.Add(t);
        }

        [RelayCommand]
        private async Task ImportSelectedTournamentsAsync()
        {
            var progress = new Progress<string>(msg =>
            {
                StatusText = msg;
            });

            await ImportSelectedTournamentsInternal(progress);
        }

        private async Task ImportSelectedTournamentsInternal(IProgress<string> progress)
        {
            var apiTournaments = SelectedTournaments.Cast<APITournament>().DistinctBy(t => t.Id).OrderBy(t => t.Date).ToList();

            if (apiTournaments.Count == 0)
                return;

            progress.Report("Starting import...");
            var existingTournaments = await _databaseService.GetAllRecordsAsync<Tournament>();
            await Task.Delay(300);

            foreach (var apiTournament in apiTournaments)
            {
                bool alreadyExists = existingTournaments.Any(t => t.ExternalTournamentId == apiTournament.Id);

                if (alreadyExists && !apiTournament.IsSeason)
                {
                    progress.Report($"Skipping {apiTournament.Competition} (already imported)");

                    await Task.Delay(400);
                    continue;
                }

                progress.Report($"Importing {apiTournament.Competition} - {apiTournament.EventName}");

                if (!DateTime.TryParse(apiTournament.Date, out var parsedDate))
                    continue;

                int year = parsedDate.Year;
                int month = parsedDate.Month;

                string key = $"{year}-{month}";

                if (_appData == null)
                    return;
                
                if (year == _appData.CurrentYear && month == _appData.CurrentMonth)
                {
                    players = databasePlayers;
                }
                else
                {
                    if (!playersCacheByMonth.ContainsKey(key))
                    {
                        bool isOldApi = year >= 2014 && year <= 2025 && month >= 1 && month <= 10;

                        string dateString = parsedDate.ToString("yyyy-MM");

                        playersCacheByMonth[key] = await _playerReposotoryWithDate.GetPlayersAsync(dateString, isOldApi);
                    }

                    players = playersCacheByMonth[key];
                }

                await InsertTournament(apiTournament, parsedDate);
            }

            progress.Report("Finished");
            await Task.Delay(500);

            await _navigation.GoToAsync("AllTournaments");
        }

        private async Task InsertTournament(APITournament apiTournament, DateTime parsedDate)
        {
            if (_appData == null)
                return;

            var me = players.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            me ??= databasePlayers.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            if (me == null)
                return;

            string tournamentName = "";

            if(apiTournament.EventName == null || apiTournament.EventName == "" || apiTournament.EventName == apiTournament.Competition)
            {
                if (apiTournament.EventName == null)
                {
                    tournamentName = "Bez EventName";
                }
                else
                {
                    tournamentName = apiTournament.EventName;
                }
            }
            else
            {
                tournamentName = $"{apiTournament.Competition} - {apiTournament.EventName}";
            }

            var tournament = new Tournament
            {
                ExternalTournamentId = apiTournament.Id,
                TournamentPlayerId = _appData.AppUserPlayerId,
                TournamentPlayerName = me.Name,
                TournamentPlayerSurname = me.Surname,
                Date = parsedDate,
                Name = tournamentName,
                Coefficient = apiTournament.Coefficient
            };

            await _databaseService.SaveAsync(tournament);
            Data.TournamentId = tournament.Id;

            var apiGames = await _tournamentService.GetTournamentGames(_appData.AppUserNewId,apiTournament.Id,parsedDate.ToString("yyyy-MM-dd"),apiTournament.IsSeason);

            if (apiGames == null || apiGames.Count == 0)
                return;

            await _importer.InsertGamesAsync(apiGames, tournament, players, databasePlayers, me);
        }
    }
}

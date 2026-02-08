using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Data_Storage;
using RankingApp.Models;
using RankingApp.Services;
using System.Collections.ObjectModel;

namespace RankingApp.ViewModels
{
    public partial class GameViewModel(DatabaseService databaseService, PlayerReposotoryWithDate playerReposotory) : BaseViewModel
    {
        private readonly DatabaseService _databaseService = databaseService;
        private readonly PlayerReposotoryWithDate _playerReposotory = playerReposotory;

        [ObservableProperty]
        private bool isSearchDisabled;

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private Game? oneGame;

        [ObservableProperty]
        private PlayerDB? selectedOpponent;

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private BoolOption? selectedOpponentForeignOption;

        private List<PlayerDB> _allPlayers = [];
        private List<PlayerDB> _tournamentRankingPlayers = [];
        public ObservableCollection<int> SetsOptions { get; } = [0, 1, 2, 3, 4];
        public List<BoolOption> IsOpponentForeignOptions { get; } = new()
        {
            new BoolOption { Value = true, Label = "Yes" },
            new BoolOption { Value = false, Label = "No" }
        };

        partial void OnSelectedOpponentForeignOptionChanged(BoolOption? value)
        {
            if (OneGame is null || value is null)
                return;

            OneGame.IsOpponentForeign = value.Value;
        }

        partial void OnSearchTextChanged(string? value)
        {
            FilterPlayers(value ?? string.Empty);
        }

        partial void OnSelectedOpponentChanged(PlayerDB? value)
        {
            if (value is null || OneGame is null)
                return;

            AssignOpponentProperties(value);
        }

        public async Task LoadDataAsync()
        {
            IsSearchDisabled = true;
            OneGame = await _databaseService.GetByIdAsync<Game>(Data.GameId);
            var tournament = await _databaseService.GetByIdAsync<Tournament>(OneGame.TournamentId);
            OneGame.GameCoefficient = tournament == null ? OneGame.GameCoefficient : tournament.Coefficient;
            var appData = await _databaseService.GetAppDataAsync();
            var dbPlayers = await _databaseService.GetAllRecordsAsync<PlayerDB>();

            _allPlayers = dbPlayers.Where(x => x.Id != tournament.TournamentPlayerId && x.Place != 0).OrderBy(x => x.PointsWithBonus).ToList();
            int tournamentYear = tournament.Date.Year;
            int tournamentMonth = tournament.Date.Month;
            bool sameRankingMonth = (tournamentYear == appData.CurrentYear && tournamentMonth == appData.CurrentMonth);
            if (!sameRankingMonth)
            {
                string dateString = tournament.Date.ToString("yyyy-MM-01");
                var apiPlayers = await _playerReposotory.GetPlayersAsync(dateString, true);

                _tournamentRankingPlayers = apiPlayers ?? [];

                var inactivePlayers = _allPlayers.Where(x => x.IsActive == false).ToList();
                var combined = _tournamentRankingPlayers.Concat(inactivePlayers)
                                                        .OrderBy(x => x.PointsWithBonus)
                                                        .ToList();

                Players = new ObservableCollection<PlayerDB>(combined);
            }
            else
            {
                Players = new ObservableCollection<PlayerDB>(_allPlayers);
            }

            SelectedOpponentForeignOption = IsOpponentForeignOptions.FirstOrDefault(x => x.Value == OneGame.IsOpponentForeign);
            await _databaseService.SaveAsync<Game>(OneGame);
            IsSearchDisabled = false;
        }

        public async Task SaveGameAsync()
        {
            await _databaseService.SaveAsync<Game>(OneGame);
        }

        public void FilterPlayers(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Players = new ObservableCollection<PlayerDB>(_allPlayers);
                return;
            }

            var normalizedInput = NameNormalizer.NormalizeKey(searchText);

            var filtered = _allPlayers.Where(x => x.KeyName.Contains(normalizedInput)).ToList();

            Players = new ObservableCollection<PlayerDB>(filtered);
        }

        private void AssignOpponentProperties(PlayerDB opponent)
        {
            if (OneGame is null)
                return;

            OneGame.Name = opponent.Name;
            OneGame.Surname = opponent.Surname;
            OneGame.OpponentPoints = opponent.Points;
            OneGame.OpponentPointsWithBonus = opponent.PointsWithBonus;
            OneGame.OpponentAge = opponent.Age;
            OneGame.OpponentPlace = opponent.Place;
        }
    }
}

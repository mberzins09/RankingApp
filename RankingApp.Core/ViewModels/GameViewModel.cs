using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    public partial class GameViewModel(IDatabaseService databaseService, IMonthPlayersService monthPlayers) : BaseViewModel, ISaveBeforeNavigate
    {
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly IMonthPlayersService _monthPlayers = monthPlayers;

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
        public ObservableCollection<int> SetsOptions { get; } = [0, 1, 2, 3, 4];
        public List<BoolOption> IsOpponentForeignOptions { get; } =
        [
            new BoolOption { Value = true, Label = "Yes" },
            new BoolOption { Value = false, Label = "No" }
        ];

        public async Task<bool> SaveBeforeNavigateAsync()
        {
            await SaveGameAsync();
            return true;
        }

        partial void OnSelectedOpponentForeignOptionChanged(BoolOption? value)
        {
            if (OneGame is null || value is null)
                return;

            OneGame.IsOpponentForeign = value.Value;
            _ = SaveGameAsync();
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
            _ = SaveGameAsync();
        }

        public async Task LoadDataAsync()
        {
            IsSearchDisabled = true;
            OneGame = await _databaseService.GetByIdAsync<Game>(Data.GameId);

            if (OneGame == null)
                return;

            var tournament = await _databaseService.GetByIdAsync<Tournament>(OneGame.TournamentId);

            if (tournament != null)
            {
                OneGame.GameCoefficient = tournament.Coefficient;
            }

            var appData = await _databaseService.GetAppDataAsync();
            var dBPlayers = await _databaseService.GetAllRecordsAsync<PlayerDB>();

            if (tournament == null)
                return;

            _allPlayers = await _monthPlayers.GetPlayersForTournamentAsync(tournament, appData, dBPlayers);
            Players = new ObservableCollection<PlayerDB>(_allPlayers);

            SelectedOpponentForeignOption = IsOpponentForeignOptions.FirstOrDefault(x => x.Value == OneGame.IsOpponentForeign);
            await _databaseService.SaveAsync<Game>(OneGame);
            IsSearchDisabled = false;
        }

        public async Task SaveGameAsync()
        {
            if (OneGame != null)
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

        public void AssignOpponentProperties(PlayerDB opponent)
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

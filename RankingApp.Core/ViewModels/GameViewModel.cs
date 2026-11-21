using System.ComponentModel;
using RankingApp.Core.Interfaces;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    public partial class GameViewModel(IDatabaseService databaseService, IMonthPlayersService monthPlayers, INavigationService navigation) : BaseViewModel, ISaveBeforeNavigate, IQueryAttributable
    {
        private readonly INavigationService _navigation = navigation;

        // Set from the navigation parameter "gameId"; Data.GameId is the fallback
        private int _pendingGameId;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (NavParams.TryGetInt(query, NavParams.GameId, out int id))
            {
                _pendingGameId = id;
                Data.GameId = id;
            }

            if (NavParams.TryGetInt(query, NavParams.TournamentId, out int tournamentId))
                Data.TournamentId = tournamentId;
        }

        /// <summary>Back to this game's tournament (back if we came from it, otherwise opens it).</summary>
        public async Task ReturnToTournamentAsync()
        {
            int tournamentId = OneGame?.TournamentId ?? Data.TournamentId;
            await _navigation.ReturnToTournamentAsync(tournamentId);
        }
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


        // ── Auto-save: every change to the game is saved right away ──────────
        // (sets, opponent, foreign...) so leaving with the back button loses nothing.
        private bool _isLoadingGame;
        private bool _saveQueued;

        partial void OnOneGameChanged(Game? oldValue, Game? newValue)
        {
            if (oldValue != null)
                oldValue.PropertyChanged -= GameProperty_Changed;

            if (newValue != null)
                newValue.PropertyChanged += GameProperty_Changed;
        }

        private void GameProperty_Changed(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isLoadingGame)
                QueueSave();
        }

        /// <summary>
        /// One change (e.g. picking an opponent) sets many properties at once - save once after
        /// all of them are set, not once per property.
        /// </summary>
        private async void QueueSave()
        {
            if (_saveQueued)
                return;

            _saveQueued = true;

            try
            {
                await Task.Yield();
                _saveQueued = false;
                await SaveGameAsync();
            }
            catch
            {
                _saveQueued = false;
            }
        }

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
            _isLoadingGame = true;
            try
            {
                await LoadGameAsync();
            }
            finally
            {
                _isLoadingGame = false;
            }
        }

        private async Task LoadGameAsync()
        {
            IsSearchDisabled = true;
            int gameId = _pendingGameId > 0 ? _pendingGameId : Data.GameId;
            _pendingGameId = 0;
            Data.GameId = gameId;

            OneGame = await _databaseService.GetByIdAsync<Game>(gameId);

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

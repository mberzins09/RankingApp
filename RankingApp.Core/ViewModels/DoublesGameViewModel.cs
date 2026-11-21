using System.ComponentModel;
using RankingApp.Core.Interfaces;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using RankingApp.Core.Models;
using RankingApp.Core.Services;
using RankingApp.Core.Services.Interfaces;
using System.Collections.ObjectModel;

namespace RankingApp.Core.ViewModels
{
    public partial class DoublesGameViewModel(IDatabaseService databaseService, IMonthPlayersService monthPlayers, INavigationService navigation) : BaseViewModel, ISaveBeforeNavigate, IQueryAttributable
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
            int tournamentId = OneDoublesGame?.TournamentId ?? Data.TournamentId;
            await _navigation.ReturnToTournamentAsync(tournamentId);
        }
        private readonly IDatabaseService _databaseService = databaseService;
        private readonly IMonthPlayersService _monthPlayers = monthPlayers;

        [ObservableProperty]
        private DoublesGame? oneDoublesGame;

        [ObservableProperty]
        private bool isSearchDisabled;

        [ObservableProperty]
        private ObservableCollection<PlayerDB>? players;

        [ObservableProperty]
        private PlayerDB? selectedPlayer;

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private string? selectionMode; // "MyPartner", "Opponent1", "Opponent2"

        private List<PlayerDB> _allPlayers = [];
        private List<PlayerDB> _tournamentRankingPlayers = [];

        public ObservableCollection<int> SetsOptions { get; } = [0, 1, 2, 3, 4];

        public List<string> SelectionOptions { get; } = ["MyPartner", "Opponent 1", "Opponent 2"];

        public async Task<bool> SaveBeforeNavigateAsync()
        {
            await SaveDoublesGameAsync();
            return true;
        }

        partial void OnSelectionModeChanged(string? value)
        {
            SelectedPlayer = null;
        }

        partial void OnSearchTextChanged(string? value)
        {
            FilterPlayers(value ?? string.Empty);
        }

        partial void OnSelectedPlayerChanged(PlayerDB? value)
        {
            SearchText = String.Empty;
            if (value is null || OneDoublesGame is null || string.IsNullOrEmpty(SelectionMode))
                return;

            switch (SelectionMode)
            {
                case "MyPartner":
                    OneDoublesGame.MyPartnerName = value.Name;
                    OneDoublesGame.MyPartnerSurname = value.Surname;
                    OneDoublesGame.MyPartnerPoints = value.Points;
                    OneDoublesGame.MyPartnerAge = value.Age;
                    OneDoublesGame.MyPartnerPointsWithBonus = value.PointsWithBonus;
                    OneDoublesGame.MyPartnerPlace = value.Place;
                    break;
                case "Opponent 1":
                    OneDoublesGame.Opponent1Name = value.Name;
                    OneDoublesGame.Opponent1Surname = value.Surname;
                    OneDoublesGame.Opponent1Points = value.Points;
                    OneDoublesGame.Opponent1Age = value.Age;
                    OneDoublesGame.Opponent1Place = value.Place;
                    OneDoublesGame.Opponent1PointsWithBonus = value.PointsWithBonus;
                    break;
                case "Opponent 2":
                    OneDoublesGame.Opponent2Name = value.Name;
                    OneDoublesGame.Opponent2Surname = value.Surname;
                    OneDoublesGame.Opponent2Points = value.Points;
                    OneDoublesGame.Opponent2Age = value.Age;
                    OneDoublesGame.Opponent2PointsWithBonus = value.PointsWithBonus;
                    OneDoublesGame.Opponent2Place = value.Place;
                    break;
            }
        }


        // ── Auto-save: every change to the game is saved right away ──────────
        // (sets, opponent, foreign...) so leaving with the back button loses nothing.
        private bool _isLoadingGame;
        private bool _saveQueued;

        partial void OnOneDoublesGameChanged(DoublesGame? oldValue, DoublesGame? newValue)
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
                await SaveDoublesGameAsync();
            }
            catch
            {
                _saveQueued = false;
            }
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

            OneDoublesGame = await _databaseService.GetByIdAsync<DoublesGame>(gameId);

            if (OneDoublesGame == null)
                return;

            var tournament = await _databaseService.GetByIdAsync<Tournament>(OneDoublesGame.TournamentId);

            if (tournament != null)
            {
                OneDoublesGame.GameCoefficient = tournament.Coefficient;
            }

            var appData = await _databaseService.GetAppDataAsync();
            var dbPlayers = await _databaseService.GetAllRecordsAsync<PlayerDB>();

            if (tournament == null)
                return;

            _allPlayers = await _monthPlayers.GetPlayersForTournamentAsync(tournament, appData, dbPlayers);
            Players = new ObservableCollection<PlayerDB>(_allPlayers);

            await _databaseService.SaveAsync<DoublesGame>(OneDoublesGame);
            IsSearchDisabled = false;
        }

        public async Task SaveDoublesGameAsync()
        {
            if (OneDoublesGame != null)
                await _databaseService.SaveAsync<DoublesGame>(OneDoublesGame);
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
    }
}

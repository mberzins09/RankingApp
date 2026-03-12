using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RankingApp.Data_Storage;
using RankingApp.Models;
using RankingApp.Services;
using RankingApp.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Game = RankingApp.Models.Game;

namespace RankingApp.ViewModels
{
    public partial class TournamentViewModel(DatabaseService database, PlayerReposotoryWithDate playerRepository, ApiGameImporterService importer, TournamentService tournamentService) : BaseViewModel, ISaveBeforeNavigate
    {
        private readonly DatabaseService _database = database;
        private readonly PlayerReposotoryWithDate _playerRepository = playerRepository;
        private readonly ApiGameImporterService _importer = importer;
        private readonly TournamentService _tournamentService = tournamentService;

        private List<PlayerDB> _playersCache = [];
        private List<PlayerDB> _databasePlayers = [];
        private List<Game>? _games;
        private AppData _appData;

        private List<DoublesGame>? _doubleGames;

        [ObservableProperty]
        private GameStatistics? stats;

        [ObservableProperty]
        private Tournament? oneTournament;

        [ObservableProperty]
        private IGame? selectedItem;

        public List<string> CoefficientOptions { get; } = ["0", "0.25", "0.5", "1", "1.5", "2", "4"];

        public List<string> GameModes { get; } = ["Singles", "Doubles"];

        [ObservableProperty]
        private string selectedGameMode = "Singles";

        [ObservableProperty]
        private ObservableCollection<IGame>? displayGames;

        public async Task<bool> SaveBeforeNavigateAsync()
        {
            await SaveTournamentAsync();
            return true;
        }

        partial void OnSelectedGameModeChanged(string value)
        {
            RefreshDisplayGames();
        }

        partial void OnOneTournamentChanged(Tournament? value)
        {
            if (value != null)
            {
                value.PropertyChanged -= CurrentTournament_PropertyChanged;
                value.PropertyChanged += CurrentTournament_PropertyChanged;
            }
        }

        private void CurrentTournament_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not Tournament t)
                return;

            switch (e.PropertyName)
            {
                case nameof(Tournament.Date):
                    _ = EditDate(t.Date);
                    break;
                case nameof(Tournament.Coefficient):
                    _ = EditCoefficient(t.Coefficient);
                    break;
                case nameof(Tournament.Name):
                    _ = EditTournamentName(t.Name);
                    break;
            }
        }

        partial void OnSelectedItemChanged(IGame? value)
        {
            if (value is null)
                return;

            switch (value)
            {
                case Game game:
                    Data.GameId = game.Id;
                    Shell.Current.GoToAsync(nameof(GameView));
                    break;

                case DoublesGame doublesGame:
                    Data.GameId = doublesGame.Id;
                    Shell.Current.GoToAsync(nameof(DoublesGameView));
                    break;
            }

            SelectedItem = null;
        }

        public async Task SaveTournamentAsync()
        {
            if (OneTournament != null)
            {
                await _database.SaveAsync<Tournament>(OneTournament);
            }
        }

        public async Task LoadDataAsync()
        {
            _appData = await _database.GetAppDataAsync();

            if (_databasePlayers.Count == 0)
            {
                _databasePlayers = await _database.GetAllRecordsAsync<PlayerDB>();
            }
            OneTournament = await _database.GetByIdAsync<Tournament>(Data.TournamentId);
            await LoadGamesAsync();

            if (OneTournament == null)
                return;

            await FillPlayersAsync();
        }

        public async Task FillPlayersAsync()
        {
            if (OneTournament == null)
                return;

            int tournamentYear = OneTournament.Date.Year;
            int tournamentMonth = OneTournament.Date.Month;

            bool isOldAPIBody = tournamentYear is >= 2014 and <= 2025 && tournamentMonth is >= 1 and <= 10;

            if (_appData.CurrentYear != tournamentYear || _appData.CurrentMonth != tournamentMonth)
            {
                var dateString = OneTournament.Date.ToString("yyyy-MM");
                _playersCache = await _playerRepository.GetPlayersAsync(dateString, isOldAPIBody);
            }
            else
            {
                _playersCache = _databasePlayers;
            }
        }

        public async Task LoadGamesAsync()
        {
            OneTournament = await _database.GetByIdAsync<Tournament>(Data.TournamentId);
            var allGames = await _database.GetAllRecordsAsync<Game>();
            _games = [.. allGames.Where(x => x.TournamentId == Data.TournamentId)];

            var allDoublesGames = await _database.GetAllRecordsAsync<DoublesGame>();
            _doubleGames = [.. allDoublesGames.Where(x => x.TournamentId == Data.TournamentId)];

            OneTournament.PointsDifference = allGames.Where(x => x.TournamentId == Data.TournamentId).Sum(x => x.RatingDifference);

            RefreshDisplayGames();
        }

        [RelayCommand]
        private async Task DeleteItemAsync(IGame item)
        {
            switch (item)
            {
                case Game game:
                    await _database.DeleteAsync<Game>(game);
                    break;

                case DoublesGame doublesGame:
                    await _database.DeleteAsync<DoublesGame>(doublesGame);
                    break;

                default:
                    return;
            }

            await LoadGamesAsync();
        }

        private void RefreshDisplayGames()
        {
            IEnumerable<IGame> games = SelectedGameMode == "Doubles" ? _doubleGames ?? Enumerable.Empty<IGame>() : _games ?? Enumerable.Empty<IGame>();

            DisplayGames = new ObservableCollection<IGame>(games);

            Stats = GameStatisticsCalculator.Calculate(DisplayGames);
        }

        public async Task CreateNewGameAsync(bool isDoubles)
        {
            var player = new PlayerDB()
            {
                Id = 10000,
                Place = 10000,
                Points = 0,
                PointsWithBonus = 0,
                Name = "Id changed",
                Surname = "Error",
                Gender = "male",
                OverallPlace = 10000,
                BirthDate = ""
            };

            if (OneTournament != null)
            {
                if (_playersCache == null || _playersCache.Count == 0)
                {
                    _playersCache = await _database.GetAllRecordsAsync<PlayerDB>();
                }

                DateTime date = OneTournament.Date;

                PlayerDB? foundPlayer = null;
                foundPlayer ??= _playersCache.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

                if (foundPlayer != null)
                {
                    player = foundPlayer;
                }
            }

            string name = player.Name == "Edgars(R)" ? "Edgars" : player.Name;
            string coef = OneTournament?.Coefficient ?? "0.5";
            DateTime tDate = OneTournament?.Date ?? DateTime.Today;
            int tournamentId = OneTournament?.Id ?? Data.TournamentId;
            string tournamentName = OneTournament?.Name;

            if (isDoubles)
            {
                var doublesGame = new DoublesGame()
                {
                    MyName = name,
                    MySurname = player.Surname,
                    MyPoints = player.Points,
                    MyPointsWithBonus = player.PointsWithBonus,
                    MyAge = player.Age,
                    MyPlace = player.Place,
                    GameCoefficient = coef,
                    TournamentDate = tDate,
                    TournamentId = tournamentId,
                    TournamentName = tournamentName
                };

                await _database.SaveAsync<DoublesGame>(doublesGame);
                Data.GameId = doublesGame.Id;
            }
            else
            {
                var game = new Game()
                {
                    MyName = name,
                    MySurname = player.Surname,
                    MyPoints = player.Points,
                    MyPointsWithBonus = player.PointsWithBonus,
                    MyAge = player.Age,
                    MyPlace = player.Place,
                    GameCoefficient = coef,
                    TournamentDate = tDate,
                    IsOpponentForeign = false,
                    OpponentPoints = 0,
                    TournamentId = tournamentId,
                    TournamentName = tournamentName
                };

                await _database.SaveAsync<Game>(game);
                Data.GameId = game.Id;
            }
        }

        public async Task EditDate(DateTime date)
        {
            if (OneTournament is null)
                return;

            var persisted = await _database.GetByIdAsync<Tournament>(OneTournament.Id);
            if (persisted != null && persisted.Date.Date == date.Date)
            {
                return;
            }

            var games = await _database.GetAllRecordsAsync<Game>();
            var doublesGames = await _database.GetAllRecordsAsync<DoublesGame>();

            var dateGames = games.Where(x => x.TournamentId == OneTournament.Id).ToList();
            var dateDoublesGames = doublesGames.Where(x => x.TournamentId == OneTournament.Id).ToList();
            
            foreach (var game in dateGames)
            {
                game.TournamentDate = date;

                await _database.SaveAsync<Game>(game);
            }

            foreach (var doubleGame in dateDoublesGames)
            {
                doubleGame.TournamentDate = date;

                await _database.SaveAsync<DoublesGame>(doubleGame);
            }

            await _database.SaveAsync<Tournament>(OneTournament);
            await LoadGamesAsync();
            await FillPlayersAsync();
        }

        public async Task EditCoefficient(string coef)
        {
            if (OneTournament is null)
                return;

            var games = await _database.GetAllRecordsAsync<Game>();
            var doublesGames = await _database.GetAllRecordsAsync<DoublesGame>();

            var coefGames = games.Where(x => x.TournamentId == OneTournament.Id).ToList();
            var coefDoublesGames = doublesGames.Where(x => x.TournamentId == OneTournament.Id).ToList();

            foreach (var game in coefGames)
            {
                game.GameCoefficient = coef;

                await _database.SaveAsync<Game>(game);
            }

            foreach (var doublegame in coefDoublesGames)
            {
                doublegame.GameCoefficient = coef;

                await _database.SaveAsync<DoublesGame>(doublegame);
            }

            await _database.SaveAsync<Tournament>(OneTournament);
            await LoadGamesAsync();
        }

        public async Task EditTournamentName(string name)
        {
            if (OneTournament is null)
                return;

            var games = await _database.GetAllRecordsAsync<Game>();
            var doublesGames = await _database.GetAllRecordsAsync<DoublesGame>();

            var nameGames = games.Where(x => x.TournamentId == OneTournament.Id).ToList();
            var nameDoublesGames = doublesGames.Where(x => x.TournamentId == OneTournament.Id).ToList();

            foreach (var game in nameGames)
            {
                game.TournamentName = name;

                await _database.SaveAsync<Game>(game);
            }

            foreach (var doubleGame in nameDoublesGames)
            {
                doubleGame.TournamentName = name;

                await _database.SaveAsync<DoublesGame>(doubleGame);
            }

            await _database.SaveAsync<Tournament>(OneTournament);
            await LoadGamesAsync();
        }

        public async Task ImportGamesForTournamentAsync()
        {
            if (OneTournament == null)
                return;

            if (OneTournament.ExternalTournamentId == 0)
                return;

            var apiGames = await _tournamentService.GetTournamentGames(_appData.AppUserNewId, OneTournament.ExternalTournamentId, OneTournament.Date.ToString("yyyy-MM-dd"), true);

            if (apiGames.Count == 0)
            {
                apiGames = await _tournamentService.GetTournamentGames(_appData.AppUserNewId, OneTournament.ExternalTournamentId, OneTournament.Date.ToString("yyyy-MM-dd"), false);
            }

            var me =_playersCache.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            me ??= _databasePlayers.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            if (me == null)
                return;

            await _importer.InsertGamesAsync(apiGames, OneTournament, _playersCache, _databasePlayers, me);

            await LoadGamesAsync();
        }

        public async Task FixGamesAsync()
        {
            if (OneTournament == null)
                return;

            if (_games == null || _games.Count == 0)
                return;

            var me = _playersCache.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            me ??= _databasePlayers.FirstOrDefault(p => p.KeyName == _appData.AppUserKeyName);

            if (me == null)
                return;

            foreach (var g in _games)
            {
                string oppKey = NameNormalizer.NormalizeKey($"{g.Name}{g.Surname}");

                var opp = _playersCache.FirstOrDefault(p => p.KeyName == oppKey);

                opp ??= _databasePlayers.FirstOrDefault(p => p.KeyName == oppKey);

                if (opp == null)
                    continue;

                g.OpponentPoints = opp.Points;
                g.OpponentPointsWithBonus = opp.PointsWithBonus;
                g.OpponentPlace = opp.Place;

                g.MyPoints = me.Points;
                g.MyPointsWithBonus = me.PointsWithBonus;
                g.MyPlace = me.Place;

                g.MyAge = AgeCalculator.CalculateAge(me.BirthDate, OneTournament.Date);
                g.OpponentAge = AgeCalculator.CalculateAge(opp.BirthDate, OneTournament.Date);

                await _database.SaveAsync<Game>(g);
            }

            await LoadGamesAsync();
        }
    }
}

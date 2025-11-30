using CommunityToolkit.Maui.Extensions;
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
    public partial class TournamentViewModel(DatabaseService database, PlayerReposotoryWithDate playerRepository) : BaseViewModel
    {
        private readonly DatabaseService _database = database;
        private readonly PlayerReposotoryWithDate _playerRepository = playerRepository;

        private List<PlayerDB> _playersCache = new();
        private List<Game>? _games;

        private List<DoublesGame>? _doubleGames;

        [ObservableProperty]
        private int totalGames;

        [ObservableProperty]
        private int totalWins;

        [ObservableProperty]
        private int totalLosses;

        [ObservableProperty]
        private int totalSets;

        [ObservableProperty]
        private int totalSetsWon;

        [ObservableProperty]
        private int totalSetsLost;

        [ObservableProperty]
        private double fifthSetWinPercentage;

        [ObservableProperty]
        private double totalSetsPercentage;

        [ObservableProperty]
        private double totalGamesPercentage;

        [ObservableProperty]
        private int fifthSetTotal;

        [ObservableProperty]
        private int fifthSetsWon;

        [ObservableProperty]
        private int fifthSetsLost;

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
                await _database.SaveTournamentAsync(OneTournament);
            }
        }

        public async Task LoadDataAsync()
        {
            OneTournament = await _database.GetTournamentAsync(Data.TournamentId);
            await LoadGamesAsync();

            if (OneTournament == null)
                return;

            await FillPlayersAsync();
        }

        public async Task FillPlayersAsync()
        {
            if (OneTournament == null)
                return;

            var appData = await _database.GetAppDataAsync();
            int tournamentYear = OneTournament.Date.Year;
            int tournamentMonth = OneTournament.Date.Month;

            bool isOldAPIBody = tournamentYear is >= 2014 and <= 2025 && tournamentMonth is >= 1 and <= 10;

            if (appData.CurrentYear != tournamentYear || appData.CurrentMonth != tournamentMonth)
            {
                var dateString = OneTournament.Date.ToString("yyyy-MM");
                _playersCache = await _playerRepository.GetPlayersAsync(dateString, isOldAPIBody);
            }
            else
            {
                _playersCache = await _database.GetPlayersAsync();
            }
        }

        public async Task LoadGamesAsync()
        {
            OneTournament = await _database.GetTournamentAsync(Data.TournamentId);
            var allGames = await _database.GetGamesAsync();
            _games = [.. allGames.Where(x => x.TournamentId == Data.TournamentId)];

            var allDoublesGames = await _database.GetDoublesGamesAsync();
            _doubleGames = [.. allDoublesGames.Where(x => x.TournamentId == Data.TournamentId)];

            OneTournament.PointsDifference = allGames
                                          .Where(x => x.TournamentId == Data.TournamentId)
                                          .Sum(x => x.RatingDifference);

            RefreshDisplayGames();
        }

        [RelayCommand]
        private async Task DeleteItemAsync(IGame item)
        {
            switch (item)
            {
                case Game game:
                    await _database.DeleteGameAsync(game);
                    break;

                case DoublesGame doublesGame:
                    await _database.DeleteDoublesGameAsync(doublesGame);
                    break;

                default:
                    return;
            }

            await LoadGamesAsync();
        }

        private void RefreshDisplayGames()
        {
            IEnumerable<IGame> games = SelectedGameMode == "Doubles"
        ? _doubleGames ?? Enumerable.Empty<IGame>()
        : _games ?? Enumerable.Empty<IGame>();

            DisplayGames = new ObservableCollection<IGame>(games);

            TotalGames = games.Count();
            TotalWins = games.Count(g => g.IsWin);
            TotalLosses = TotalGames - TotalWins;
            TotalGamesPercentage = TotalGames > 0 ? Math.Round((double)TotalWins / TotalGames * 100, 2) : 0;

            TotalSetsWon = games.Sum(g => g.MySets ?? 0);
            TotalSetsLost = games.Sum(g => g.OpponentSets ?? 0);
            TotalSets = TotalSetsWon + TotalSetsLost;
            TotalSetsPercentage = TotalSets > 0 ? Math.Round((double)TotalSetsWon / TotalSets * 100, 2) : 0;

            var fifthSetGames = games.Where(g => (g.MySets ?? 0) + (g.OpponentSets ?? 0) == 5);
            FifthSetTotal = fifthSetGames.Count();
            FifthSetsWon = fifthSetGames.Count(g => g.IsWin);
            FifthSetsLost = FifthSetTotal - FifthSetsWon;

            FifthSetWinPercentage = FifthSetTotal > 0 ? Math.Round((double)FifthSetsWon / FifthSetTotal * 100, 2) : 0;
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
                    _playersCache = await _database.GetPlayersAsync();
                }

                var appData = await _database.GetAppDataAsync();
                int oldId = appData.AppUserOldId;
                int newId = appData.AppUserNewId;
                int tournamentPlayerId = OneTournament.TournamentPlayerId;

                DateTime date = OneTournament.Date;

                PlayerDB? foundPlayer = null;

                if (tournamentPlayerId == oldId || tournamentPlayerId == newId)
                {
                    foundPlayer = _playersCache.FirstOrDefault(p => p.Id == newId)
                               ?? _playersCache.FirstOrDefault(p => p.Id == oldId);
                }

                foundPlayer ??= _playersCache.FirstOrDefault(p => p.Id == tournamentPlayerId);

                if (foundPlayer != null)
                {
                    player = foundPlayer;
                }
            }

            string name = player.Name == "Edgars(R)" ? "Edgars" : player.Name;
            string coef = OneTournament?.Coefficient ?? "0.5";
            DateTime tDate = OneTournament?.Date ?? DateTime.Today;
            int tournamentId = OneTournament?.Id ?? Data.TournamentId;
            string tournamentName = OneTournament?.Name ?? "New";

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

                await _database.SaveDoublesGameAsync(doublesGame);
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

                await _database.SaveGameAsync(game);
                Data.GameId = game.Id;
            }
        }

        public async Task EditDate(DateTime date)
        {
            if (OneTournament is null)
                return;

            var games = await _database.GetGamesAsync();
            var doublesGames = await _database.GetDoublesGamesAsync();

            var dateGames = games.Where(x => x.TournamentId == OneTournament.Id).ToList();
            var dateDoublesGames = doublesGames.Where(x => x.TournamentId == OneTournament.Id).ToList();
            
            foreach (var game in dateGames)
            {
                game.TournamentDate = date;

                await _database.SaveGameAsync(game);
            }

            foreach (var doubleGame in dateDoublesGames)
            {
                doubleGame.TournamentDate = date;

                await _database.SaveDoublesGameAsync(doubleGame);
            }

            await _database.SaveTournamentAsync(OneTournament);
            await LoadGamesAsync();
            await FillPlayersAsync();
        }

        public async Task EditCoefficient(string coef)
        {
            if (OneTournament is null)
                return;

            var games = await _database.GetGamesAsync();
            var doublesGames = await _database.GetDoublesGamesAsync();

            var coefGames = games.Where(x => x.TournamentId == OneTournament.Id).ToList();
            var coefDoublesGames = doublesGames.Where(x => x.TournamentId == OneTournament.Id).ToList();

            foreach (var game in coefGames)
            {
                game.GameCoefficient = coef;

                await _database.SaveGameAsync(game);
            }

            foreach (var doublegame in coefDoublesGames)
            {
                doublegame.GameCoefficient = coef;

                await _database.SaveDoublesGameAsync(doublegame);
            }

            await _database.SaveTournamentAsync(OneTournament);
            await LoadGamesAsync();
        }

        public async Task EditTournamentName(string name)
        {
            if (OneTournament is null)
                return;

            var games = await _database.GetGamesAsync();
            var doublesGames = await _database.GetDoublesGamesAsync();

            var nameGames = games.Where(x => x.TournamentId == OneTournament.Id).ToList();
            var nameDoublesGames = doublesGames.Where(x => x.TournamentId == OneTournament.Id).ToList();

            foreach (var game in nameGames)
            {
                game.TournamentName = name;

                await _database.SaveGameAsync(game);
            }

            foreach (var doubleGame in nameDoublesGames)
            {
                doubleGame.TournamentName = name;

                await _database.SaveDoublesGameAsync(doubleGame);
            }

            await _database.SaveTournamentAsync(OneTournament);
            await LoadGamesAsync();
        }
    }
}

using RankingApp.Data_Storage;
using RankingApp.Models;
using SQLite;
using System.Globalization;

namespace RankingApp.Services
{
    public class PlayerService(DatabaseService database, PlayerReposotoryWithDate repositoryWithDate)
    {
        private readonly DatabaseService _database = database;
        private readonly PlayerReposotoryWithDate _repositoryWithDate = repositoryWithDate;

        public async Task<List<PlayerDB>> LoadPlayersFromApiOrDbAsync(DateTime? date = null, Action<string>? progressCallback = null)
        {
            progressCallback?.Invoke("Checking current date...");
            DateTime now = date ?? DateTime.UtcNow;
            string currentDateString = now.ToString("yyyy-MM");
            string previousDateString = now.AddMonths(-1).ToString("yyyy-MM");

            progressCallback?.Invoke($"Fetching players for {currentDateString}...");
            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(currentDateString, false);
            if (apiPlayers == null || apiPlayers.Count == 0)
            {
                progressCallback?.Invoke($"No data for {currentDateString}, trying {previousDateString}...");
                apiPlayers = await _repositoryWithDate.GetPlayersAsync(previousDateString, false);
                if (apiPlayers != null && apiPlayers.Count > 0)
                {
                    progressCallback?.Invoke($"Updating AppData for {previousDateString}...");
                    await UpdateAppDataWithDate(previousDateString);
                }
            }
            else
            {
                progressCallback?.Invoke($"Updating AppData for {currentDateString}...");
                await UpdateAppDataWithDate(currentDateString);
            }

            if (apiPlayers != null && apiPlayers.Count > 0)
            {
                progressCallback?.Invoke("Syncing local database...");
                await SyncWithLocalDb(apiPlayers);
            }

            progressCallback?.Invoke("Sorting players...");
            return (await _database.GetAllRecordsAsync<PlayerDB>()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        public async Task GetOldTournamentsAsync(IProgress<string> progress)
        {
            var appData = await _database.GetAppDataAsync();

            if (appData.AppUserPlayerId == 0 ||
                string.IsNullOrWhiteSpace(appData.AppUserKeyName))
            {
                progress.Report("❌ You must select App Default Player first.");
                await Task.Delay(2000);
                return;
            }

            var lgtfPath = Path.Combine(FileSystem.AppDataDirectory, "lgtf.sqlite");
            var lgtfDb = new SQLiteAsyncConnection(lgtfPath);

            progress.Report("Loading local data...");

            var tournaments = await _database.GetAllRecordsAsync<Tournament>();
            var existingTournamentDates = new HashSet<DateOnly>(tournaments.Select(t => DateOnly.FromDateTime(t.Date)));

            var databasePlayers = await _database.GetAllRecordsAsync<PlayerDB>();
            List<PlayerDB> currentMonthPlayers = [];

            var playerMe = await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId);

            var lgtfGames = await lgtfDb.QueryAsync<LgtfGame>("SELECT * FROM games WHERE player1_id = ? OR player2_id = ?",playerMe.Id,playerMe.Id);
            var competitionIds = lgtfGames.Select(g => g.competition_id).Distinct().ToList();
            var lgtfCompetitions = await lgtfDb.QueryAsync<LgtfCompetition>($"SELECT * FROM competitions WHERE id IN ({string.Join(",", competitionIds)})");
            var uniqueGames = lgtfGames.GroupBy(g => new{g.competition_id,g.player1_id,g.player2_id,g.player1_sets,g.player2_sets}).Select(g => g.First()).ToList();
            var competitionsSorted = lgtfCompetitions.OrderBy(c => DateTime.Parse(c.start_date)).ToList();

            var competitionsToInsert = competitionsSorted.Where(c => !existingTournamentDates.Contains(DateOnly.FromDateTime(DateTime.Parse(c.start_date)))).ToList();

            if (competitionsToInsert.Count == 0)
            {
                progress.Report("ℹ️ No new tournaments to import.");
                await Task.Delay(2000);
                return;
            }

            var loadedDates = new HashSet<string>();

            var firstCompetitionDate = competitionsToInsert.Select(c => DateTime.Parse(c.start_date)).Min();

            progress.Report($"Collecting player data before first played tournament...");

            for (int year = 2014; year <= firstCompetitionDate.Year; year++)
            {
                await LoadAndMergeApiPlayersAsync(year,1,currentMonthPlayers);
            }

            foreach (var competition in competitionsToInsert)
            {
                var tournamentDate = DateTime.Parse(competition.start_date);
                var yearMonthKey = $"{tournamentDate:yyyy-MM}";

                progress.Report($"Importing {competition.name}...");
                if (!loadedDates.Contains(yearMonthKey))
                {
                    await LoadAndMergeApiPlayersAsync(tournamentDate.Year, tournamentDate.Month, currentMonthPlayers);

                    loadedDates.Add(yearMonthKey);
                }

                bool isFirstCompetition = (tournamentDate.Year == firstCompetitionDate.Year) && (tournamentDate.Month == firstCompetitionDate.Month);

                var tournament = new Tournament
                {
                    Name = $"{competition.name} ({competition.places})",
                    Coefficient = competition.coef.ToString(CultureInfo.InvariantCulture),
                    Date = tournamentDate,
                    TournamentPlayerId = playerMe.Id,
                    TournamentPlayerName = playerMe.Name,
                    TournamentPlayerSurname = playerMe.Surname
                };

                await _database.SaveAsync(tournament);

                var competitionGames = uniqueGames.Where(g => g.competition_id == competition.id).ToList();

                foreach (var lgtfGame in competitionGames)
                {
                    bool isMePlayer1 = lgtfGame.player1_id == playerMe.Id;

                    var opponentId = isMePlayer1 ? lgtfGame.player2_id : lgtfGame.player1_id;

                    bool isForeign;
                    var me = ResolvePlayer(playerMe.Id, currentMonthPlayers, databasePlayers, isFirstCompetition, isMe: true, out _);
                    var opponent = ResolvePlayer(opponentId, currentMonthPlayers, databasePlayers, isFirstCompetition, isMe: false, out isForeign);

                    var gameEntity = new Game
                    {
                        MyPoints = me.Points,
                        MyName = me.Name,
                        MySurname = me.Surname,
                        OpponentPoints = opponent.Points,
                        Name = opponent.Name,
                        Surname = opponent.Surname,
                        MySets = isMePlayer1 ? lgtfGame.player1_sets : lgtfGame.player2_sets,
                        OpponentSets = isMePlayer1 ? lgtfGame.player2_sets : lgtfGame.player1_sets,
                        TournamentId = tournament.Id,
                        TournamentName = tournament.Name,
                        TournamentDate = tournament.Date,
                        GameCoefficient = tournament.Coefficient,
                        IsOpponentForeign = isForeign,
                        MyPointsWithBonus = me.PointsWithBonus,
                        OpponentPointsWithBonus = opponent.PointsWithBonus,
                        MyPlace = me.Place,
                        OpponentPlace = opponent.Place,
                        MyAge = AgeCalculator.CalculateAge(me.BirthDate, tournament.Date),
                        OpponentAge = AgeCalculator.CalculateAge(opponent.BirthDate, tournament.Date)
                    };

                    await _database.SaveAsync(gameEntity);
                }
            }

            progress.Report("✅ Import finished");
            await Task.Delay(2000);
        }

        private async Task LoadAndMergeApiPlayersAsync(int year,int month,List<PlayerDB> currentMonthPlayers)
        {
            string date = $"{year:D4}-{month:D2}";
            var apiPlayers = await _repositoryWithDate.GetPlayersAsync(date, false);

            if (apiPlayers == null || apiPlayers.Count == 0)
                return;

            foreach (var apiPlayer in apiPlayers)
            {
                var existing = currentMonthPlayers
                    .FirstOrDefault(p => p.KeyName == apiPlayer.KeyName);

                if (existing != null)
                {
                    existing.Place = apiPlayer.Place;
                    existing.OverallPlace = apiPlayer.OverallPlace;
                    existing.Points = apiPlayer.Points;
                    existing.PointsWithBonus = apiPlayer.PointsWithBonus;
                }
                else
                {
                    currentMonthPlayers.Add(apiPlayer);
                }
            }
        }

        private static PlayerDB ResolvePlayer(int lgtfPlayerId,List<PlayerDB> currentMonthPlayers,List<PlayerDB> databasePlayers,bool isFirstMonth,bool isMe,out bool isForeign)
        {
            isForeign = false;
            var playerDB = databasePlayers.FirstOrDefault(p => p.Id == lgtfPlayerId);

            if (playerDB == null)
            {
                isForeign = true;
                return new PlayerDB{Id = lgtfPlayerId,Place = 0,Points = 0,PointsWithBonus = 0};
            }

            string playerKeyName = playerDB.KeyName;
            var player = currentMonthPlayers.FirstOrDefault(p => p.KeyName == playerKeyName);

            if (player != null)
                return player;

            if (isMe && isFirstMonth)
            {
                return new PlayerDB
                {
                    Id = playerDB.Id,
                    Name = playerDB.Name,
                    Surname = playerDB.Surname,
                    BirthDate = playerDB.BirthDate,
                    Gender = playerDB.Gender,
                    KeyName = playerDB.KeyName,

                    Points = 0,
                    PointsWithBonus = 0
                };
            }

            isForeign = true;
            return playerDB;
        }

        public async Task<List<PlayerDB>> GetPlayersFromDbAsync()
        {
            return (await _database.GetAllRecordsAsync<PlayerDB>()).OrderByDescending(x => x.PointsWithBonus).ToList();
        }

        private async Task SyncWithLocalDb(List<PlayerDB> apiPlayers)
        {
            await _database.BulkUpsertPlayersAsync(apiPlayers);
        }

        public async Task<List<Game>> GetGamesFromDbAsync()
        {
            return await _database.GetAllRecordsAsync<Game>();
        }

        public async Task<Tournament?> GetTournamentAsync(int id)
        {
            return await _database.GetByIdAsync<Tournament>(id);
        }

        private async Task UpdateAppDataWithDate(string dateString)
        {
            var appData = await _database.GetAppDataAsync();
            if (DateTime.TryParseExact(dateString, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                appData.CurrentYear = dt.Year;
                appData.CurrentMonth = dt.Month;
                await _database.SaveAppDataAsync(appData);
            }
        }

        public async Task<AppData> GetAppDataAsync()
        {
            return await _database.GetAppDataAsync();
        }

        public async Task SaveAppDataAsync(AppData appData)
        {
            await _database.SaveAppDataAsync(appData);
        }

        public async Task<PlayerDB?> GetAppDefaultPlayerAsync(AppData appData)
        {
            var appDefaultPlayer = await _database.GetByIdAsync<PlayerDB>(appData.AppUserPlayerId);

            return appDefaultPlayer;
        }
    }
}

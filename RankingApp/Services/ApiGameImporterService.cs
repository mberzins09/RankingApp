using RankingApp.Models;
using RankingApp.Data_Storage;
using SQLite;

namespace RankingApp.Services;

public class ApiGameImporterService(DatabaseService database)
{
    private readonly DatabaseService _database = database;

    public async Task InsertGamesAsync(List<APIGame> apiGames, Tournament tournament, List<PlayerDB> players, List<PlayerDB> databasePlayers, PlayerDB me)
    {
        var allGames = await _database.GetAllRecordsAsync<Game>();

        foreach (var apiGame in apiGames)
        {
            if (allGames.Any(g => g.ExternalGameId == apiGame.Id))
                continue;

            await InsertSingleGame(apiGame, tournament, players, databasePlayers, me);
        }
    }

    private async Task InsertSingleGame(APIGame apiGame, Tournament tournament, List<PlayerDB> players, List<PlayerDB> databasePlayers, PlayerDB me)
    {
        if (apiGame == null)
            return;

        if (apiGame.Player1 == null || apiGame.Player2 == null)
            return;

        string p1Key = NameNormalizer.NormalizeKey($"{apiGame.Player1.Name}{apiGame.Player1.Surname}");
        string p2Key = NameNormalizer.NormalizeKey($"{apiGame.Player2.Name}{apiGame.Player2.Surname}");

        bool isMePlayer1 = p1Key == me.KeyName;
        string oppKey = isMePlayer1 ? p2Key : p1Key;

        string? myScoreRaw = isMePlayer1 ? apiGame.Player1Score : apiGame.Player2Score;
        string? oppScoreRaw = isMePlayer1 ? apiGame.Player2Score : apiGame.Player1Score;

        if (!int.TryParse(myScoreRaw, out int mySets) || !int.TryParse(oppScoreRaw, out int oppSets))
        {
            return;
        }

        var opp = players.FirstOrDefault(p => p.KeyName == oppKey);
        opp ??= databasePlayers.FirstOrDefault(p => p.KeyName == oppKey);
        string oppName = isMePlayer1 ? apiGame.Player2.Name : apiGame.Player1.Name;
        string oppSurname = isMePlayer1 ? apiGame.Player2.Surname : apiGame.Player1.Surname;
        opp ??= new PlayerDB
        {
            Name = oppName,
            Surname = oppSurname,
            Points = 0,
            PointsWithBonus = 0,
            BirthDate = "",
            KeyName = NameNormalizer.NormalizeKey($"{oppName}{oppSurname}"),
            IsActive = false
        };

        var existingPlayer = await _database.GetPlayerByKeyAsync(opp.KeyName);

        if (existingPlayer == null)
        {
            await _database.SaveAsync(opp);
        }

        var game = new Game
        {
            ExternalGameId = apiGame.Id,

            MyName = me.Name,
            MySurname = me.Surname,
            MyPoints = me.Points,
            MyAge = AgeCalculator.CalculateAge(me.BirthDate, tournament.Date),
            MySets = mySets,
            MyPlace = me.Place,
            MyPointsWithBonus = me.PointsWithBonus,

            Name = opp.Name,
            Surname = opp.Surname,
            OpponentPoints = opp.Points,
            OpponentPointsWithBonus = opp.PointsWithBonus,
            OpponentAge = AgeCalculator.CalculateAge(opp.BirthDate, tournament.Date),
            OpponentPlace = opp.Place,
            OpponentSets = oppSets,

            TournamentId = tournament.Id,
            TournamentName = tournament.Name,
            TournamentDate = tournament.Date,
            GameCoefficient = tournament.Coefficient
        };

        await _database.SaveAsync(game);
    }
}

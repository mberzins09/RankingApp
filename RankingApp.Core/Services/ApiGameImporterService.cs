using RankingApp.Core.Models;
using RankingApp.Core.Services.Interfaces;

namespace RankingApp.Core.Services;

public class ApiGameImporterService(IDatabaseService database)
{
    private readonly IDatabaseService _database = database;

    /// <param name="myApiPlayerId">Optional API player id of the app user. When given, "me" is also
    /// recognised by id (not only by name key) - used by the teams import.</param>
    public async Task InsertGamesAsync(List<APIGame> apiGames, Tournament tournament, List<PlayerDB> players, List<PlayerDB> databasePlayers, PlayerDB me, int myApiPlayerId = 0)
    {
        var allGames = await _database.GetAllRecordsAsync<Game>();

        // Group game shown again in a later group stage is not a new game
        apiGames = TeamsGamesCollector.RemoveCarriedOverGroupGames(apiGames);

        foreach (var apiGame in apiGames)
        {
            if (allGames.Any(g => g.ExternalGameId == apiGame.Id))
                continue;

            await InsertSingleGame(apiGame, tournament, players, databasePlayers, me, myApiPlayerId);
        }
    }

    private async Task InsertSingleGame(APIGame apiGame, Tournament tournament, List<PlayerDB> players, List<PlayerDB> databasePlayers, PlayerDB me, int myApiPlayerId)
    {
        if (apiGame == null)
            return;

        if (apiGame.Player1 == null || apiGame.Player2 == null)
            return;

        string p1Key = NameNormalizer.NormalizeKey($"{apiGame.Player1.Name}{apiGame.Player1.Surname}");
        string p2Key = NameNormalizer.NormalizeKey($"{apiGame.Player2.Name}{apiGame.Player2.Surname}");

        bool isMePlayer1 = p1Key == me.KeyName || (myApiPlayerId > 0 && apiGame.Player1.Id == myApiPlayerId);
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

    // ====================================================================
    //  Doubles (teams events)
    // ====================================================================

    /// <summary>
    /// Inserts doubles games that are not in the DB yet (by ExternalGameId). Players are recognised
    /// by splitting the pair name "Name Surname / Name Surname" and normalising each part to a KeyName.
    /// </summary>
    public async Task InsertDoublesGamesAsync(List<APIDoublesGame> apiGames, Tournament tournament, List<PlayerDB> players, List<PlayerDB> databasePlayers, PlayerDB me)
    {
        if (apiGames == null || apiGames.Count == 0)
            return;

        var existingIds = (await _database.GetAllRecordsAsync<DoublesGame>())
            .Select(g => g.ExternalGameId)
            .Where(id => id != 0)
            .ToHashSet();

        foreach (var apiGame in apiGames)
        {
            if (existingIds.Contains(apiGame.Id))
                continue;

            await InsertDoublesGame(apiGame, tournament, players, databasePlayers, me);
            existingIds.Add(apiGame.Id);
        }
    }

    private async Task InsertDoublesGame(APIDoublesGame apiGame, Tournament tournament, List<PlayerDB> players, List<PlayerDB> databasePlayers, PlayerDB me)
    {
        var pair1 = TeamsGamesCollector.SplitPair(apiGame.Pair1Name);
        var pair2 = TeamsGamesCollector.SplitPair(apiGame.Pair2Name);

        if (pair1.Count != 2 || pair2.Count != 2)
            return;

        bool meInPair1 = pair1.Any(p => p.Key == me.KeyName);
        bool meInPair2 = pair2.Any(p => p.Key == me.KeyName);

        if (meInPair1 == meInPair2)
            return; // not my game (or broken data)

        var myPair = meInPair1 ? pair1 : pair2;
        var oppPair = meInPair1 ? pair2 : pair1;

        var partner = await ResolvePlayerAsync(myPair.First(p => p.Key != me.KeyName), players, databasePlayers);
        var opp1 = await ResolvePlayerAsync(oppPair[0], players, databasePlayers);
        var opp2 = await ResolvePlayerAsync(oppPair[1], players, databasePlayers);

        var game = new DoublesGame
        {
            ExternalGameId = apiGame.Id,

            MyName = me.Name,
            MySurname = me.Surname,
            MyPoints = me.Points,
            MyPointsWithBonus = me.PointsWithBonus,
            MyPlace = me.Place,
            MyAge = AgeCalculator.CalculateAge(me.BirthDate, tournament.Date),

            MyPartnerName = partner.Name,
            MyPartnerSurname = partner.Surname,
            MyPartnerPoints = partner.Points,
            MyPartnerPointsWithBonus = partner.PointsWithBonus,
            MyPartnerPlace = partner.Place,
            MyPartnerAge = AgeCalculator.CalculateAge(partner.BirthDate, tournament.Date),

            Opponent1Name = opp1.Name,
            Opponent1Surname = opp1.Surname,
            Opponent1Points = opp1.Points,
            Opponent1PointsWithBonus = opp1.PointsWithBonus,
            Opponent1Place = opp1.Place,
            Opponent1Age = AgeCalculator.CalculateAge(opp1.BirthDate, tournament.Date),

            Opponent2Name = opp2.Name,
            Opponent2Surname = opp2.Surname,
            Opponent2Points = opp2.Points,
            Opponent2PointsWithBonus = opp2.PointsWithBonus,
            Opponent2Place = opp2.Place,
            Opponent2Age = AgeCalculator.CalculateAge(opp2.BirthDate, tournament.Date),

            MySets = meInPair1 ? apiGame.Pair1Sets : apiGame.Pair2Sets,
            OpponentSets = meInPair1 ? apiGame.Pair2Sets : apiGame.Pair1Sets,

            TournamentId = tournament.Id,
            TournamentName = tournament.Name,
            TournamentDate = tournament.Date,
            GameCoefficient = tournament.Coefficient
        };

        await _database.SaveAsync(game);
    }

    /// <summary>
    /// Month ranking → current PlayerDB → new inactive PlayerDB (saved, same as for singles opponents).
    /// </summary>
    private async Task<PlayerDB> ResolvePlayerAsync((string FullName, string Key) player, List<PlayerDB> players, List<PlayerDB> databasePlayers)
    {
        var found = players.FirstOrDefault(p => p.KeyName == player.Key)
                    ?? databasePlayers.FirstOrDefault(p => p.KeyName == player.Key);

        if (found != null)
            return found;

        var existing = await _database.GetPlayerByKeyAsync(player.Key);
        if (existing != null)
            return existing;

        var (name, surname) = TeamsGamesCollector.SplitFullName(player.FullName);

        var created = new PlayerDB
        {
            Name = name,
            Surname = surname,
            Points = 0,
            PointsWithBonus = 0,
            BirthDate = "",
            KeyName = player.Key,
            IsActive = false
        };

        await _database.SaveAsync(created);
        return created;
    }
}

namespace RankingApp.Core.Models
{
    // DTOs returned by the public /api/rankingapp/* endpoints on MartinsWeb.
    // These replace the lgtf.sqlite file that used to be bundled with the app.
    // Property names must stay in sync with MartinsWeb/Models/RankingAppApiModels.cs.

    public class LgtfApiPlayer
    {
        public int     Id              { get; set; }
        public string  Name            { get; set; } = "";
        public string  Surname         { get; set; } = "";
        public string? BirthDate       { get; set; }
        public string? Gender          { get; set; }
        public int     Place           { get; set; }
        public int     OverallPlace    { get; set; }
        public int     Points          { get; set; }
        public int     PointsWithBonus { get; set; }
        public bool    IsActive        { get; set; }
        public int     NewId           { get; set; }
        public string? KeyName         { get; set; }
    }

    public class LgtfApiCompetition
    {
        public int     Id        { get; set; }
        public string  Name      { get; set; } = "";
        public string  StartDate { get; set; } = "";
        public string? Places    { get; set; }
        public double  Coef      { get; set; }
    }

    public class LgtfApiGame
    {
        public int    Id              { get; set; }
        public int    CompetitionId   { get; set; }

        public int    Player1Id       { get; set; }
        public int    Player2Id       { get; set; }
        public string Player1Name     { get; set; } = "";
        public string Player1Surname  { get; set; } = "";
        public string Player2Name     { get; set; } = "";
        public string Player2Surname  { get; set; } = "";
        public string Player1KeyName  { get; set; } = "";
        public string Player2KeyName  { get; set; } = "";

        public int    Player1Sets     { get; set; }
        public int    Player2Sets     { get; set; }
        public int    Player1Points          { get; set; }
        public int    Player2Points          { get; set; }
        public int    Player1PointsWithBonus { get; set; }
        public int    Player2PointsWithBonus { get; set; }
        public int    Player1Age      { get; set; }
        public int    Player2Age      { get; set; }
        public int    Player1Place    { get; set; }
        public int    Player2Place    { get; set; }
    }

    public class LgtfApiPlayerHistory
    {
        /// <summary>The player's Id on the server (compare with Player1Id / Player2Id, not with the local PlayerDB.Id).</summary>
        public int    PlayerId { get; set; }
        public string KeyName  { get; set; } = "";
        public List<LgtfApiCompetition> Competitions { get; set; } = [];
        public List<LgtfApiGame>        Games        { get; set; } = [];
    }
}

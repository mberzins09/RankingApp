namespace RankingApp.Core.Models
{
    public enum ResultViewKind { Participants, Group, Bracket, Stage, TeamMatches }

    /// <summary>One entry of the "what to show" picker on the event results page.</summary>
    public class ResultViewOption
    {
        public string Key { get; set; } = "";
        public string Title { get; set; } = "";
        public ResultViewKind Kind { get; set; }

        // Stage entries only: heading shown on top and keys of the groups / brackets of that stage, in order
        public string Heading { get; set; } = "";
        public List<string> ItemKeys { get; set; } = [];

        // Picker shows ToString()
        public override string ToString() => Title;
    }

    /// <summary>Entry of the month picker. Number 0 = all months.</summary>
    public class MonthOption
    {
        public int Number { get; set; }
        public string Name { get; set; } = "";

        public override string ToString() => Name;
    }

    // ── Group table (cross table) ────────────────────────────────────────────

    public enum CellOutcome { None, Win, Loss }

    public class GroupTableCell
    {
        public bool IsSelf { get; set; }
        public CellOutcome Outcome { get; set; }
        public string Points { get; set; } = "";
        public string Score { get; set; } = "";

        /// <summary>Ranking change of the row player in this game (null = no ranking data).</summary>
        public int? RatingChange { get; set; }
    }

    public class GroupTableRow
    {
        public int Number { get; set; }
        public string PlayerName { get; set; } = "";
        public string Points { get; set; } = "";
        public string Place { get; set; } = "";
        public List<GroupTableCell> Cells { get; set; } = [];
    }

    public class GroupTableModel
    {
        public string Title { get; set; } = "";
        public List<GroupTableRow> Rows { get; set; } = [];
    }

    // ── Play-off bracket ─────────────────────────────────────────────────────

    public class BracketMatch
    {
        public string Player1 { get; set; } = "";
        public string Player2 { get; set; } = "";
        public string Score1 { get; set; } = "";
        public string Score2 { get; set; } = "";

        /// <summary>0 = not decided, 1 = first player won, 2 = second player won.</summary>
        public int Winner { get; set; }

        /// <summary>Ranking change of each player in this game (null = not decided / no ranking data / doubles).</summary>
        public int? RatingChange1 { get; set; }
        public int? RatingChange2 { get; set; }
    }

    public class BracketRound
    {
        public string Title { get; set; } = "";
        public List<BracketMatch> Matches { get; set; } = [];
    }

    public class BracketModel
    {
        public string Title { get; set; } = "";
        public List<BracketRound> Rounds { get; set; } = [];
        public string WinnerName { get; set; } = "";
    }
}

namespace RankingApp.Core.Models
{
    /// <summary>
    /// A list row that can show / hide child rows right below it (the children are inserted into the same flat list,
    /// so a CollectionView never has to resize an existing row).
    /// </summary>
    public interface IExpandableRow
    {
        bool IsExpanded { get; set; }
        IEnumerable<object> ChildRows { get; }
    }

    // ── Teams events: team matches with their singles / doubles games ────────

    /// <summary>One team match ("Team A 4 : 3 Team B"). Tap shows its games.</summary>
    public partial class TeamMatchItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject, IExpandableRow
    {
        public string Team1 { get; set; } = "";
        public string Team2 { get; set; } = "";
        public string Score1 { get; set; } = "";
        public string Score2 { get; set; } = "";
        public string Subtitle { get; set; } = "";

        /// <summary>0 = not decided, 1 = first team won, 2 = second team won.</summary>
        public int Winner { get; set; }

        public List<TeamGameItem> Games { get; set; } = [];

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [CommunityToolkit.Mvvm.ComponentModel.NotifyPropertyChangedFor(nameof(Chevron))]
        private bool isExpanded;

        public string Score => $"{Score1} : {Score2}";
        public bool IsTeam1Winner => Winner == 1;
        public bool IsTeam2Winner => Winner == 2;
        public string Chevron => Games.Count == 0 ? "" : IsExpanded ? "▴" : "▾";
        public IEnumerable<object> ChildRows => Games;
    }

    /// <summary>One game of a team match: "+5 Player1 – Player2 -4   3:1". Doubles have no rating change.</summary>
    public class TeamGameItem
    {
        public string Player1 { get; set; } = "";
        public string Player2 { get; set; } = "";
        public string Score { get; set; } = "";
        public bool IsDoubles { get; set; }

        /// <summary>0 = not played, 1 = first player won, 2 = second player won.</summary>
        public int Winner { get; set; }

        public int? RatingChange1 { get; set; }
        public int? RatingChange2 { get; set; }

        public bool IsPlayer1Winner => Winner == 1;
        public bool IsPlayer2Winner => Winner == 2;

        public bool HasRating => RatingChange1.HasValue && RatingChange2.HasValue;
        public string Rating1Text => RatingFormat.Change(RatingChange1 ?? 0);
        public string Rating2Text => RatingFormat.Change(RatingChange2 ?? 0);
        public bool IsRating1Positive => RatingChange1 > 0;
        public bool IsRating1Negative => RatingChange1 < 0;
        public bool IsRating2Positive => RatingChange2 > 0;
        public bool IsRating2Negative => RatingChange2 < 0;
    }
}

namespace RankingApp.Core.Models
{
    public enum ResultViewKind { Participants, Group, Bracket, Stage }

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

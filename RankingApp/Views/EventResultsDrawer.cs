using Microsoft.Maui.Controls.Shapes;
using RankingApp.Core.Models;

namespace RankingApp.Views;

/// <summary>
/// Draws group cross tables and play-off brackets in the app's purple style.
/// Colors come from Resources/Styles/Colors.xaml.
/// </summary>
public static class EventResultsDrawer
{
    private static Color C(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : fallback;

    private static Color TextMain => C("TextMain", Colors.White);
    private static Color TextSoft => C("TextSoft", Colors.LightGray);
    private static Color TextFaint => C("TextFaint", Colors.Gray);
    private static Color Positive => C("Positive", Colors.LightGreen);
    private static Color Negative => C("Negative", Colors.IndianRed);
    private static Color Glass => C("Glass", Color.FromArgb("#1FFFFFFF"));
    private static Color GlassStrong => C("GlassStrong", Color.FromArgb("#33FFFFFF"));
    private static Color Divider => C("Divider", Color.FromArgb("#26FFFFFF"));
    private static Color Orchid => C("Orchid", Colors.MediumPurple);

    private static Label MakeLabel(string text, double size, Color color, bool bold = false,
        TextAlignment horizontal = TextAlignment.Center, LineBreakMode lineBreak = LineBreakMode.NoWrap) => new()
    {
        Text = text,
        FontSize = size,
        TextColor = color,
        FontFamily = bold ? "OpenSansSemibold" : "OpenSansRegular",
        HorizontalTextAlignment = horizontal,
        VerticalTextAlignment = TextAlignment.Center,
        LineBreakMode = lineBreak,
        MaxLines = 1
    };

    // ══ Group table ══════════════════════════════════════════════════════

    private const double NrWidth = 34, NameWidth = 150, CellWidth = 58, HeaderHeight = 34, RowHeight = 56;

    public static View DrawGroupTable(GroupTableModel model)
    {
        int n = model.Rows.Count;

        var grid = new Grid { ColumnSpacing = 0, RowSpacing = 0, HorizontalOptions = LayoutOptions.Start };

        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(NrWidth)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(NameWidth)));

        for (int i = 0; i < n; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(CellWidth)));

        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(CellWidth)));   // points
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(CellWidth)));   // place

        grid.RowDefinitions.Add(new RowDefinition(new GridLength(HeaderHeight)));

        for (int i = 0; i < n; i++)
            grid.RowDefinitions.Add(new RowDefinition(new GridLength(RowHeight)));

        // Header
        var headers = new List<string> { "Nr.", "Player" };
        headers.AddRange(Enumerable.Range(1, n).Select(i => i.ToString()));
        headers.Add("Points");
        headers.Add("Place");

        for (int c = 0; c < headers.Count; c++)
        {
            var label = MakeLabel(headers[c], 12, TextSoft, bold: c >= headers.Count - 2,
                horizontal: c == 1 ? TextAlignment.Start : TextAlignment.Center);

            AddCell(grid, label, 0, c, GlassStrong, c == 1 ? new Thickness(10, 0) : new Thickness(2, 0));
        }

        // Rows
        for (int r = 0; r < n; r++)
        {
            var row = model.Rows[r];
            int gridRow = r + 1;
            int col = 0;

            AddCell(grid, MakeLabel($"{row.Number}.", 12, TextFaint), gridRow, col++, Glass);

            AddCell(grid, MakeLabel(row.PlayerName, 13, TextMain, bold: true, TextAlignment.Start, LineBreakMode.TailTruncation),
                gridRow, col++, Glass, new Thickness(10, 0));

            foreach (var cell in row.Cells)
                AddCell(grid, BuildCellContent(cell), gridRow, col++, cell.IsSelf ? GlassStrong : Glass);

            AddCell(grid, MakeLabel(row.Points, 15, TextMain, bold: true), gridRow, col++, Glass);
            AddCell(grid, MakeLabel(row.Place, 15, TextMain, bold: true), gridRow, col, Glass);
        }

        var stack = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(4, 6, 14, 20) };
        stack.Children.Add(MakeLabel(model.Title, 18, TextMain, bold: true, TextAlignment.Start));
        stack.Children.Add(new Border
        {
            Stroke = Divider,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = 0,
            HorizontalOptions = LayoutOptions.Start,
            Content = grid
        });

        return stack;
    }

    private static View? BuildCellContent(GroupTableCell cell)
    {
        if (cell.IsSelf || string.IsNullOrEmpty(cell.Score))
            return null;

        Color scoreColor = cell.Outcome switch
        {
            CellOutcome.Win => Positive,
            CellOutcome.Loss => Negative,
            _ => TextMain
        };

        var stack = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
        stack.Children.Add(MakeLabel(cell.Points, 12, TextFaint));
        stack.Children.Add(MakeLabel(cell.Score, 14, scoreColor, bold: true));
        return stack;
    }

    private static void AddCell(Grid grid, View? content, int row, int column, Color background, Thickness? padding = null)
    {
        var border = new Border
        {
            Stroke = Divider,
            StrokeThickness = 0.5,
            BackgroundColor = background,
            Padding = padding ?? new Thickness(2),
            Content = content
        };

        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        grid.Children.Add(border);
    }

    // ══ Play-off bracket ═════════════════════════════════════════════════

    private const double MatchWidth = 180, PlayerRowHeight = 26, ColumnGap = 40, WinnerWidth = 150, WinnerHeight = 30, TopOffset = 30;
    private const double MatchHeight = PlayerRowHeight * 2;
    private const double Slot = MatchHeight + 16;

    public static View DrawBracket(BracketModel model)
    {
        var layout = new AbsoluteLayout();
        var centers = new List<double[]>();   // vertical center of every match, per round
        double bottom = TopOffset + MatchHeight;

        // ── positions ───────────────────────────────────────────────────
        for (int r = 0; r < model.Rounds.Count; r++)
        {
            int count = model.Rounds[r].Matches.Count;
            var c = new double[count];

            for (int i = 0; i < count; i++)
            {
                if (r == 0)
                {
                    c[i] = TopOffset + i * Slot + MatchHeight / 2;
                }
                else
                {
                    var prev = centers[r - 1];

                    c[i] = prev.Length >= 2 * count && 2 * i + 1 < prev.Length
                        ? (prev[2 * i] + prev[2 * i + 1]) / 2          // between its two feeder matches
                        : prev[Math.Min(i, prev.Length - 1)];           // bye: stays level with the previous match
                }

                bottom = Math.Max(bottom, c[i] + MatchHeight / 2);
            }

            centers.Add(c);
        }

        // ── drawing ─────────────────────────────────────────────────────
        for (int r = 0; r < model.Rounds.Count; r++)
        {
            double x = r * (MatchWidth + ColumnGap);

            Place(layout, MakeLabel(model.Rounds[r].Title, 12, TextFaint, bold: true), x, 0, MatchWidth, 24);

            for (int i = 0; i < model.Rounds[r].Matches.Count; i++)
            {
                double center = centers[r][i];

                Place(layout, BuildMatchCard(model.Rounds[r].Matches[i]), x, center - MatchHeight / 2, MatchWidth, MatchHeight);

                if (r == 0)
                    continue;

                // connectors from the previous round
                var prev = centers[r - 1];
                double prevRight = (r - 1) * (MatchWidth + ColumnGap) + MatchWidth;
                double mid = prevRight + ColumnGap / 2;

                if (prev.Length >= 2 * model.Rounds[r].Matches.Count && 2 * i + 1 < prev.Length)
                {
                    double a = prev[2 * i], b = prev[2 * i + 1];

                    Line(layout, prevRight, a, mid - prevRight, true);
                    Line(layout, prevRight, b, mid - prevRight, true);
                    Line(layout, mid, Math.Min(a, b), Math.Abs(b - a), false);
                    Line(layout, mid, center, x - mid, true);
                }
                else
                {
                    Line(layout, prevRight, prev[Math.Min(i, prev.Length - 1)], x - prevRight, true);
                }
            }
        }

        // ── winner ──────────────────────────────────────────────────────
        double width = model.Rounds.Count * (MatchWidth + ColumnGap);

        if (model.Rounds.Count > 0 && !string.IsNullOrEmpty(model.WinnerName))
        {
            double finalCenter = centers[^1][^1];
            double lastRight = (model.Rounds.Count - 1) * (MatchWidth + ColumnGap) + MatchWidth;

            Line(layout, lastRight, finalCenter, ColumnGap, true);

            var winner = new Border
            {
                BackgroundColor = Orchid,
                Stroke = Divider,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(8, 0),
                Content = MakeLabel(model.WinnerName, 13, TextMain, bold: true, TextAlignment.Start, LineBreakMode.TailTruncation)
            };

            Place(layout, winner, width, finalCenter - WinnerHeight / 2, WinnerWidth, WinnerHeight);
            width += WinnerWidth;
        }

        layout.WidthRequest = width + 4;
        layout.HeightRequest = bottom + 8;
        layout.HorizontalOptions = LayoutOptions.Start;

        var stack = new VerticalStackLayout { Spacing = 8, Padding = new Thickness(4, 6, 14, 20) };
        stack.Children.Add(MakeLabel(model.Title, 18, TextMain, bold: true, TextAlignment.Start));
        stack.Children.Add(layout);

        return stack;
    }

    private static View BuildMatchCard(BracketMatch match)
    {
        var grid = new Grid { RowSpacing = 0 };
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(PlayerRowHeight)));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(PlayerRowHeight)));

        AddPlayerRow(grid, 0, match.Player1, match.Score1, won: match.Winner == 1, decided: match.Winner != 0);
        AddPlayerRow(grid, 1, match.Player2, match.Score2, won: match.Winner == 2, decided: match.Winner != 0);

        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Padding = 0,
            Content = grid
        };
    }

    private static void AddPlayerRow(Grid grid, int row, string name, string score, bool won, bool decided)
    {
        var line = new Grid { ColumnSpacing = 6, Padding = new Thickness(8, 0), BackgroundColor = won ? Orchid : Glass };
        line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        line.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        Color color = won ? TextMain : decided ? TextFaint : TextSoft;

        line.Children.Add(MakeLabel(name, 12, color, bold: won, TextAlignment.Start, LineBreakMode.TailTruncation));

        var scoreLabel = MakeLabel(score, 13, color, bold: true);
        Grid.SetColumn(scoreLabel, 1);
        line.Children.Add(scoreLabel);

        Grid.SetRow(line, row);
        grid.Children.Add(line);
    }

    private static void Place(AbsoluteLayout layout, View view, double x, double y, double width, double height)
    {
        AbsoluteLayout.SetLayoutBounds(view, new Rect(x, y, width, height));
        layout.Children.Add(view);
    }

    /// <summary>Connector line: horizontal from (x, y) to the right, or vertical from (x, y) downwards.</summary>
    private static void Line(AbsoluteLayout layout, double x, double y, double length, bool horizontal)
    {
        if (length <= 0)
            return;

        var box = new BoxView { Color = TextFaint, CornerRadius = 1 };

        Place(layout, box,
            horizontal ? x : x - 1,
            horizontal ? y - 1 : y,
            horizontal ? length : 2,
            horizontal ? 2 : length);
    }
}

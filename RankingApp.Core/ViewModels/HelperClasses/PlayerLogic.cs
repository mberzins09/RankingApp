using RankingApp.Core.Models;
using System.Text.RegularExpressions;

namespace RankingApp.Core.ViewModels.HelperClasses
{
    public static class PlayerLogic
    {
        public static List<PlayerDB> Filter(List<PlayerDB> players, string filter)
        {
            return filter switch
            {
                "Men" => [.. players.Where(p => p.Gender == "male" && p.IsActive)],
                "Women" => [.. players.Where(p => p.Gender == "female" && p.IsActive)],
                "Inactive" => [.. players.Where(p => !p.IsActive)],
                "All" => players,
                _ => players.Where(p => p.IsActive).ToList(),
            };
        }

        public static List<PlayerListItem> Sort(List<PlayerDB> players, string sort)
        {
            IEnumerable<PlayerDB> sorted = sort switch
            {
                "Points" => players.OrderByDescending(p => p.Points),
                "PointsChanged" => players.OrderByDescending(p => p.PointsChanged),
                "Age" => players.OrderByDescending(p => p.Age),
                _ => players.OrderByDescending(p => p.PointsWithBonus),
            };

            var list = sorted.ToList();

            return [.. list.Select((p, index) => new PlayerListItem
            {
                Player = p,
                Place = index + 1
            })];
        }

        public static List<PlayerListItem> Search(List<PlayerListItem> players, string? searchText)
        {
            if (players == null || players.Count == 0)
                return [];

            if (string.IsNullOrWhiteSpace(searchText))
                return players;

            var input = searchText.Trim();
            var normalizedInput = NameNormalizer.NormalizeKey(input);

            var rangePattern = @"^\s*(\d+)\s*[-.]{1,2}\s*(\d+)\s*$";
            var greaterThanPattern = @"^>\s*(\d+)$";
            var greaterOrEqualPattern = @"^>=\s*(\d+)$";
            var lessThanPattern = @"^<\s*(\d+)$";
            var lessOrEqualPattern = @"^<=\s*(\d+)$";

            IEnumerable<PlayerListItem> result = players;

            switch (input)
            {
                case var s when Regex.IsMatch(s, rangePattern):
                    var match = Regex.Match(s, rangePattern);

                    int start = int.Parse(match.Groups[1].Value);
                    int end = int.Parse(match.Groups[2].Value);

                    result = result.Where(p => p.Place >= start && p.Place <= end);
                    break;

                case var s when Regex.IsMatch(s, greaterOrEqualPattern):
                    int val = int.Parse(Regex.Match(s, greaterOrEqualPattern).Groups[1].Value);
                    result = result.Where(p => p.Place >= val);
                    break;

                case var s when Regex.IsMatch(s, greaterThanPattern):
                    val = int.Parse(Regex.Match(s, greaterThanPattern).Groups[1].Value);
                    result = result.Where(p => p.Place > val);
                    break;

                case var s when Regex.IsMatch(s, lessOrEqualPattern):
                    val = int.Parse(Regex.Match(s, lessOrEqualPattern).Groups[1].Value);
                    result = result.Where(p => p.Place <= val);
                    break;

                case var s when Regex.IsMatch(s, lessThanPattern):
                    val = int.Parse(Regex.Match(s, lessThanPattern).Groups[1].Value);
                    result = result.Where(p => p.Place < val);
                    break;

                default:
                    result = result.Where(p => p.Player.KeyName?.Contains(normalizedInput) == true);
                    break;
            }

            return [.. result];
        }
    }
}

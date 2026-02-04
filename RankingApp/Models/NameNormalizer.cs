using System.Globalization;
using System.Text;

namespace RankingApp.Models
{
    public static class NameNormalizer
    {
        public static string NormalizeKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var normalized = input
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                if (Char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString()
                     .Replace(" ", "")
                     .Replace("-", "");
        }
    }
}

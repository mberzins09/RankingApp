using System.Globalization;

namespace RankingApp.Core.Models
{
    /// <summary>
    /// The API sends coefficients as "1.50" / "0.50", but the Tournament page Picker offers "1.5" / "0.5".
    /// The Picker only shows a SelectedItem that is exactly one of its options, so always store the short form.
    /// </summary>
    public static class CoefficientFormatter
    {
        public static string Normalize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string cleaned = raw.Trim().Replace(',', '.');

            return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value.ToString("0.##", CultureInfo.InvariantCulture)
                : raw.Trim();
        }
    }
}

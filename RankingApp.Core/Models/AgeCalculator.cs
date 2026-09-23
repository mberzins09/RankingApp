using System.Globalization;

namespace RankingApp.Core.Models
{
    public static class AgeCalculator
    {
        // Formats seen from the ranking APIs and lgtf.sqlite: "1987-05-09", "1987-05-08T20:00:00.000000Z", "09.05.1987"
        private static readonly string[] KnownFormats =
        [
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "dd.MM.yyyy",
            "d.M.yyyy",
            "yyyy.MM.dd",
            "dd/MM/yyyy"
        ];

        /// <summary>
        /// Parses a birth date independent of the phone's language settings.
        /// Returns false for empty or unusable values ("", "0000-00-00", dates in the future).
        /// </summary>
        public static bool TryParseBirthDate(string? birthDate, out DateTime birth)
        {
            birth = default;

            if (string.IsNullOrWhiteSpace(birthDate))
                return false;

            string value = birthDate.Trim();

            if (DateTime.TryParseExact(value, KnownFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out birth))
            {
                birth = birth.Date;
            }
            else if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
                         DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out birth))
            {
                // "1987-05-08T20:00:00Z" is midnight of 9 May in Latvia (UTC+2..+4). A birth date has no
                // real time, so an evening UTC time always means the next calendar day.
                birth = birth.Hour >= 12 ? birth.Date.AddDays(1) : birth.Date;
            }
            else
            {
                return false;
            }

            return birth.Year > 1900 && birth <= DateTime.Today;
        }

        public static bool HasValidBirthDate(string? birthDate) => TryParseBirthDate(birthDate, out _);

        public static int CalculateAge(string BirthDate, DateTime date)
        {
            if (!TryParseBirthDate(BirthDate, out var birth))
            {
                return 0;
            }

            int age = date.Year - birth.Year;
            if (birth.AddYears(age) > date)
            {
                age--;
            }

            return Math.Max(age, 0);
        }

        /// <summary>Age today (not frozen at app start).</summary>
        public static int Calculate(string BirthDate)
        {
            return CalculateAge(BirthDate, DateTime.Now);
        }
    }
}

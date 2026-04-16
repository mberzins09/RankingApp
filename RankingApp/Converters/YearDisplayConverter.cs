using System.Globalization;

namespace RankingApp.Converters
{
    public class YearDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int year)
                return year == 0 ? "All Years" : year.ToString();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;

            if (string.IsNullOrWhiteSpace(text))
                return 0;

            if (text.Equals("All Years", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (int.TryParse(text, out int result))
                return result;

            return 0;
        }
    }
}

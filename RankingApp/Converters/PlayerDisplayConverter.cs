using RankingApp.Models;
using System.Globalization;

namespace RankingApp.Converters
{
    public class PlayerDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlayerDB player && parameter is string filter)
            {
                return (filter == "Mens" || filter == "Womens") ? player.Display : player.AllDisplay;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

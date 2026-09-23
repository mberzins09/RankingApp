using System.Globalization;

namespace RankingApp.Converters
{
    public class PointsChangedToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int pointsChanged)
            {
                if (pointsChanged > 0) return Color.FromArgb("#6EE7B7");
                if (pointsChanged < 0) return Color.FromArgb("#FF7A8A");
            }
            return Color.FromArgb("#A992D6");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

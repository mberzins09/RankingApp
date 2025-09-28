using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace RankingApp.Converters
{
    public class PointsChangedToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int pointsChanged)
            {
                if (pointsChanged > 0) return Colors.Green;
                if (pointsChanged < 0) return Colors.Red;
            }
            return Colors.DimGrey;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

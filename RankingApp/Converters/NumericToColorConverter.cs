using System.Globalization;

namespace RankingApp.Converters
{
    public class NumericToColorConverter : IValueConverter
    {
        public Color PositiveColor { get; set; } = Colors.Green;
        public Color NegativeColor { get; set; } = Colors.Red;
        public Color NeutralColor { get; set; } = Colors.White;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return NeutralColor;

            double num;
            double parsed;
            switch (value)
            {
                case int i:
                    num = i;
                    break;
                case long l:
                    num = l;
                    break;
                case double d:
                    num = d;
                    break;
                case float f:
                    num = f;
                    break;
                case decimal m:
                    num = (double)m;
                    break;
                case string s when double.TryParse(s, NumberStyles.Any, culture, out parsed):
                    num = parsed;
                    break;
                default:
                    if (double.TryParse(value.ToString(), NumberStyles.Any, culture, out parsed))
                        num = parsed;
                    else
                        return NeutralColor;
                    break;
            }

            if (num > 0) return PositiveColor;
            if (num < 0) return NegativeColor;
            return NeutralColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

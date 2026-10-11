using System.Globalization;
using System.Windows.Data;

namespace WinDeskClock.Converters
{
    /// <summary>Inverts a boolean value, e.g. to bind IsEnabled to "not this other flag".</summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !(value is true);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => !(value is true);
    }
}

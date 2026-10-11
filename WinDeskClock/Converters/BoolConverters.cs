using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace WinDeskClock.Converters
{
    /// <summary>True -> Visible, False -> Collapsed.</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is Visibility.Visible;
    }

    /// <summary>True -> 1.0, False -> 0.4 (used to dim an incompatible plugin's icon).</summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? 1.0 : 0.4;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>True -> Primary (highlighted) button appearance, False -> Secondary.</summary>
    public class BoolToAppearanceConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? ControlAppearance.Primary : ControlAppearance.Secondary;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}

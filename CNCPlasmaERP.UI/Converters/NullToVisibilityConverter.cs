using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CNCPlasmaERP.UI.Converters
{
    /// <summary>
    /// Visible when the bound value is non-null; Collapsed when null. Use this for
    /// any object reference (e.g. a SelectedItem). StringToVisibilityConverter is
    /// specifically for strings and also treats an empty/whitespace string as
    /// absent — the two are not interchangeable.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is null ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

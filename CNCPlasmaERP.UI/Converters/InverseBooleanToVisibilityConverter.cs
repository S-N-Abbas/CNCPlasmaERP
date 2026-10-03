using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CNCPlasmaERP.UI.Converters
{
    /// <summary>Inverse of the standard BooleanToVisibilityConverter: true -&gt; Collapsed, false -&gt; Visible.</summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

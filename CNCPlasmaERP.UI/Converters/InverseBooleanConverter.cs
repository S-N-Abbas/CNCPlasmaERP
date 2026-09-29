using System;
using System.Globalization;
using System.Windows.Data;

namespace CNCPlasmaERP.UI.Converters
{
    /// <summary>Inverts a bool — e.g. bind a button's IsEnabled to "not busy".</summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;
    }
}

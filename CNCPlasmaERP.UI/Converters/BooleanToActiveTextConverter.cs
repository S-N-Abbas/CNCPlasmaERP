using System;
using System.Globalization;
using System.Windows.Data;

namespace CNCPlasmaERP.UI.Converters
{
    /// <summary>Converts a bool IsActive flag to "Active"/"Inactive" display text.</summary>
    public class BooleanToActiveTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? "Active" : "Inactive";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

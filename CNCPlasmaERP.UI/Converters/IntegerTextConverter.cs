using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CNCPlasmaERP.UI.Converters
{
    /// <summary>
    /// Two-way converter for numeric TextBoxes bound to a non-nullable integer.
    /// WPF's built-in conversion throws a FormatException when the box is
    /// emptied, leaving the ViewModel holding the OLD value while the box looks
    /// empty. This treats empty/unparseable text as 0, so the ViewModel always
    /// matches what the user sees. Zero displays as a blank box.
    /// </summary>
    public class IntegerTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is int i && i != 0 ? i.ToString("G29", culture) : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            int.TryParse(value as string, NumberStyles.Number, culture, out var result) ? result : 0;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CNCPlasmaERP.UI.Converters
{
    /// <summary>
    /// Two-way converter for numeric TextBoxes bound to a non-nullable decimal.
    /// WPF's built-in conversion throws a FormatException when the box is
    /// emptied, leaving the ViewModel holding the OLD value while the box looks
    /// empty. This treats empty/unparseable text as 0, so the ViewModel always
    /// matches what the user sees. Zero displays as a blank box.
    /// </summary>
    public class DecimalTextConverter : IValueConverter
    {
        private decimal? _lastParsedValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not decimal d)
            {
                return string.Empty;
            }

            if (_lastParsedValue.HasValue && _lastParsedValue.Value == d)
            {
                return Binding.DoNothing;
            }

            _lastParsedValue = null;
            return d == 0 ? string.Empty : d.ToString("G29", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = (value as string)?.Trim() ?? string.Empty;

            if (decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, culture, out var result))
            {
                _lastParsedValue = result;
                return result;
            }

            // Not a number yet. If it could still BECOME one ("", "-", "." or "-."),
            // keep the text as typed. Anything else (letters) is cleared on redraw.
            var separator = Regex.Escape(culture.NumberFormat.NumberDecimalSeparator);
            var isPartialNumber = Regex.IsMatch(text, $@"^-?\d*{separator}?\d*$");

            _lastParsedValue = isPartialNumber ? 0m : null;
            return 0m;
        }
    }
}

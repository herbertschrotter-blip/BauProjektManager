using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BauProjektManager.PlanManager.Views;

/// <summary>Hex-String ("#RRGGBB") → <see cref="Color"/>; ungueltig/leer = transparent (Segmenttyp-Farben).</summary>
public class HexToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try { return (Color)ColorConverter.ConvertFromString(hex)!; }
            catch (FormatException) { /* fallback */ }
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

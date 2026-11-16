using System.Globalization;
using System.Windows.Data;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>Kleine Konverter fuer das Chatfenster (x:Static, ohne Ressourcen-Eintrag).</summary>
public static class ClaudeChatConverters
{
    /// <summary>bool → !bool (z. B. IsEnabled = nicht beantwortet).</summary>
    public static IValueConverter Not { get; } = new NotConverter();

    private sealed class NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not true;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is not true;
    }
}

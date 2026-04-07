using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Settings.ViewModels;

namespace BauProjektManager.Settings.Views;

/// <summary>
/// Converts ProjectStatus enum to display text.
/// Active → "● Aktiv", Completed → "● Abgeschlossen", Archived → "● Archiviert"
/// </summary>
public class StatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.Active => "● Aktiv",
                ProjectStatus.Completed => "● Abgeschlossen",
                
                _ => status.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts ProjectStatus enum to color brush.
/// Active → Green, Completed → Red, Archived → Gray
/// </summary>
public class StatusColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Green = new(Color.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly SolidColorBrush Red = new(Color.FromRgb(0xE2, 0x4B, 0x4A));
    

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.Active => Green,
                ProjectStatus.Completed => Red,
                
                _ => Red
            };
        }
        return Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        // Register converters before InitializeComponent
        Resources.Add("StatusConverter", new StatusConverter());
        Resources.Add("StatusColorConverter", new StatusColorConverter());
        InitializeComponent();

        // Ordnerstruktur-Control initialisieren
        if (DataContext is SettingsViewModel vm2)
        {
            var template = vm2.GetFolderTemplate();
            GlobalFolderTemplate.LoadFromTemplate(template);
            GlobalFolderTemplate.TemplateChanged += () =>
            {
                vm2.SaveFolderTemplateFrom(GlobalFolderTemplate.ToTemplate());
            };
        }
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && vm.SelectedProject is not null)
        {
            vm.EditProjectCommand.Execute(null);
        }
    }

    private void OnBrowseBasePath(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Arbeitsordner wählen"
        };

        if (dialog.ShowDialog() == true && DataContext is SettingsViewModel vm)
        {
            vm.UpdateBasePath(dialog.FolderName);
        }
    }

    private void OnBrowseArchivePath(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Archivordner wählen"
        };

        if (dialog.ShowDialog() == true && DataContext is SettingsViewModel vm)
        {
            vm.UpdateArchivePath(dialog.FolderName);
        }
    }
}

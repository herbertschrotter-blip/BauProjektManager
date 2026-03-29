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
                ProjectStatus.Archived => "● Archiviert",
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
    private static readonly SolidColorBrush Gray = new(Color.FromRgb(0x99, 0x99, 0x99));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.Active => Green,
                ProjectStatus.Completed => Red,
                ProjectStatus.Archived => Gray,
                _ => Gray
            };
        }
        return Gray;
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
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && vm.SelectedProject is not null)
        {
            vm.EditProjectCommand.Execute(null);
        }
    }

    private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.SelectedTreeItem = e.NewValue as FolderTreeItem;
        }
    }

    private void OnTemplateMoveUp(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.TemplateMoveUpCommand.Execute(null);
    }

    private void OnTemplateMoveDown(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.TemplateMoveDownCommand.Execute(null);
    }

    private void OnTemplateAddMain(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.TemplateAddMainCommand.Execute(null);
    }

    private void OnTemplateAddSub(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.TemplateAddSubCommand.Execute(null);
    }

    private void OnTemplateRemove(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.TemplateRemoveCommand.Execute(null);
    }

    private void OnTemplateToggleInbox(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.TemplateToggleInboxCommand.Execute(null);
    }

    private void OnTemplateTogglePrefix(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.TemplateTogglePrefixCommand.Execute(null);
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

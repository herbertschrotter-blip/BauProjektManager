using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Settings.ViewModels;

namespace BauProjektManager.Settings.Views;

/// <summary>
/// Converts ProjectStatus enum to display text.
/// Active → "● Aktiv", Completed → "● Abgeschlossen"
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
/// Active → Green, Completed → Red
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

/// <summary>
/// Shows placeholder text when search field is empty.
/// </summary>
public class PlaceholderVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class SettingsView : UserControl
{
    // Filter button colors
    private static readonly SolidColorBrush ActiveFilterBg = new(Color.FromRgb(0x00, 0x7A, 0xCC));
    private static readonly SolidColorBrush ActiveFilterFg = new(Colors.White);
    private static readonly SolidColorBrush InactiveFilterBg = new(Colors.Transparent);
    private static readonly SolidColorBrush InactiveFilterFg = new(Color.FromRgb(0x99, 0x99, 0x99));

    public SettingsView(ProjectDatabase db, IDialogService dialogService)
    {
        // Register converters before InitializeComponent
        Resources.Add("StatusConverter", new StatusConverter());
        Resources.Add("StatusColorConverter", new StatusColorConverter());
        Resources.Add("PlaceholderVisibilityConverter", new PlaceholderVisibilityConverter());
        InitializeComponent();

        // ViewModel mit DI-Services erstellen
        var vm = new SettingsViewModel(db, dialogService);
        DataContext = vm;

        // Ordnerstruktur-Control initialisieren
        var template = vm.GetFolderTemplate();
        GlobalFolderTemplate.LoadFromTemplate(template);
        GlobalFolderTemplate.TemplateChanged += () =>
        {
            vm.SaveFolderTemplateFrom(GlobalFolderTemplate.ToTemplate());
        };

        // Filter-Buttons initial stylen
        UpdateFilterButtonStyles("Alle");

        // Suchfeld Platzhalter
        TxtSearch.GotFocus += (_, _) =>
        {
            if (TxtSearch.Text == "") TxtSearch.Tag = "focused";
        };
        TxtSearch.LostFocus += (_, _) => TxtSearch.Tag = null;

        // Filter-Button-Styles bei Property-Änderung aktualisieren
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.StatusFilter))
            {
                UpdateFilterButtonStyles(vm.StatusFilter);
            }
        };
    }

    private void UpdateFilterButtonStyles(string activeFilter)
    {
        StyleFilterButton(BtnFilterAll, activeFilter == "Alle");
        StyleFilterButton(BtnFilterActive, activeFilter == "Aktiv");
        StyleFilterButton(BtnFilterCompleted, activeFilter == "Abgeschlossen");
    }

    private static void StyleFilterButton(Button btn, bool isActive)
    {
        btn.Background = isActive ? ActiveFilterBg : InactiveFilterBg;
        btn.Foreground = isActive ? ActiveFilterFg : InactiveFilterFg;
    }

    private void OnFilterButtonClick(object sender, RoutedEventArgs e)
    {
        // Styling wird über PropertyChanged-Event gehandelt
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && vm.SelectedProject is not null)
        {
            vm.EditProjectCommand.Execute(null);
        }
    }

    private void OnNewProjectClick(object sender, RoutedEventArgs e)
    {
        NewProjectPopup.IsOpen = true;
    }

    private void OnMenuCreateProject(object sender, MouseButtonEventArgs e)
    {
        NewProjectPopup.IsOpen = false;
        if (DataContext is SettingsViewModel vm)
            vm.AddProjectCommand.Execute(null);
    }

    private void OnMenuImportProject(object sender, MouseButtonEventArgs e)
    {
        NewProjectPopup.IsOpen = false;
        if (DataContext is SettingsViewModel vm)
            vm.ImportFolderCommand.Execute(null);
    }

    private void OnMenuItemEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
            border.Background = (Brush)FindResource("BpmBgHover");
    }

    private void OnMenuItemLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
            border.Background = Brushes.Transparent;
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                if (System.IO.Directory.Exists(path))
                    System.Diagnostics.Process.Start("explorer.exe", path);
            }
            catch { }
        }
    }

    private void OnContextOpenFolder(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.OpenProjectFolderCommand.Execute(null);
    }

    private void OnContextEdit(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.EditProjectCommand.Execute(null);
    }

    private void OnContextDelete(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.DeleteProjectCommand.Execute(null);
    }

    private void OnDataGridKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not SettingsViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            vm.EditProjectCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            vm.DeleteProjectCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            vm.OpenProjectFolderCommand.Execute(null);
            e.Handled = true;
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
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BauProjektManager.Settings.Views;

/// <summary>Tab 4 Portale und Links: CRUD, Portal-Typen, Link-Vorschau, Browser-Aufruf (BPM-070 Partial-Split).</summary>
public partial class ProjectEditDialog
{
    private void OnAddPortal(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSharedOrDefault();
        var link = new ProjectLink { LinkType = "Portal" };
        if (ShowLinkEditDialog(link, settings.PortalTypes, "Portal hinzufügen", true))
        {
            link.SortOrder = _portalLinks.Count;
            _portalLinks.Add(link);
            RefreshLinkPreview();
        }
    }

    private void OnEditPortal(object sender, RoutedEventArgs e)
    {
        if (DgPortals.SelectedItem is not ProjectLink link) return;
        var settings = _settingsService.LoadSharedOrDefault();
        ShowLinkEditDialog(link, settings.PortalTypes, "Portal bearbeiten", true);
        DgPortals.Items.Refresh();
        RefreshLinkPreview();
    }

    private void OnRemovePortal(object sender, RoutedEventArgs e)
    {
        if (DgPortals.SelectedItem is not ProjectLink link) return;
        if (MessageBox.Show($"Portal \"{link.Name}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _portalLinks.Remove(link);
            RefreshLinkPreview();
        }
    }

    private void OnOpenPortal(object sender, RoutedEventArgs e)
    {
        if (DgPortals.SelectedItem is not ProjectLink link || !link.IsConfigured) return;
        OpenUrlInBrowser(link.Url);
    }

    private void OnAddCustomLink(object sender, RoutedEventArgs e)
    {
        var link = new ProjectLink { LinkType = "Custom" };
        if (ShowLinkEditDialog(link, null, "Link hinzufügen", false))
        {
            link.SortOrder = _customLinks.Count;
            _customLinks.Add(link);
            RefreshLinkPreview();
        }
    }

    private void OnEditCustomLink(object sender, RoutedEventArgs e)
    {
        if (DgCustomLinks.SelectedItem is not ProjectLink link) return;
        ShowLinkEditDialog(link, null, "Link bearbeiten", false);
        DgCustomLinks.Items.Refresh();
        RefreshLinkPreview();
    }

    private void OnRemoveCustomLink(object sender, RoutedEventArgs e)
    {
        if (DgCustomLinks.SelectedItem is not ProjectLink link) return;
        if (MessageBox.Show($"Link \"{link.Name}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _customLinks.Remove(link);
            RefreshLinkPreview();
        }
    }

    private void OnOpenCustomLink(object sender, RoutedEventArgs e)
    {
        if (DgCustomLinks.SelectedItem is not ProjectLink link || !link.IsConfigured) return;
        OpenUrlInBrowser(link.Url);
    }

    private void OnEditPortalTypes(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSharedOrDefault();
        var items = new ObservableCollection<string>(settings.PortalTypes);
        if (ShowSimpleListEditDialog("Portal-Typen bearbeiten", items))
        {
            settings.PortalTypes = items.ToList();
            _settingsService.SaveSharedOrDefault(settings);
        }
    }

    private bool ShowLinkEditDialog(ProjectLink link, List<string>? nameOptions, string title, bool isPortal)
    {
        var w = new Window
        {
            Title = title, Width = 450, Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 3; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Name: ComboBox für Portale, TextBox für eigene Links
        System.Windows.Controls.Control nameControl;
        if (isPortal && nameOptions is not null)
        {
            var cmb = new ComboBox { IsEditable = true, ItemsSource = nameOptions, Text = link.Name, Margin = new Thickness(0, 3, 0, 3) };
            Grid.SetRow(cmb, 0); Grid.SetColumn(cmb, 1);
            nameControl = cmb;
        }
        else
        {
            var txt = MakeTextBox(link.Name, 0, 1);
            nameControl = txt;
        }

        var txtUrl = MakeTextBox(link.Url, 1, 1);

        var btnOk = new Button
        {
            Content = "OK", Width = 80, Padding = new Thickness(0, 5, 0, 5), Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 2); Grid.SetColumn(btnOk, 1);
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        grid.Children.Add(MakeLabel(isPortal ? "Portal:" : "Bezeichnung:", 0, 0));
        grid.Children.Add(nameControl);
        grid.Children.Add(MakeLabel("URL:", 1, 0));
        grid.Children.Add(txtUrl);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            if (nameControl is ComboBox cmb2)
                link.Name = (cmb2.SelectedItem as string ?? cmb2.Text).Trim();
            else if (nameControl is TextBox txt2)
                link.Name = txt2.Text.Trim();
            link.Url = txtUrl.Text.Trim();
            return true;
        }
        return false;
    }

    private void RefreshLinkPreview()
    {
        if (WpLinkPreview is null) return;
        WpLinkPreview.Children.Clear();

        foreach (var link in _portalLinks.Where(l => l.IsConfigured))
        {
            var btn = new Button
            {
                Content = link.Name, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 6),
                Background = BrushFromHex("#005A9E"), Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, FontSize = 12
            };
            btn.Click += (_, _) => OpenUrlInBrowser(link.Url);
            WpLinkPreview.Children.Add(btn);
        }

        foreach (var link in _customLinks.Where(l => l.IsConfigured))
        {
            var btn = new Button
            {
                Content = link.Name, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 6),
                Background = BrushFromHex("#3E3E42"), Foreground = BrushFromHex("#CCCCCC"),
                BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, FontSize = 12
            };
            btn.Click += (_, _) => OpenUrlInBrowser(link.Url);
            WpLinkPreview.Children.Add(btn);
        }

        if (WpLinkPreview.Children.Count == 0)
        {
            WpLinkPreview.Children.Add(new TextBlock
            {
                Text = "Noch keine Links konfiguriert", Foreground = BrushFromHex("#555555"),
                FontSize = 11, FontStyle = System.Windows.FontStyles.Italic
            });
        }
    }

    private static void OpenUrlInBrowser(string url)
    {
        try
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url, UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Link konnte nicht geöffnet werden: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

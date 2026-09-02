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

/// <summary>Tab 3 Beteiligte: CRUD, Sortierung, Rollen-Liste, JSON-Import (BPM-070 Partial-Split).</summary>
public partial class ProjectEditDialog
{
    private void OnAddParticipant(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSharedOrDefault();
        var p = new ProjectParticipant();
        if (ShowParticipantEditDialog(p, settings, "Beteiligten hinzufügen"))
        {
            p.SortOrder = _participants.Count;
            _participants.Add(p);
        }
    }

    private void OnEditParticipant(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        var settings = _settingsService.LoadSharedOrDefault();
        ShowParticipantEditDialog(p, settings, "Beteiligten bearbeiten");
        DgParticipants.Items.Refresh();
    }

    private void OnRemoveParticipant(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        if (MessageBox.Show($"Beteiligten \"{p.Company}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            _participants.Remove(p);
    }

    private void OnMoveParticipantUp(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        var idx = _participants.IndexOf(p);
        if (idx <= 0) return;
        _participants.RemoveAt(idx);
        _participants.Insert(idx - 1, p);
        DgParticipants.SelectedItem = p;
    }

    private void OnMoveParticipantDown(object sender, RoutedEventArgs e)
    {
        if (DgParticipants.SelectedItem is not ProjectParticipant p) return;
        var idx = _participants.IndexOf(p);
        if (idx < 0 || idx >= _participants.Count - 1) return;
        _participants.RemoveAt(idx);
        _participants.Insert(idx + 1, p);
        DgParticipants.SelectedItem = p;
    }

    private void OnEditParticipantRoles(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSharedOrDefault();
        var items = new ObservableCollection<string>(settings.ParticipantRoles);
        if (ShowSimpleListEditDialog("Rollen bearbeiten", items))
        {
            settings.ParticipantRoles = items.ToList();
            _settingsService.SaveSharedOrDefault(settings);
        }
    }

    private void OnImportParticipantsJson(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON Dateien|*.json",
            Title = "Beteiligte aus JSON importieren"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var json = _fs.ReadAllText(dlg.FileName);
            var imported = System.Text.Json.JsonSerializer.Deserialize<List<ProjectParticipant>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (imported is null || imported.Count == 0)
            {
                MessageBox.Show("Keine Beteiligten in der JSON-Datei gefunden.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach (var p in imported)
            {
                p.Id = string.Empty;
                p.SortOrder = _participants.Count;
                _participants.Add(p);
            }
            MessageBox.Show($"{imported.Count} Beteiligte importiert.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Import: {ex.Message}", "Import", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool ShowParticipantEditDialog(ProjectParticipant p, SharedConfig settings, string title)
    {
        var w = new Window
        {
            Title = title, Width = 450, Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 6; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var cmbRole = new ComboBox { IsEditable = true, ItemsSource = settings.ParticipantRoles, Text = p.Role, Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(cmbRole, 0); Grid.SetColumn(cmbRole, 1);
        var txtCompany = MakeTextBox(p.Company, 1, 1);
        var txtContact = MakeTextBox(p.ContactPerson, 2, 1);
        var txtPhone = MakeTextBox(p.Phone, 3, 1);
        var txtEmail = MakeTextBox(p.Email, 4, 1);

        var btnOk = new Button
        {
            Content = "OK", Width = 80, Padding = new Thickness(0, 5, 0, 5), Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 5); Grid.SetColumn(btnOk, 1);
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        grid.Children.Add(MakeLabel("Rolle:", 0, 0)); grid.Children.Add(cmbRole);
        grid.Children.Add(MakeLabel("Firma:", 1, 0)); grid.Children.Add(txtCompany);
        grid.Children.Add(MakeLabel("Kontaktperson:", 2, 0)); grid.Children.Add(txtContact);
        grid.Children.Add(MakeLabel("Telefon:", 3, 0)); grid.Children.Add(txtPhone);
        grid.Children.Add(MakeLabel("Email:", 4, 0)); grid.Children.Add(txtEmail);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            p.Role = (cmbRole.SelectedItem as string ?? cmbRole.Text).Trim();
            p.Company = txtCompany.Text.Trim();
            p.ContactPerson = txtContact.Text.Trim();
            p.Phone = txtPhone.Text.Trim();
            p.Email = txtEmail.Text.Trim();
            return true;
        }
        return false;
    }
}

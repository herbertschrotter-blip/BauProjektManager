﻿using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace BauProjektManager.Settings.Views;

/// <summary>Tab 2 Bauwerk: die code-erzeugten Dialoge fuer Bauteil, Geschoss (mit/ohne Weiter) und Geschoss-Bezeichnungen (BPM-070 Partial-Split).</summary>
public partial class ProjectEditDialog
{
    private bool ShowPartEditDialog(BuildingPart part, SharedConfig settings, string title)
    {
        var w = new Window
        {
            Title = title,
            Width = 420,
            Height = 240,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        // Dark Theme Styles vom Dialog vererben (ComboBox, TextBox etc.)
        foreach (var key in Resources.Keys)
            w.Resources[key] = Resources[key];
        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var txtShort = MakeTextBox(part.ShortName, 0, 1);
        var txtDesc = MakeTextBox(part.Description, 1, 1);
        var cmbType = new ComboBox { ItemsSource = settings.BuildingTypes, Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(cmbType, 2); Grid.SetColumn(cmbType, 1);
        if (!string.IsNullOrEmpty(part.BuildingType)) cmbType.SelectedItem = part.BuildingType;
        else if (settings.BuildingTypes.Count > 0) cmbType.SelectedIndex = 0;
        var txtZero = MakeTextBox(part.ZeroLevelAbsolute != 0 ? part.ZeroLevelAbsolute.ToString("F2", CultureInfo.InvariantCulture) : "", 3, 1);

        var btnOk = new Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(0, 5, 0, 5),
            Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 4); Grid.SetColumn(btnOk, 1);
        // Kürzel ist Pflicht: es bildet folder_name (ADR-059-Addendum) und das
        // Radial-Segment der Plan-Erfassung — ohne Kürzel entsteht ein leerer
        // Ordner-/Segmentname. Speichern wird daher blockiert (BPM-111.05).
        btnOk.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtShort.Text))
            {
                MessageBox.Show(
                    "Bitte ein Kürzel angeben — es bildet den Ordnernamen und das Segment der Plan-Erfassung.",
                    "Kürzel fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtShort.Focus();
                return;
            }
            w.DialogResult = true; w.Close();
        };

        grid.Children.Add(MakeLabel("Kürzel:", 0, 0)); grid.Children.Add(txtShort);
        grid.Children.Add(MakeLabel("Beschreibung:", 1, 0)); grid.Children.Add(txtDesc);
        grid.Children.Add(MakeLabel("Bauwerkstyp:", 2, 0)); grid.Children.Add(cmbType);
        grid.Children.Add(MakeLabel("± 0,00 abs.:", 3, 0)); grid.Children.Add(txtZero);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            part.ShortName = txtShort.Text.Trim();
            part.Description = txtDesc.Text.Trim();
            part.BuildingType = cmbType.SelectedItem as string ?? "";
            var zeroText = txtZero.Text.Replace(',', '.');
            if (double.TryParse(zeroText, CultureInfo.InvariantCulture, out var z)) part.ZeroLevelAbsolute = z;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Dialog für Geschoss anlegen/bearbeiten — editierbares Dropdown + Höhenwerte.
    /// </summary>
    private bool ShowLevelEditDialog(BuildingLevel level, SharedConfig settings, string title)
    {
        var w = new Window
        {
            Title = title,
            Width = 400,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        foreach (var key in Resources.Keys)
            w.Resources[key] = Resources[key];

        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 6; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Geschoss-Dropdown (editierbar, ShortNames)
        var shortNames = settings.LevelNames.Select(l => l.ShortName).ToList();
        var cmbName = new ComboBox
        {
            IsEditable = true,
            ItemsSource = shortNames,
            Text = level.Name,
            Margin = new Thickness(0, 3, 0, 3)
        };
        StyleComboBoxDark(cmbName);
        Grid.SetRow(cmbName, 0); Grid.SetColumn(cmbName, 1);

        // Beschreibung (auto-filled, aber editierbar)
        var txtDesc = MakeTextBox(level.Description, 1, 1);

        // Beschreibung automatisch aktualisieren bei Geschoss-Änderung
        cmbName.SelectionChanged += (_, _) =>
        {
            var selectedName = cmbName.SelectedItem as string ?? cmbName.Text;
            var autoDesc = BuildingLevel.GetAutoDescription(selectedName, settings.LevelNames);
            if (!string.IsNullOrEmpty(autoDesc)) txtDesc.Text = autoDesc;
        };

        var txtRdok = MakeTextBox(level.Rdok != 0 ? level.Rdok.ToString("F2") : "", 2, 1);
        var txtFbok = MakeTextBox(level.Fbok != 0 ? level.Fbok.ToString("F2") : "", 3, 1);
        var txtRduk = MakeTextBox(level.Rduk.HasValue ? level.Rduk.Value.ToString("F2") : "", 4, 1);

        var btnOk = new Button
        {
            Content = "OK",
            Width = 80,
            Padding = new Thickness(0, 5, 0, 5),
            Margin = new Thickness(0, 10, 0, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(btnOk, 5); Grid.SetColumn(btnOk, 1);
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        grid.Children.Add(MakeLabel("Geschoss:", 0, 0)); grid.Children.Add(cmbName);
        grid.Children.Add(MakeLabel("Beschreibung:", 1, 0)); grid.Children.Add(txtDesc);
        grid.Children.Add(MakeLabel("RDOK:", 2, 0)); grid.Children.Add(txtRdok);
        grid.Children.Add(MakeLabel("FBOK:", 3, 0)); grid.Children.Add(txtFbok);
        grid.Children.Add(MakeLabel("RDUK:", 4, 0)); grid.Children.Add(txtRduk);
        grid.Children.Add(btnOk);
        w.Content = grid;

        if (w.ShowDialog() == true)
        {
            level.Name = (cmbName.SelectedItem as string ?? cmbName.Text).Trim();
            level.Description = string.IsNullOrWhiteSpace(txtDesc.Text)
                ? BuildingLevel.GetAutoDescription(level.Name, settings.LevelNames)
                : txtDesc.Text.Trim();
            if (double.TryParse(txtRdok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rdok)) level.Rdok = rdok;
            if (double.TryParse(txtFbok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var fbok)) level.Fbok = fbok;
            level.Rduk = double.TryParse(txtRduk.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rduk) ? rduk : null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Geschoss-Dialog mit 2 Buttons: "+ Geschoss" (speichern + nächstes) und "Fertig" (speichern + schließen).
    /// Wird von AddLevelsLoop aufgerufen.
    /// </summary>
    private LevelDialogResult ShowLevelEditDialogWithContinue(BuildingLevel level, SharedConfig settings)
    {
        var result = LevelDialogResult.Cancel;

        var w = new Window
        {
            Title = "Geschoss hinzufügen",
            Width = 400,
            Height = 290,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        foreach (var key in Resources.Keys)
            w.Resources[key] = Resources[key];

        var grid = new Grid { Margin = new Thickness(15) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var shortNames = settings.LevelNames.Select(l => l.ShortName).ToList();
        var cmbName = new ComboBox
        {
            IsEditable = true,
            ItemsSource = shortNames,
            Text = level.Name,
            Margin = new Thickness(0, 3, 0, 3)
        };
        Grid.SetRow(cmbName, 0); Grid.SetColumn(cmbName, 1);

        var txtDesc = MakeTextBox(level.Description, 1, 1);
        cmbName.SelectionChanged += (_, _) =>
        {
            var selectedName = cmbName.SelectedItem as string ?? cmbName.Text;
            var autoDesc = BuildingLevel.GetAutoDescription(selectedName, settings.LevelNames);
            if (!string.IsNullOrEmpty(autoDesc)) txtDesc.Text = autoDesc;
        };

        var txtRdok = MakeTextBox("", 2, 1);
        var txtFbok = MakeTextBox("", 3, 1);
        var txtRduk = MakeTextBox("", 4, 1);

        // 2 Buttons: + Geschoss und Fertig
        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(btnPanel, 6); Grid.SetColumn(btnPanel, 0); Grid.SetColumnSpan(btnPanel, 2);

        var btnAddMore = new Button
        {
            Content = "+ Geschoss",
            Padding = new Thickness(12, 5, 12, 5),
            Margin = new Thickness(0, 0, 8, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var btnDone = new Button
        {
            Content = "Fertig",
            Padding = new Thickness(12, 5, 12, 5),
            Background = BrushFromHex("#3C3C3C"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        void ApplyValues()
        {
            level.Name = (cmbName.SelectedItem as string ?? cmbName.Text).Trim();
            level.Description = string.IsNullOrWhiteSpace(txtDesc.Text)
                ? BuildingLevel.GetAutoDescription(level.Name, settings.LevelNames)
                : txtDesc.Text.Trim();
            if (double.TryParse(txtRdok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rdok)) level.Rdok = rdok;
            if (double.TryParse(txtFbok.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var fbok)) level.Fbok = fbok;
            level.Rduk = double.TryParse(txtRduk.Text.Replace(',', '.'), CultureInfo.InvariantCulture, out var rduk) ? rduk : null;
        }

        btnAddMore.Click += (_, _) => { ApplyValues(); result = LevelDialogResult.AddMore; w.DialogResult = true; w.Close(); };
        btnDone.Click += (_, _) => { ApplyValues(); result = LevelDialogResult.Done; w.DialogResult = true; w.Close(); };

        btnPanel.Children.Add(btnAddMore);
        btnPanel.Children.Add(btnDone);

        grid.Children.Add(MakeLabel("Geschoss:", 0, 0)); grid.Children.Add(cmbName);
        grid.Children.Add(MakeLabel("Beschreibung:", 1, 0)); grid.Children.Add(txtDesc);
        grid.Children.Add(MakeLabel("RDOK:", 2, 0)); grid.Children.Add(txtRdok);
        grid.Children.Add(MakeLabel("FBOK:", 3, 0)); grid.Children.Add(txtFbok);
        grid.Children.Add(MakeLabel("RDUK:", 4, 0)); grid.Children.Add(txtRduk);
        grid.Children.Add(btnPanel);
        w.Content = grid;

        return w.ShowDialog() == true ? result : LevelDialogResult.Cancel;
    }

    /// <summary>
    /// ✎ Button: Geschoss-Namensliste bearbeiten (2-spaltig: Kurz + Lang).
    /// </summary>
    private void OnEditLevelNames(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSharedOrDefault();
        var items = new ObservableCollection<LevelNameEntry>(
            settings.LevelNames.Select(l => new LevelNameEntry(l.ShortName, l.LongName)));

        var w = new Window
        {
            Title = "Geschoss-Bezeichnungen bearbeiten",
            Width = 450,
            Height = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };

        var stack = new StackPanel { Margin = new Thickness(15) };

        var dg = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserResizeRows = false,
            Height = 260,
            Background = BrushFromHex("#1E1E1E"),
            Foreground = BrushFromHex("#CCCCCC"),
            BorderBrush = BrushFromHex("#3E3E42"),
            RowBackground = BrushFromHex("#1E1E1E"),
            AlternatingRowBackground = BrushFromHex("#1E1E1E"),
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HorizontalGridLinesBrush = BrushFromHex("#3E3E42"),
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        dg.ColumnHeaderStyle = new Style(typeof(DataGridColumnHeader));
        dg.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, BrushFromHex("#007ACC")));
        dg.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, System.Windows.Media.Brushes.White));
        dg.ColumnHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(6, 4, 6, 4)));
        dg.Columns.Add(new DataGridTextColumn { Header = "Kurzbezeichnung", Binding = new System.Windows.Data.Binding("ShortName"), Width = new DataGridLength(120) });
        dg.Columns.Add(new DataGridTextColumn { Header = "Langbezeichnung", Binding = new System.Windows.Data.Binding("LongName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

        var bp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        var btnAdd = new Button
        {
            Content = "Hinzufügen",
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 0, 5, 0),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnAdd.Click += (_, _) =>
        {
            items.Add(new LevelNameEntry("NEU", "Neues Geschoss"));
            dg.ScrollIntoView(items[^1]);
        };
        var btnRem = new Button
        {
            Content = "Entfernen",
            Padding = new Thickness(10, 4, 10, 4),
            Background = BrushFromHex("#C62828"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnRem.Click += (_, _) => { if (dg.SelectedIndex >= 0) items.RemoveAt(dg.SelectedIndex); };
        bp.Children.Add(btnAdd); bp.Children.Add(btnRem);

        var btnOk = new Button
        {
            Content = "Übernehmen",
            Padding = new Thickness(15, 5, 15, 5),
            Background = BrushFromHex("#007ACC"),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };

        stack.Children.Add(dg);
        stack.Children.Add(bp);
        stack.Children.Add(btnOk);
        w.Content = stack;

        if (w.ShowDialog() == true)
        {
            settings.LevelNames = items.ToList();
            _settingsService.SaveSharedOrDefault(settings);
            ColLevelName.ItemsSource = settings.LevelNames.Select(l => l.ShortName).ToList();
        }
    }
}

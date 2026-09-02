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

/// <summary>Tab 2 Bauwerk: Bauteil-/Geschoss-CRUD, Hoehenberechnung, globales Nullniveau (BPM-070 Partial-Split).</summary>
public partial class ProjectEditDialog
{
    // ═══════════════════════════════════════════
    // TAB 2: BAUWERK
    // ═══════════════════════════════════════════

    private void OnPartSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgParts.SelectedItem is BuildingPart part)
        {
            RecalculateLevels(part);
            DgLevels.ItemsSource = part.Levels;
            TxtLevelsHeader.Text = $"Geschosse: {part.ShortName} ({part.Description})";
            TxtZeroInfo.Text = $"± 0,00 = {part.ZeroLevelAbsolute:F2} m ü.A.";
        }
        else
        {
            DgLevels.ItemsSource = null;
            TxtLevelsHeader.Text = "Geschosse";
            TxtZeroInfo.Text = "";
        }
    }

    private void RecalculateLevels(BuildingPart part)
    {
        var settings = _settingsService.LoadSharedOrDefault();
        var levels = part.Levels;

        int egIndex = levels.FindIndex(l => l.Name.Equals("EG", StringComparison.OrdinalIgnoreCase));
        for (int i = 0; i < levels.Count; i++)
        {
            levels[i].Prefix = egIndex >= 0 ? i - egIndex : i;
            levels[i].Description = BuildingLevel.GetAutoDescription(levels[i].Name, settings.LevelNames);
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (i < levels.Count - 1)
            {
                levels[i].StoryHeight = Math.Round(levels[i + 1].Fbok - levels[i].Fbok, 3);
                levels[i].RawHeight = Math.Round(levels[i + 1].Rdok - levels[i].Rdok, 3);
                // Deckenstärke = RDOK(darüber) − RDUK(aktuell)
                levels[i].DeckThickness = levels[i].Rduk is { } rduk
                    ? Math.Round(levels[i + 1].Rdok - rduk, 3)
                    : null;
            }
            else
            {
                levels[i].StoryHeight = null;
                levels[i].RawHeight = null;
                levels[i].DeckThickness = null;
            }
        }
    }

    private void RefreshLevelsGrid()
    {
        if (DgParts.SelectedItem is BuildingPart part)
        {
            RecalculateLevels(part);
            DgLevels.ItemsSource = null;
            DgLevels.ItemsSource = part.Levels;
        }
    }

    private void OnLevelCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row.Item is not BuildingLevel level) return;

        var colHeader = (e.Column.Header as string) ?? "";

        if (e.EditingElement is TextBox tb && colHeader is "RDOK" or "FBOK" or "RDUK")
        {
            var text = tb.Text.Replace(',', '.');
            if (double.TryParse(text, CultureInfo.InvariantCulture, out var val))
            {
                switch (colHeader)
                {
                    case "RDOK": level.Rdok = val; break;
                    case "FBOK": level.Fbok = val; break;
                    case "RDUK": level.Rduk = val; break;
                }
                tb.Text = val.ToString("F2");
            }
            else if (colHeader == "RDUK" && string.IsNullOrWhiteSpace(tb.Text))
            {
                level.Rduk = null;
            }
        }

        Dispatcher.BeginInvoke(new Action(() => RefreshLevelsGrid()));
    }

    // --- Bauteil CRUD ---

    private void OnAddPart(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.LoadSharedOrDefault();

        bool addMore = true;
        while (addMore)
        {
            var part = new BuildingPart();
            if (!ShowPartEditDialog(part, settings, "Bauteil hinzufügen"))
                break;

            part.SortOrder = _buildingParts.Count;
            _buildingParts.Add(part);
            DgParts.SelectedItem = part;

            // Geschoss-Schleife für dieses Bauteil
            AddLevelsLoop(part, settings);

            // Weiteres Bauteil?
            addMore = ShowDarkConfirm("Weiteres Bauteil anlegen?", "Bauteil");
        }
    }

    /// <summary>
    /// Geschoss-Eingabeschleife: Öffnet den Geschoss-Dialog wiederholt
    /// bis der User "Fertig" wählt.
    /// </summary>
    private void AddLevelsLoop(BuildingPart part, SharedConfig settings)
    {
        bool addMoreLevels = true;
        while (addMoreLevels)
        {
            string suggestedName;
            if (part.Levels.Count == 0)
                suggestedName = settings.LevelNames.Count > 0 ? settings.LevelNames[0].ShortName : "EG";
            else
                suggestedName = BuildingLevel.GetNextLevelName(part.Levels[^1].Name, settings.LevelNames);

            var suggestedDesc = BuildingLevel.GetAutoDescription(suggestedName, settings.LevelNames);
            var level = new BuildingLevel { Name = suggestedName, Description = suggestedDesc };

            var result = ShowLevelEditDialogWithContinue(level, settings);
            if (result == LevelDialogResult.Cancel)
                break;

            level.SortOrder = part.Levels.Count;
            part.Levels.Add(level);
            RefreshLevelsGrid();

            if (result == LevelDialogResult.Done)
                break;

            // result == LevelDialogResult.AddMore → weiter im Loop
        }
    }

    private enum LevelDialogResult { Cancel, Done, AddMore }

    private void OnEditPart(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        var settings = _settingsService.LoadSharedOrDefault();
        ShowPartEditDialog(part, settings, "Bauteil bearbeiten");
        DgParts.Items.Refresh();
        OnPartSelectionChanged(sender, null!);
    }

    private void OnRemovePart(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (MessageBox.Show($"Bauteil \"{part.ShortName}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _buildingParts.Remove(part);
            DgLevels.ItemsSource = null;
            TxtLevelsHeader.Text = "Geschosse";
            TxtZeroInfo.Text = "";
        }
    }

    // --- Geschoss CRUD ---

    /// <summary>
    /// + Geschoss: Öffnet Dialog mit intelligentem Vorschlag für das nächste Geschoss.
    /// </summary>
    private void OnAddLevel(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part)
        {
            MessageBox.Show("Bitte zuerst ein Bauteil auswählen.", "Geschoss", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var settings = _settingsService.LoadSharedOrDefault();

        // Nächstes logisches Geschoss vorschlagen
        string suggestedName;
        if (part.Levels.Count == 0)
            suggestedName = settings.LevelNames.Count > 0 ? settings.LevelNames[0].ShortName : "EG";
        else
            suggestedName = BuildingLevel.GetNextLevelName(part.Levels[^1].Name, settings.LevelNames);

        var suggestedDesc = BuildingLevel.GetAutoDescription(suggestedName, settings.LevelNames);
        var level = new BuildingLevel { Name = suggestedName, Description = suggestedDesc };

        if (ShowLevelEditDialog(level, settings, "Geschoss hinzufügen"))
        {
            level.SortOrder = part.Levels.Count;
            part.Levels.Add(level);
            RefreshLevelsGrid();
        }
    }

    private void OnEditLevel(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        var settings = _settingsService.LoadSharedOrDefault();
        ShowLevelEditDialog(level, settings, "Geschoss bearbeiten");
        RefreshLevelsGrid();
    }

    private void OnRemoveLevel(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        if (MessageBox.Show($"Geschoss \"{level.Name}\" entfernen?", "Entfernen",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            part.Levels.Remove(level);
            RefreshLevelsGrid();
        }
    }

    private void OnMoveLevelUp(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        var idx = part.Levels.IndexOf(level);
        if (idx <= 0) return;
        part.Levels.RemoveAt(idx);
        part.Levels.Insert(idx - 1, level);
        RefreshLevelsGrid();
        DgLevels.SelectedItem = level;
    }

    private void OnMoveLevelDown(object sender, RoutedEventArgs e)
    {
        if (DgParts.SelectedItem is not BuildingPart part) return;
        if (DgLevels.SelectedItem is not BuildingLevel level) return;
        var idx = part.Levels.IndexOf(level);
        if (idx < 0 || idx >= part.Levels.Count - 1) return;
        part.Levels.RemoveAt(idx);
        part.Levels.Insert(idx + 1, level);
        RefreshLevelsGrid();
        DgLevels.SelectedItem = level;
    }

    // ═══════════════════════════════════════════
    // GLOBALES NULLNIVEAU
    // ═══════════════════════════════════════════

    private void OnToggleGlobalZero(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isGlobalZeroActive = !_isGlobalZeroActive;
        UpdateToggleVisual();

        TxtGlobalZero.Visibility = _isGlobalZeroActive ? Visibility.Visible : Visibility.Collapsed;
        TxtGlobalZeroHint.Visibility = _isGlobalZeroActive ? Visibility.Visible : Visibility.Collapsed;

        if (_isGlobalZeroActive && double.TryParse(TxtGlobalZero.Text.Replace(',', '.'),
            CultureInfo.InvariantCulture, out var val))
        {
            foreach (var part in _buildingParts)
                part.ZeroLevelAbsolute = val;
            DgParts.Items.Refresh();
        }
    }

    private void UpdateToggleVisual()
    {
        if (_isGlobalZeroActive)
        {
            ToggleTrack.Background = BrushFromHex("#007ACC");
            ToggleKnob.HorizontalAlignment = HorizontalAlignment.Right;
            ToggleKnob.Margin = new Thickness(0, 0, 2, 0);
        }
        else
        {
            ToggleTrack.Background = BrushFromHex("#555555");
            ToggleKnob.HorizontalAlignment = HorizontalAlignment.Left;
            ToggleKnob.Margin = new Thickness(2, 0, 0, 0);
        }
    }

    private void OnGlobalZeroValueChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isGlobalZeroActive) return;
        if (double.TryParse(TxtGlobalZero.Text.Replace(',', '.'),
            CultureInfo.InvariantCulture, out var val))
        {
            foreach (var part in _buildingParts)
                part.ZeroLevelAbsolute = val;
            DgParts.Items.Refresh();
        }
    }
}

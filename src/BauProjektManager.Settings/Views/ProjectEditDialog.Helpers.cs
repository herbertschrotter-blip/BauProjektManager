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

/// <summary>Gemeinsame UI-Helfer der code-erzeugten Unterdialoge: Eingabe-/Listen-/Bestaetigungsdialog, Brush, Label, TextBox (BPM-070 Partial-Split).</summary>
public partial class ProjectEditDialog
{
    private string ShowSmallInputDialog(string title, string label)
    {
        var w = new Window { Title = title, Width = 350, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize, Background = BrushFromHex("#2D2D30") };
        var stack = new StackPanel { Margin = new Thickness(15) };
        var lbl = new TextBlock { Text = label, Foreground = BrushFromHex("#CCCCCC"), Margin = new Thickness(0, 0, 0, 5) };
        var tb = new TextBox { Background = BrushFromHex("#1E1E1E"), Foreground = BrushFromHex("#CCCCCC"), BorderBrush = BrushFromHex("#3E3E42"), Padding = new Thickness(5, 3, 5, 3), Margin = new Thickness(0, 0, 0, 10) };
        var btn = new Button { Content = "OK", Width = 80, Padding = new Thickness(0, 5, 0, 5), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right };
        btn.Click += (_, _) => { w.DialogResult = true; w.Close(); };
        stack.Children.Add(lbl); stack.Children.Add(tb); stack.Children.Add(btn);
        w.Content = stack; w.ContentRendered += (_, _) => tb.Focus();
        return w.ShowDialog() == true && !string.IsNullOrWhiteSpace(tb.Text) ? tb.Text.Trim() : "";
    }

    /// <summary>
    /// Einfache String-Liste bearbeiten (für Projektarten etc.).
    /// </summary>
    private bool ShowSimpleListEditDialog(string title, ObservableCollection<string> items)
    {
        var w = new Window { Title = title, Width = 350, Height = 380, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize, Background = BrushFromHex("#2D2D30"), SizeToContent = SizeToContent.Height };
        var stack = new StackPanel { Margin = new Thickness(15) };
        var lb = new ListBox { ItemsSource = items, Background = BrushFromHex("#1E1E1E"), Foreground = BrushFromHex("#CCCCCC"), BorderBrush = BrushFromHex("#3E3E42"), Height = 200, Margin = new Thickness(0, 0, 0, 8) };
        var bp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var btnAdd = new Button { Content = "Hinzufügen", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 5, 0), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        btnAdd.Click += (_, _) => { var n = ShowSmallInputDialog("Hinzufügen", "Name:"); if (!string.IsNullOrEmpty(n)) items.Add(n); };
        var btnRem = new Button { Content = "Entfernen", Padding = new Thickness(10, 4, 10, 4), Background = BrushFromHex("#C62828"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        btnRem.Click += (_, _) => { if (lb.SelectedIndex >= 0) items.RemoveAt(lb.SelectedIndex); };
        bp.Children.Add(btnAdd); bp.Children.Add(btnRem);
        var btnOk = new Button { Content = "Übernehmen", Padding = new Thickness(15, 5, 15, 5), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right };
        btnOk.Click += (_, _) => { w.DialogResult = true; w.Close(); };
        stack.Children.Add(lb); stack.Children.Add(bp); stack.Children.Add(btnOk);
        w.Content = stack;
        return w.ShowDialog() == true;
    }

    private static System.Windows.Media.SolidColorBrush BrushFromHex(string hex)
        => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));

    private static TextBlock MakeLabel(string text, int row, int col)
    {
        var tb = new TextBlock { Text = text, Foreground = BrushFromHex("#CCCCCC"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(tb, row); Grid.SetColumn(tb, col);
        return tb;
    }

    private static TextBox MakeTextBox(string text, int row, int col)
    {
        var tb = new TextBox { Text = text, Background = BrushFromHex("#1E1E1E"), Foreground = BrushFromHex("#CCCCCC"), BorderBrush = BrushFromHex("#3E3E42"), Padding = new Thickness(5, 3, 5, 3), Margin = new Thickness(0, 3, 0, 3) };
        Grid.SetRow(tb, row); Grid.SetColumn(tb, col);
        return tb;
    }

    private static void StyleComboBoxDark(ComboBox cmb)
    {
        // Nicht mehr nötig — Styles werden über Window.Resources vererbt
    }

    private bool ShowDarkConfirm(string message, string title)
    {
        var w = new Window
        {
            Title = title, Width = 360, Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this, ResizeMode = ResizeMode.NoResize,
            Background = BrushFromHex("#2D2D30")
        };
        var sp = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
        sp.Children.Add(new TextBlock { Text = message, Foreground = BrushFromHex("#CCCCCC"), FontSize = 14, Margin = new Thickness(0, 0, 0, 16) });
        var bp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        bool result = false;
        var btnYes = new Button { Content = "Ja", Width = 80, Padding = new Thickness(0, 5, 0, 5), Margin = new Thickness(0, 0, 8, 0), Background = BrushFromHex("#007ACC"), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        var btnNo = new Button { Content = "Nein", Width = 80, Padding = new Thickness(0, 5, 0, 5), Background = BrushFromHex("#3C3C3C"), Foreground = BrushFromHex("#CCCCCC"), BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        btnYes.Click += (_, _) => { result = true; w.DialogResult = true; w.Close(); };
        btnNo.Click += (_, _) => { w.DialogResult = false; w.Close(); };
        bp.Children.Add(btnYes); bp.Children.Add(btnNo);
        sp.Children.Add(bp);
        w.Content = sp;
        w.ShowDialog();
        return result;
    }
}

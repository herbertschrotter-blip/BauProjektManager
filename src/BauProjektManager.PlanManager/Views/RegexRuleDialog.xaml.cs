using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

/// <summary>Dialog „Regex-Regel…“ (Seitenliste 3.6 v2.1, BPM-121.00). Logik im <see cref="RegexRuleViewModel"/>.</summary>
public partial class RegexRuleDialog : Window
{
    private readonly RegexRuleViewModel _vm;

    public RegexRuleDialog(RegexRuleViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.PropertyChanged += OnVmChanged;
        Loaded += (_, _) =>
        {
            UpdateTestBrush();
            PatternBox.Focus();
            PatternBox.CaretIndex = PatternBox.Text.Length;
        };
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RegexRuleViewModel.TestSuccess) or nameof(RegexRuleViewModel.TestResult))
            UpdateTestBrush();
    }

    private void UpdateTestBrush()
    {
        var brush = (Brush)FindResource(_vm.TestSuccess ? "BpmSuccess" : "BpmTextSecondary");
        TestNote.BorderBrush = brush;
        TestText.Foreground = brush;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!_vm.Save())
            return;
        DialogResult = true;
        Close();
    }
}

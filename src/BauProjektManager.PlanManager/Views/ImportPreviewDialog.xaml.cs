using System.Windows;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Views;

public partial class ImportPreviewDialog : Window
{
    private readonly ImportPreviewViewModel _vm;

    public ImportPreviewDialog(ImportPreviewViewModel vm)
    {
        // Inherit theme resources from owner
        foreach (var key in Application.Current.MainWindow?.Resources.Keys ?? Array.Empty<object>())
        {
            if (Application.Current.MainWindow?.Resources[key] is { } value)
                Resources[key] = value;
        }

        InitializeComponent();
        _vm = vm;
        DataContext = _vm;

        DecisionGrid.ItemsSource = _vm.Items;
        SummaryText.Text = _vm.SummaryText;
        FooterInfo.Text = $"{_vm.Items.Count} Dateien";
    }

    /// <summary>
    /// True if the user clicked "Import ausführen".
    /// </summary>
    public bool ExecuteRequested { get; private set; }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnExecute(object sender, RoutedEventArgs e)
    {
        ExecuteRequested = true;
        DialogResult = true;
        Close();
    }
}

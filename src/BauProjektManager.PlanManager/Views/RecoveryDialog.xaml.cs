using System.Windows;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Views;

/// <summary>
/// Modal-Dialog der dem User vier Recovery-Optionen anbietet für einen nicht
/// abgeschlossenen Import (siehe BPM-016 / 016.04):
/// - Fortsetzen (Forward) — Empfehlung wenn IsAutomaticAllowed
/// - Rückgängig (Rollback)
/// - Verwerfen (Cleanup)
/// - Später (Cancel — Dialog schließen ohne Aktion)
///
/// SelectedAction ist null wenn User "Später" klickte oder den Dialog schloss.
/// </summary>
public partial class RecoveryDialog : Window
{
    /// <summary>Vom User gewählte Recovery-Aktion. null = "Später" / abgebrochen.</summary>
    public RecoveryAction? SelectedAction { get; private set; }

    public RecoveryDialog(PendingImportInfo info, RecoveryRecommendation recommendation)
    {
        InitializeComponent();

        TxtTimestamp.Text = $"⏱ Gestartet: {info.Timestamp.ToLocalTime():dd.MM.yyyy HH:mm:ss}";
        TxtSource.Text = $"📁 Quelle: {info.SourcePath}";
        TxtMachine.Text = $"💻 Gerät: {info.MachineName ?? "(unbekannt)"}";
        TxtCounts.Text = $"Status: {info.CompletedActions} fertig · {info.PendingActions} ausstehend · {info.FailedActions} fehlgeschlagen · ({info.FileCount} insgesamt)";

        var actionLabel = recommendation.Action switch
        {
            RecoveryAction.Forward => "Empfehlung: Fortsetzen",
            RecoveryAction.Rollback => "Empfehlung: Rückgängig",
            RecoveryAction.Cleanup => "Empfehlung: Verwerfen",
            _ => "Empfehlung: Manuelle Prüfung"
        };

        TxtRecommendationTitle.Text = actionLabel;
        TxtRecommendationReason.Text = recommendation.Reason;

        if (recommendation.IsAutomaticAllowed)
            TxtAutoHint.Visibility = Visibility.Visible;
    }

    private void OnForward(object sender, RoutedEventArgs e)
    {
        SelectedAction = RecoveryAction.Forward;
        DialogResult = true;
        Close();
    }

    private void OnRollback(object sender, RoutedEventArgs e)
    {
        SelectedAction = RecoveryAction.Rollback;
        DialogResult = true;
        Close();
    }

    private void OnCleanup(object sender, RoutedEventArgs e)
    {
        SelectedAction = RecoveryAction.Cleanup;
        DialogResult = true;
        Close();
    }

    private void OnLater(object sender, RoutedEventArgs e)
    {
        SelectedAction = null;
        DialogResult = false;
        Close();
    }
}

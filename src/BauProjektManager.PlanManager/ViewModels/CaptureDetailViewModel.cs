using CommunityToolkit.Mvvm.ComponentModel;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Eine Zeile der Index-Historie im Detail-Panel (BPM-111.06 Slice A).
/// Reine Anzeige — aus plan_revisions gemappt.
/// </summary>
/// <param name="Index">Plan-Index der Revision (oder "Erstausgabe").</param>
/// <param name="Status">Revisions-Status (current / superseded / rejected).</param>
/// <param name="DateText">Formatiertes Datum (current_from), leer wenn unbekannt.</param>
public sealed record PlanRevisionHistoryRow(string Index, string Status, string DateText);

/// <summary>
/// Detail-Sub-ViewModel für die einzeln gewählte "Neue Pläne"-Zeile
/// (BPM-111.06 Slice A, Detail-Panel). In Slice A read-only: zeigt Plandaten,
/// Zielordner, Reason-Hinweis, Index-Historie und steuert "Update übernehmen".
/// Editierbare Felder + Re-Matching folgen in Slice A2/A3.
/// </summary>
public sealed class CaptureDetailViewModel : ObservableObject
{
    public CaptureDetailViewModel(
        CaptureRowViewModel row, IReadOnlyList<PlanRevisionHistoryRow> history)
    {
        Row = row;
        History = history;
    }

    /// <summary>Die zugrundeliegende Zeile — dient als CommandParameter für TakeUpdate.</summary>
    public CaptureRowViewModel Row { get; }

    /// <summary>Index-Historie des zugehörigen Dokuments (leer wenn kein Match).</summary>
    public IReadOnlyList<PlanRevisionHistoryRow> History { get; }

    public string FileName => Row.FileName;

    /// <summary>Zielordner der Pending-Zuordnung, sonst Kandidaten-Hinweis.</summary>
    public string TargetText => Row.PendingTarget ?? Row.CandidateText;

    /// <summary>Plannummer + Index kompakt (Kandidat, sonst bekanntes Dokument).</summary>
    public string PlanIndexText
    {
        get
        {
            var c = Row.Item.Candidates;
            var number = c.PlanNumber ?? Row.Item.Match?.PlanNumber ?? "—";
            var index = c.Index ?? Row.Item.Match?.CurrentIndex ?? "—";
            return $"{number}   ·   Index {index}";
        }
    }

    public string? ReasonText => Row.Reason;
    public bool HasReason => !string.IsNullOrWhiteSpace(Row.Reason);

    /// <summary>"Update übernehmen" nur für Update-Zeilen die noch nicht pending sind.</summary>
    public bool CanTakeUpdate => Row.IsUpdate && !Row.IsPending;

    public bool HasHistory => History.Count > 0;
}

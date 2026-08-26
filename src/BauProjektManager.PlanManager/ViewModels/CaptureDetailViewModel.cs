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
/// (BPM-111.06 Slice A, Detail-Panel): zeigt Plandaten, Zielordner,
/// Reason-Hinweis, Index-Historie und steuert "Update übernehmen".
/// Slice A2: Plannummer/Index editierbar — "Anwenden" löst den Einzel-
/// Re-Match im <see cref="ManualCaptureViewModel"/> aus (Identitätswechsel).
/// </summary>
public sealed partial class CaptureDetailViewModel : ObservableObject
{
    public CaptureDetailViewModel(
        CaptureRowViewModel row, IReadOnlyList<PlanRevisionHistoryRow> history)
    {
        Row = row;
        History = history;
        _editPlanNumber = row.Item.Candidates.PlanNumber ?? row.Item.Match?.PlanNumber ?? string.Empty;
        _editIndex = row.Item.Candidates.Index ?? string.Empty;
    }

    /// <summary>Editierbare Plannummer (Slice A2) — vorbelegt aus Kandidat bzw. Match.</summary>
    [ObservableProperty]
    private string _editPlanNumber;

    /// <summary>Editierbarer Index (Slice A2) — vorbelegt aus dem Kandidaten.</summary>
    [ObservableProperty]
    private string _editIndex;

    /// <summary>Identität editierbar — nicht bei MD5-Dubletten (Bucket A).</summary>
    public bool CanEditIdentity => !Row.IsDuplicate;

    /// <summary>
    /// Bezeichnung editierbar (Slice A3) — nur für Zeilen die ein NEUES Dokument
    /// anlegen (Erstaufnahme/Konflikt). Updates behalten den Titel des bekannten
    /// Dokuments, Dubletten werden nicht importiert.
    /// </summary>
    public bool CanEditTitle => !Row.IsDuplicate && !Row.IsUpdate;

    /// <summary>Read-only-Anzeige statt Edit-Feldern (Dubletten).</summary>
    public bool IsIdentityReadOnly => Row.IsDuplicate;

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

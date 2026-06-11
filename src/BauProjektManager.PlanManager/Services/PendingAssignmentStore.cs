using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// In-Memory-Store fuer Pending Assignments (BPM-111.04, ADR-059).
/// Lebenszyklus = App-Session: bewusst KEINE Persistenz (Entscheidung Teil 43)
/// — kein Stale-Risiko bei extern geaendertem Eingang, Neu-Zuordnen per
/// Geste ist billig. Undo Stufe 1 = <see cref="Discard"/>/<see cref="Clear"/>.
/// Key = relativer Eingangs-Pfad (eine Zuordnung pro Datei, Re-Assign ersetzt).
/// </summary>
public class PendingAssignmentStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, PendingAssignment> _items =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Setzt oder ersetzt die Zuordnung fuer eine Eingangs-Datei.</summary>
    public void Assign(PendingAssignment assignment)
    {
        lock (_lock)
        {
            _items[assignment.File.Scan.RelativePath] = assignment;
        }
        Log.Debug("Pending: {File} -> {Target}",
            assignment.File.Scan.FileName, assignment.TargetRelativeDirectory);
    }

    /// <summary>Undo Stufe 1: einzelne Zuordnung verwerfen. True wenn vorhanden.</summary>
    public bool Discard(string relativePath)
    {
        lock (_lock)
        {
            return _items.Remove(relativePath);
        }
    }

    /// <summary>Undo Stufe 1: alle Zuordnungen verwerfen.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _items.Clear();
        }
        Log.Debug("Pending: alle Zuordnungen verworfen");
    }

    public PendingAssignment? Get(string relativePath)
    {
        lock (_lock)
        {
            return _items.GetValueOrDefault(relativePath);
        }
    }

    /// <summary>Snapshot der aktuellen Zuordnungen (stabil fuer Bestaetigung).</summary>
    public IReadOnlyList<PendingAssignment> Snapshot()
    {
        lock (_lock)
        {
            return [.. _items.Values];
        }
    }

    public int Count
    {
        get { lock (_lock) { return _items.Count; } }
    }
}

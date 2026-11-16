using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>"Mit Claude sortieren" (ADR-067): der Tab stellt Analyse, Pending-Store und Zeilen-Kennzeichen bereit.</summary>
public partial class ManualCaptureViewModel : IClaudeSortHost
{
    private IReadOnlyDictionary<string, ClaudeRowMark> _claudeMarks = new Dictionary<string, ClaudeRowMark>();

    /// <summary>Chatfenster des Tabs; NULL, solange die View es nicht gesetzt hat.</summary>
    public ClaudeSortViewModel? ClaudeSort { get; set; }

    string IClaudeSortHost.ProjectId => _projectId;

    string IClaudeSortHost.ProjectRootPath => _projectRootPath;

    IReadOnlyList<CaptureItem> IClaudeSortHost.CurrentItems => [.. Rows.Select(r => r.Item)];

    PendingAssignmentStore IClaudeSortHost.PendingStore => _pending;

    Task IClaudeSortHost.ReanalyzeAsync() => RefreshAsync();

    void IClaudeSortHost.SetClaudeMarks(IReadOnlyDictionary<string, ClaudeRowMark> marks)
    {
        _claudeMarks = marks;
        ApplyClaudeMarks();
    }

    /// <summary>Kennzeichen auf die (ggf. neu aufgebauten) Zeilen legen.</summary>
    private void ApplyClaudeMarks()
    {
        foreach (var row in Rows)
            row.ClaudeMark = _claudeMarks.TryGetValue(row.RelativePath, out var mark) ? mark : null;
    }
}

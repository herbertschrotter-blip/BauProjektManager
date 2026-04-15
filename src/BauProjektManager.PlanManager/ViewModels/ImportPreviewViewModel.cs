using System.Collections.ObjectModel;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel for the Import Preview dialog.
/// Displays ImportDecisions in a DataGrid with status icons.
/// </summary>
public class ImportPreviewViewModel
{
    public ObservableCollection<ImportPreviewItem> Items { get; }
    public string SummaryText { get; }
    public ImportAnalysisResult AnalysisResult { get; }

    public ImportPreviewViewModel(ImportAnalysisResult result)
    {
        AnalysisResult = result;
        Items = new ObservableCollection<ImportPreviewItem>(
            result.Decisions.Select(d => new ImportPreviewItem(d)));

        var parts = new List<string>();
        if (result.NewCount > 0) parts.Add($"✅ {result.NewCount} Neu");
        if (result.UpdateCount > 0) parts.Add($"🔄 {result.UpdateCount} Update");
        if (result.SkipCount > 0) parts.Add($"⏭️ {result.SkipCount} Identisch");
        if (result.WarningCount > 0) parts.Add($"⚠️ {result.WarningCount} Warnung");
        if (result.UnknownCount > 0) parts.Add($"❓ {result.UnknownCount} Unbekannt");
        if (result.ConflictCount > 0) parts.Add($"🔀 {result.ConflictCount} Konflikt");
        SummaryText = string.Join("  |  ", parts);
    }
}

/// <summary>
/// Single row in the import preview DataGrid.
/// </summary>
public class ImportPreviewItem
{
    public ImportPreviewItem(ImportDecision decision)
    {
        Decision = decision;
        FileName = decision.File.Parsed.FileName;
        Status = decision.Status;
        StatusIcon = GetStatusIcon(decision.Status);
        StatusText = GetStatusText(decision.Status);
        DocumentType = decision.File.DocumentTypeDisplayName ?? "—";
        PlanNumber = decision.File.PlanNumber ?? "—";
        RevisionToken = decision.File.RevisionToken ?? "—";
        DocumentKey = decision.DocumentKey ?? "—";
        TargetPath = decision.TargetRelativePath ?? "—";
        Reason = decision.Reasons.Count > 0 ? decision.Reasons[0] : "";
    }

    public ImportDecision Decision { get; }
    public string FileName { get; }
    public ImportStatus Status { get; }
    public string StatusIcon { get; }
    public string StatusText { get; }
    public string DocumentType { get; }
    public string PlanNumber { get; }
    public string RevisionToken { get; }
    public string DocumentKey { get; }
    public string TargetPath { get; }
    public string Reason { get; }

    public bool IsActionable => Status is not ImportStatus.SkipIdentical
        and not ImportStatus.Unknown and not ImportStatus.Conflict;

    private static string GetStatusIcon(ImportStatus status) => status switch
    {
        ImportStatus.New => "✅",
        ImportStatus.SkipIdentical => "⏭️",
        ImportStatus.UpdateNewerIndex => "🔄",
        ImportStatus.ChangedNoIndex => "🔃",
        ImportStatus.ChangedSameIndex => "⚠️",
        ImportStatus.OlderRevision => "⚠️",
        ImportStatus.LearnIndex => "📝",
        ImportStatus.Unknown => "❓",
        ImportStatus.Conflict => "🔀",
        _ => "•"
    };

    private static string GetStatusText(ImportStatus status) => status switch
    {
        ImportStatus.New => "Neu",
        ImportStatus.SkipIdentical => "Identisch",
        ImportStatus.UpdateNewerIndex => "Neue Revision",
        ImportStatus.ChangedNoIndex => "Geändert",
        ImportStatus.ChangedSameIndex => "Geändert (gleicher Index)",
        ImportStatus.OlderRevision => "Ältere Revision",
        ImportStatus.LearnIndex => "Index erkannt",
        ImportStatus.Unknown => "Unbekannt",
        ImportStatus.Conflict => "Mehrere Profile",
        _ => "—"
    };
}

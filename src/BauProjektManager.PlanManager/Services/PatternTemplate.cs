using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// A reusable pattern template extracted from a RecognitionProfile.
/// Stored globally in pattern-templates.json (Cloud .AppData/).
/// Contains parsing + recognition rules but no project-specific data.
/// </summary>
public class PatternTemplate
{
    public string Id { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;
    public List<string> Delimiters { get; set; } = ["-", "_"];
    public List<ProfileSegment> Segments { get; set; } = [];
    public List<RecognitionRule> Recognition { get; set; } = [];
    public IndexSourceType IndexSource { get; set; } = IndexSourceType.FileName;
    public string IndexMode { get; set; } = "optional";
    public List<string> FolderHierarchy { get; set; } = [];
    public string SourceProjectName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// A reusable pattern template extracted from a RecognitionProfile.
/// Stored globally in pattern-templates.json (Cloud .AppData/).
/// Contains parsing + recognition rules but no project-specific data.
/// Schema v2: uses DocumentTypeId + TokenizationConfig.
/// </summary>
public class PatternTemplate
{
    public string Id { get; set; } = string.Empty;
    public string DocumentTypeId { get; set; } = string.Empty;
    public string DocumentTypeName { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;
    public TokenizationConfig Tokenization { get; set; } = new();
    public List<ProfileSegment> Segments { get; set; } = [];
    public List<RecognitionRule> Recognition { get; set; } = [];
    public IndexSourceType IndexSource { get; set; } = IndexSourceType.FileName;
    public string IndexMode { get; set; } = "optional";
    public IndexExtractionConfig? IndexExtraction { get; set; }
    public List<string> FolderHierarchy { get; set; } = [];
    public string SourceProjectName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

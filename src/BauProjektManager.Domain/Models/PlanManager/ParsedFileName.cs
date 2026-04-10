namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Ergebnis des Dateinamen-Parsings.
/// Enthält den Originalnamen, die Extension und die geparsten Segmente.
/// </summary>
public class ParsedFileName
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string BaseName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public List<FileNameSegment> Segments { get; set; } = [];
    public List<string> UsedDelimiters { get; set; } = [];
}

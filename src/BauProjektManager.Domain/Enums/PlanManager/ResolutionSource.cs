namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Where a classification/resolution field was derived from.
/// Used in ResolutionEvidence to track provenance.
/// </summary>
public enum ResolutionSource
{
    /// <summary>Extracted from the file name (parsing/regex).</summary>
    FileName,
    /// <summary>Derived from the relative folder path.</summary>
    FolderPath,
    /// <summary>Matched by a RecognitionProfile rule.</summary>
    Profile,
    /// <summary>Derived from the file extension.</summary>
    Extension,
    /// <summary>Manually set by the user (e.g. in preview corrections).</summary>
    UserOverride
}

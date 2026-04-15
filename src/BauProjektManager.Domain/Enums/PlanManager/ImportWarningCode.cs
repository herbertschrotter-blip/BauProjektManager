namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Typed warning codes for the import analysis pipeline.
/// Used instead of free strings for consistent filtering and localized UI text.
/// </summary>
public enum ImportWarningCode
{
    MissingFolderContext,
    MultipleProfileMatches,
    MultiplePlanNumbers,
    IndexExtractedFromFallback,
    StageDerivedFromFolder,
    TypeDerivedFromFolder,
    NoIndexDetected,
    AmbiguousRevisionToken,
    DuplicateBaseName,
    SuspiciousMixedDocument
}

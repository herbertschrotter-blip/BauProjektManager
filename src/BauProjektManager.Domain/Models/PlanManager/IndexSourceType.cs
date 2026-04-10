namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Wie der Index (Revision) eines Dokuments erkannt wird.
/// ADR-045: Dreistufiges Modell.
/// </summary>
public enum IndexSourceType
{
    /// <summary>
    /// Index wird aus einem Segment im Dateinamen gelesen (z.B. A, B, C).
    /// </summary>
    FileName,

    /// <summary>
    /// Kein Index. Versionen werden per MD5-Hash erkannt.
    /// </summary>
    None,

    /// <summary>
    /// Index wird aus dem PDF-Plankopf gelesen. Post-V1.
    /// </summary>
    PlanHeader
}

namespace BauProjektManager.Domain.Enums;

/// <summary>
/// Speicher-Scope einer persistierten Datei.
/// Local = LocalAppData (geraetespezifisch, nicht synct).
/// CloudShared = BasePath/.AppData (synct via Cloud-Speicher).
/// ProjectLocal = ProjectRoot/.bpm/ (synct via Cloud, projektspezifisch).
/// </summary>
public enum PersistenceScope
{
    Local,
    CloudShared,
    ProjectLocal
}

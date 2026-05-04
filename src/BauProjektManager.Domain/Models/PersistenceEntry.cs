using BauProjektManager.Domain.Enums;

namespace BauProjektManager.Domain.Models;

/// <summary>
/// Ein registrierter Persistenz-Eintrag (Datei oder Datenbank).
/// Wird in IPersistenceRegistry gehalten und im DevTools-Inventar angezeigt.
/// </summary>
public sealed record PersistenceEntry(
    string DisplayName,
    string AbsolutePath,
    PersistenceType Type,
    PersistenceScope Scope,
    string? Description = null);

using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Dokumenttyp-Stammdaten aus bpm.db (Tabelle document_types,
/// ADR-059-Addendum / DB-SCHEMA Kap. 4.12). Quelle fuer Ring 1 des Radials
/// und das typabhaengige Unterteilungs-Schema.
/// </summary>
/// <param name="Id">ULID, projekt-scoped (Built-ins werden via <paramref name="IsBuiltin"/> + Name identifiziert).</param>
/// <param name="Name">Anzeigename (z. B. "Polierplan").</param>
/// <param name="FolderName">Physischer Ordnername — EINMAL beim Anlegen erzeugt, Praefix bleibt erhalten.</param>
/// <param name="ColorHex">Radial-Segmentfarbe, NULL = Theme-Default.</param>
/// <param name="Ring2Source">Unterteilungs-Schema (raeumlich/kategorial/keins).</param>
/// <param name="SortOrder">Reihenfolge im Ring.</param>
/// <param name="IsBuiltin">True fuer Seed-Typen.</param>
/// <param name="Categories">Typgebundene Kategorien (nur bei Ring2Source=Categories befuellt).</param>
public sealed record PlanDocumentType(
    string Id,
    string Name,
    string FolderName,
    string? ColorHex,
    Ring2Source Ring2Source,
    int SortOrder,
    bool IsBuiltin,
    IReadOnlyList<PlanDocumentTypeCategory> Categories);

/// <summary>
/// Typgebundene Kategorie (bpm.db document_type_categories, Kap. 4.13) —
/// z. B. Protokollart "Baubesprechung" oder Fertigteil-Kategorie "Waende".
/// </summary>
public sealed record PlanDocumentTypeCategory(
    string Id,
    string Name,
    string FolderName,
    int SortOrder);

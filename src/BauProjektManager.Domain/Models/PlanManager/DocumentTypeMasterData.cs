using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Dokumenttyp-Stammdaten aus bpm.db (Tabelle document_types,
/// ADR-059-Addendum / DB-SCHEMA Kap. 4.12). Quelle fuer Ring 1 des Radials
/// und das typabhaengige Unterteilungs-Schema.
/// </summary>
/// <param name="Id">ULID, projekt-scoped (Built-ins werden via <paramref name="IsBuiltin"/> + Name identifiziert).</param>
/// <param name="Name">Anzeigename (z. B. "Polierplan").</param>
/// <param name="FolderName">Physischer Typordner unter dem Root — EINMAL beim Anlegen erzeugt, Praefix bleibt erhalten. LEER bei Root-Typ (ADR-061).</param>
/// <param name="ColorHex">Radial-Segmentfarbe, NULL = Theme-Default.</param>
/// <param name="Ring2Source">Unterteilungs-Schema (raeumlich/kategorial/keins).</param>
/// <param name="SortOrder">Reihenfolge im Ring.</param>
/// <param name="IsBuiltin">True fuer Seed-Typen.</param>
/// <param name="Categories">Typgebundene Kategorien (nur bei Ring2Source=Categories befuellt).</param>
/// <param name="Key">
/// Stabiler Schluessel (ADR-061), != UI-Name, nach Anlage gesperrt. UNIQUE je Projekt.
/// Trailing-Default fuer additive Slice 0.1 — befuellt ab Slice 0.3/0.4 (Seed/DB).
/// </param>
/// <param name="RootRelativePath">
/// Echter Ablage-Root je Typ, relativ zum Projektroot (z. B. "01 Planunterlagen" / "06 Protokolle", ADR-061).
/// Trailing-Default fuer additive Slice 0.1 — befuellt ab Slice 0.3/0.4; DB-seitig NOT NULL / CHECK &lt;&gt; '' (Slice 0.2).
/// </param>
public sealed record PlanDocumentType(
    string Id,
    string Name,
    string FolderName,
    string? ColorHex,
    Ring2Source Ring2Source,
    int SortOrder,
    bool IsBuiltin,
    IReadOnlyList<PlanDocumentTypeCategory> Categories,
    string Key = "",
    string RootRelativePath = "");

/// <summary>
/// Typgebundene Kategorie (bpm.db document_type_categories, Kap. 4.13) —
/// z. B. Protokollart "Baubesprechung" oder Fertigteil-Kategorie "Waende".
/// </summary>
public sealed record PlanDocumentTypeCategory(
    string Id,
    string Name,
    string FolderName,
    int SortOrder);

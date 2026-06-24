using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Erzeugt neue Dokumenttypen zur Laufzeit ("+ Neu…"-Schnellanlage, ADR-061
/// Slice 0.4). Kapselt die Normalisierung (key + folder_name) und die
/// key-Eindeutigkeit je Projekt — bewusst NICHT in der Low-Level-DB-Methode
/// (ProjectDatabase bleibt dumm). Der key wird aus dem Namen abgeleitet und ist
/// nach Anlage gesperrt (es gibt keine Update-key-Methode).
/// </summary>
public class DocumentTypeCreationService
{
    private readonly ProjectDatabase _db;
    private readonly IPlanValueNormalizer _normalizer;

    public DocumentTypeCreationService(ProjectDatabase db, IPlanValueNormalizer normalizer)
    {
        _db = db;
        _normalizer = normalizer;
    }

    /// <summary>
    /// Legt einen Dokumenttyp an und gibt ihn (aus der DB nachgeladen) zurueck.
    /// </summary>
    /// <param name="projectId">Projekt-Scope.</param>
    /// <param name="displayName">Anzeigename (z. B. "Polierplan").</param>
    /// <param name="rootRelativePath">Ablagebereich relativ zum Projektroot (z. B. "01 Planunterlagen").</param>
    /// <param name="ring2Source">Unterteilungs-Schema (Ring 2).</param>
    /// <param name="folderName">Optionaler Typordner; leer -> aus Name abgeleitet.</param>
    /// <param name="colorHex">Optionale Radial-Segmentfarbe; null -> Theme-Default.</param>
    public PlanDocumentType Create(
        string projectId, string displayName, string rootRelativePath,
        Ring2Source ring2Source, string? folderName = null, string? colorHex = null)
    {
        var name = displayName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Typname darf nicht leer sein.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(rootRelativePath))
            throw new ArgumentException("Ablagebereich darf nicht leer sein.", nameof(rootRelativePath));

        var existing = _db.GetDocumentTypes(projectId);
        var key = MakeUniqueKey(name, existing);
        // Ordnername immer normalisieren (gueltiger Windows-Name) — ob abgeleitet oder vorgegeben.
        var folder = _normalizer.NormalizeForFolderName(
            string.IsNullOrWhiteSpace(folderName) ? name : folderName);
        var sortOrder = existing.Count == 0 ? 10 : existing.Max(t => t.SortOrder) + 10;

        var id = _db.InsertDocumentType(
            projectId, name, folder, colorHex, ring2Source, sortOrder,
            isBuiltin: false, id: null, key: key, rootRelativePath: rootRelativePath.Trim());

        Log.Information("Dokumenttyp angelegt: {Key} unter {Root} (Projekt {ProjectId})",
            key, rootRelativePath, projectId);

        return _db.GetDocumentTypes(projectId).First(t => t.Id == id);
    }

    /// <summary>
    /// key aus dem Namen (NormalizeForKey). Bei Kollision im Projekt -2, -3, …
    /// Leerer Normalisat-Fall faellt auf "typ" zurueck.
    /// </summary>
    private string MakeUniqueKey(string name, IReadOnlyList<PlanDocumentType> existing)
    {
        var baseKey = _normalizer.NormalizeForKey(name);
        if (string.IsNullOrEmpty(baseKey))
            baseKey = "typ";

        var taken = existing.Select(t => t.Key)
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!taken.Contains(baseKey))
            return baseKey;

        for (var n = 2; ; n++)
        {
            var candidate = $"{baseKey}-{n}";
            if (!taken.Contains(candidate))
                return candidate;
        }
    }
}

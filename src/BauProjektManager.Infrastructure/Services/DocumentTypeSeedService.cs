using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Seed der Dokumenttyp-Stammdaten je Projekt AUS dem FolderTemplate
/// (ADR-061 Slice 0.4). Ein Template-Node wird Dokumenttyp GENAU DANN, wenn
/// <see cref="FolderTemplateEntry.CreatesDocumentType"/> bzw.
/// <see cref="SubFolderEntry.CreatesDocumentType"/> true ist — keine implizite
/// Ableitung aus Name/Prefix/Position. key, root_relative_path und folder_name
/// stammen aus der Template-Struktur (DB = Ordner-Wahrheit). Idempotent: laeuft
/// nur, wenn das Projekt noch keine Typen hat. Danach ist die DB fuehrend.
/// </summary>
public class DocumentTypeSeedService
{
    private readonly ProjectDatabase _db;

    public DocumentTypeSeedService(ProjectDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// Radial-Segmentfarben je Typ-Key (Mockup-Palette). Praesentation, nicht
    /// Ordner-Wahrheit — daher bewusst hier statt im Template.
    /// </summary>
    private static readonly Dictionary<string, string> _colorByKey = new()
    {
        ["ausschreibungsplan"]    = "#534AB7",
        ["polierplan"]            = "#185FA5",
        ["schalung"]              = "#1F7280",
        ["bewehrung"]             = "#993C1D",
        ["fertigteile"]           = "#6E6E6E",
        ["baustelleneinrichtung"] = "#0F6E56",
        ["protokolle"]            = "#555555",
    };

    /// <summary>
    /// Seedet Dokumenttypen aus dem Template, falls das Projekt noch keine hat.
    /// Ohne explizites Template wird das Default-Template verwendet.
    /// </summary>
    public void EnsureSeeded(string projectId, IReadOnlyList<FolderTemplateEntry>? template = null)
    {
        if (_db.HasDocumentTypes(projectId))
            return;

        var nodes = template ?? AppSettings.GetDefaultFolderTemplate();
        var sortOrder = 0;
        var count = 0;

        for (var i = 0; i < nodes.Count; i++)
        {
            var main = nodes[i];
            var mainFolder = main.GetNumberedName(i); // z. B. "01 Planunterlagen"

            // Hauptordner als Root-Typ: folder_name leer, root = der Hauptordner selbst.
            if (main.CreatesDocumentType)
            {
                sortOrder += 10;
                SeedType(projectId, main.DocumentTypeKey, main.DocumentTypeDisplayName ?? main.Name,
                    main.Ring2Source ?? Ring2Source.None, main.Categories,
                    rootRelativePath: mainFolder, folderName: string.Empty, sortOrder);
                count++;
            }

            // Unterordner-Typen: root = Hauptordner, folder_name = nummerierter Unterordner.
            var subPosition = 0;
            foreach (var sub in main.SubFolders)
            {
                var subFolder = sub.GetDisplayName(subPosition);
                if (sub.CreatesDocumentType)
                {
                    sortOrder += 10;
                    SeedType(projectId, sub.DocumentTypeKey, sub.DocumentTypeDisplayName ?? sub.Name,
                        sub.Ring2Source ?? Ring2Source.None, sub.Categories,
                        rootRelativePath: mainFolder, folderName: subFolder, sortOrder);
                    count++;
                }

                if (sub.HasPrefix)
                    subPosition++;
            }
        }

        Log.Information("DocumentType-Seed: {Count} Typen aus Template fuer Projekt {ProjectId} angelegt",
            count, projectId);
    }

    private void SeedType(string projectId, string? key, string name, Ring2Source ring2,
        List<FolderTemplateCategory> categories, string rootRelativePath, string folderName, int sortOrder)
    {
        var typeKey = key ?? string.Empty;
        var color = _colorByKey.TryGetValue(typeKey, out var c) ? c : null;

        var typeId = _db.InsertDocumentType(
            projectId, name, folderName, color, ring2, sortOrder,
            isBuiltin: true, id: null, key: typeKey, rootRelativePath: rootRelativePath);

        for (var ci = 0; ci < categories.Count; ci++)
        {
            var cat = categories[ci];
            var catFolder = cat.HasPrefix ? $"{ci:D2} {cat.Name}" : cat.Name;
            _db.InsertDocumentTypeCategory(typeId, cat.Name, catFolder, (ci + 1) * 10);
        }
    }
}

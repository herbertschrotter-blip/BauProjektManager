using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using Serilog;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// Seed der Dokumenttyp-Stammdaten je Projekt (ADR-059-Addendum, BPM-111.05).
/// Idempotent: laeuft beim ersten Zugriff der Plan-Erfassung; existierende
/// Typen werden nie ueberschrieben ("Default-Ordnerstruktur ist der Seed der
/// Stammdaten — die DB ist ab dann fuehrend").
/// </summary>
public class DocumentTypeSeedService
{
    private readonly ProjectDatabase _db;

    public DocumentTypeSeedService(ProjectDatabase db)
    {
        _db = db;
    }

    /// <summary>
    /// Built-in-Typen nach ADR-059-Addendum (Farben = Mockup-Palette).
    /// IDs sind ULIDs pro Projekt (document_types ist projekt-scoped) —
    /// Built-ins werden ueber is_builtin + Name identifiziert.
    /// </summary>
    private static readonly (string Name, string Color, Ring2Source Ring2, string[] Categories)[] _builtins =
    [
        ("Polierplan",  "#185FA5", Ring2Source.BuildingParts, []),
        ("Statik",      "#534AB7", Ring2Source.BuildingParts, []),
        ("Bewehrung",   "#993C1D", Ring2Source.BuildingParts, []),
        ("Schalung",    "#1F7280", Ring2Source.BuildingParts, []),
        ("Architektur", "#0F6E56", Ring2Source.BuildingParts, []),
        ("Fertigteile", "#6E6E6E", Ring2Source.Categories,
            ["Wände", "Decken", "Stiegen"]),
        ("Protokolle",  "#555555", Ring2Source.Categories,
            ["Baubesprechung", "Bautagesbericht", "Sicherheit", "Abnahme"])
    ];

    /// <summary>Seedet die Built-in-Typen, falls das Projekt noch keine hat.</summary>
    public void EnsureSeeded(string projectId)
    {
        if (_db.HasDocumentTypes(projectId))
            return;

        for (var i = 0; i < _builtins.Length; i++)
        {
            var (name, color, ring2, categories) = _builtins[i];
            var typeId = _db.InsertDocumentType(
                projectId, name, folderName: null, color, ring2,
                sortOrder: (i + 1) * 10, isBuiltin: true);

            for (var c = 0; c < categories.Length; c++)
                _db.InsertDocumentTypeCategory(typeId, categories[c],
                    folderName: null, sortOrder: (c + 1) * 10);
        }

        Log.Information("DocumentType-Seed: {Count} Built-in-Typen fuer Projekt {ProjectId} angelegt",
            _builtins.Length, projectId);
    }
}

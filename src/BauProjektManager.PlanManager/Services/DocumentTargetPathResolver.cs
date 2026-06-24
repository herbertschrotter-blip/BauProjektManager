using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Eingabe für den Resolver. Jedes Token wird in der Reihenfolge
/// Id → key → normalisierter Name/Ordnername aufgelöst (ADR-061).
/// </summary>
/// <param name="DocumentType">Dokumenttyp (Id, key oder Name).</param>
/// <param name="Ring2">Bauteil bzw. Kategorie (Id/key/Name); null bei Ring2Source.None.</param>
/// <param name="Ring3">Geschoss (Id/Name); optional, nur bei BuildingParts.</param>
/// <param name="FileName">Zieldateiname inkl. Endung.</param>
public sealed record DocumentTargetRequest(
    string DocumentType,
    string? Ring2,
    string? Ring3,
    string FileName);

/// <summary>
/// Aufgelöstes Ablageziel (ADR-061). Fließt durch ImportDecision bis
/// ImportExecutionService (Slice 0.6). Pfade relativ zum Projektroot.
/// </summary>
public sealed record ResolvedDocumentTarget(
    string DocumentTypeId,
    string RelativeDirectory,
    string FileName,
    string RelativePath);

/// <summary>Fail-Fast-Fehler des Resolvers — es wird NIE ein Teilpfad gebaut.</summary>
public sealed class DocumentTargetResolutionException : Exception
{
    public DocumentTargetResolutionException(string message) : base(message) { }
}

/// <summary>
/// Baut den Ablage-Zielpfad eines Plandokuments AUSSCHLIESSLICH aus den
/// DB-Stammdaten + übergebenen Tokens (ADR-061):
/// <c>root_relative_path / folder_name(if!=leer) / Ring2 / Ring3 / fileName</c>.
/// Auflösung je Ebene: Id → key-exact → name/folder_name-normalisiert → Fail.
/// KEIN Fuzzy (gehört in die vorgelagerte Erkennung). Fail-Fast bei fehlendem
/// Pflicht-Ring oder leerem Ordnersegment — niemals ein Teilpfad.
/// </summary>
public sealed class DocumentTargetPathResolver
{
    private readonly ProjectDatabase _db;
    private readonly IPlanValueNormalizer _normalizer;
    private readonly IPathService _path;

    public DocumentTargetPathResolver(
        ProjectDatabase db, IPlanValueNormalizer normalizer, IPathService path)
    {
        _db = db;
        _normalizer = normalizer;
        _path = path;
    }

    /// <summary>Löst das Ablageziel auf oder wirft <see cref="DocumentTargetResolutionException"/>.</summary>
    public ResolvedDocumentTarget Resolve(string projectId, DocumentTargetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw Fail("Dateiname fehlt.");

        var type = ResolveType(_db.GetDocumentTypes(projectId), request.DocumentType)
            ?? throw Fail($"Dokumenttyp nicht auflösbar: '{request.DocumentType}'.");

        if (string.IsNullOrWhiteSpace(type.RootRelativePath))
            throw Fail($"Dokumenttyp '{type.Name}' hat keinen Ablage-Root (root_relative_path leer).");

        var segments = new List<string> { type.RootRelativePath };
        if (!string.IsNullOrEmpty(type.FolderName))
            segments.Add(type.FolderName);

        switch (type.Ring2Source)
        {
            case Ring2Source.None:
                break;

            case Ring2Source.BuildingParts:
                var part = ResolvePart(_db.GetBuildingParts(projectId), RequireRing(request.Ring2, "Bauteil", type.Name))
                    ?? throw Fail($"Bauteil nicht auflösbar: '{request.Ring2}'.");
                segments.Add(RequireFolder(part.FolderName, "Bauteil", part.ShortName));

                if (!string.IsNullOrWhiteSpace(request.Ring3))
                {
                    var level = ResolveLevel(part.Levels, request.Ring3)
                        ?? throw Fail($"Geschoss nicht auflösbar: '{request.Ring3}'.");
                    segments.Add(RequireFolder(level.FolderName, "Geschoss", level.Name));
                }
                break;

            case Ring2Source.Categories:
                var category = ResolveCategory(type.Categories, RequireRing(request.Ring2, "Kategorie", type.Name))
                    ?? throw Fail($"Kategorie nicht auflösbar: '{request.Ring2}'.");
                segments.Add(RequireFolder(category.FolderName, "Kategorie", category.Name));
                break;
        }

        var directory = _path.Combine(segments.ToArray());
        var fullPath = _path.Combine(directory, request.FileName);
        return new ResolvedDocumentTarget(type.Id, directory, request.FileName, fullPath);
    }

    // --- Auflösung je Ebene: Id zuerst, dann key, dann normalisierter Name/Ordnername ---

    private PlanDocumentType? ResolveType(IReadOnlyList<PlanDocumentType> types, string token)
        => types.FirstOrDefault(t => t.Id == token)
        ?? types.FirstOrDefault(t => !string.IsNullOrEmpty(t.Key)
                                     && t.Key.Equals(token, StringComparison.OrdinalIgnoreCase))
        ?? types.FirstOrDefault(t => Matches(t.Name, token) || Matches(t.FolderName, token));

    private BuildingPart? ResolvePart(IReadOnlyList<BuildingPart> parts, string token)
        => parts.FirstOrDefault(p => p.Id == token)
        ?? parts.FirstOrDefault(p => Matches(p.ShortName, token) || Matches(p.FolderName, token));

    private BuildingLevel? ResolveLevel(IReadOnlyList<BuildingLevel> levels, string token)
        => levels.FirstOrDefault(l => l.Id == token)
        ?? levels.FirstOrDefault(l => Matches(l.Name, token) || Matches(l.FolderName, token));

    private PlanDocumentTypeCategory? ResolveCategory(IReadOnlyList<PlanDocumentTypeCategory> categories, string token)
        => categories.FirstOrDefault(c => c.Id == token)
        ?? categories.FirstOrDefault(c => Matches(c.Name, token) || Matches(c.FolderName, token));

    private bool Matches(string value, string token)
        => !string.IsNullOrEmpty(value)
        && _normalizer.NormalizeForMatch(value) == _normalizer.NormalizeForMatch(token);

    private static string RequireRing(string? value, string ring, string typeName)
        => string.IsNullOrWhiteSpace(value)
            ? throw Fail($"{ring} (Ring 2) für Typ '{typeName}' erforderlich, aber nicht angegeben.")
            : value;

    private static string RequireFolder(string folderName, string ring, string display)
        => string.IsNullOrWhiteSpace(folderName)
            ? throw Fail($"{ring} '{display}' hat keinen Ordnernamen (folder_name leer).")
            : folderName;

    private static DocumentTargetResolutionException Fail(string message) => new(message);
}

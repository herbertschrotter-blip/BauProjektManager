using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Manages RecognitionProfiles per project.
/// Profiles are stored as individual JSON files in .bpm/profiles/ (ADR-046).
/// </summary>
/// <remarks>
/// BPM-082: Schema v3, Methoden segment (Default) und regex (Fallback).
/// BPM-108 / ADR-056 (Phase B): Schema v4 — ProfileSegment.FieldTypeId statt FieldType-Enum;
/// IdentityFields/FolderHierarchy/RenameSchema referenzieren segment_types.id bzw. token_key.
/// LoadAll/LoadById verwerfen Profile mit SchemaVersion != 4 (Fruehphase = Reset, kein Migrations-Code).
/// Optionaler <see cref="ISegmentTypeCatalog"/> berechnet beim Laden den
/// <see cref="ProfileHealth"/> und befuellt <see cref="RecognitionProfile.MissingSegmentTypeIds"/>.
/// </remarks>
public class ProfileManager : IProfileManager
{
    /// <summary>Aktuelle Schema-Version. Profile mit anderem Wert werden beim Laden verworfen.</summary>
    public const int CurrentSchemaVersion = 4;

    /// <summary>Reservierter System-Key in <see cref="RecognitionProfile.IdentityFields"/>.</summary>
    public const string DocumentTypeIdentityKey = "documentType";

    private readonly IIdGenerator _idGenerator;
    private readonly IPersistenceRegistry? _persistenceRegistry;
    private readonly ISegmentTypeCatalog? _segmentTypeCatalog;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ProfileManager(
        IIdGenerator idGenerator,
        IPersistenceRegistry? persistenceRegistry = null,
        ISegmentTypeCatalog? segmentTypeCatalog = null)
    {
        _idGenerator = idGenerator;
        _persistenceRegistry = persistenceRegistry;
        _segmentTypeCatalog = segmentTypeCatalog;
    }

    /// <summary>
    /// Returns the .bpm/profiles/ directory path for a project.
    /// Creates the directory if it does not exist.
    /// </summary>
    private static string GetProfilesDirectory(string projectRootPath)
    {
        var dir = Path.Combine(projectRootPath, ".bpm", "profiles");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Loads all profiles for a project from .bpm/profiles/*.json.
    /// </summary>
    /// <remarks>
    /// Schema-Disziplin (BPM-108 Phase B): nur <see cref="CurrentSchemaVersion"/> wird akzeptiert.
    /// Aeltere Profile werden verworfen und im Log dokumentiert — Reset via DevTool-Archive.
    /// Profile mit Missing-IDs (Catalog-Lookup fehlgeschlagen) werden geladen, aber als
    /// <see cref="ProfileHealth.MissingSegmentTypes"/> markiert.
    /// </remarks>
    public List<RecognitionProfile> LoadAll(string projectRootPath)
    {
        var dir = GetProfilesDirectory(projectRootPath);
        var profiles = new List<RecognitionProfile>();

        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<RecognitionProfile>(json, JsonOptions);
                if (profile is null)
                    continue;

                if (profile.SchemaVersion != CurrentSchemaVersion)
                {
                    Log.Error("Profil verworfen: {File} — SchemaVersion {Version}, erwartet {Expected}. Datei loeschen und neu anlegen.",
                        file, profile.SchemaVersion, CurrentSchemaVersion);
                    continue;
                }

                if (!IsProfileLoadable(profile, out var reason))
                {
                    Log.Error("Profil verworfen: {File} — {Reason}", file, reason);
                    continue;
                }

                ComputeHealth(profile);
                profiles.Add(profile);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Profil konnte nicht geladen werden: {File}", file);
            }
        }

        Log.Information("ProfileManager: {Count} Profile geladen aus {Path}",
            profiles.Count, dir);
        return profiles.OrderBy(p => p.DocumentTypeName).ToList();
    }

    /// <summary>
    /// Loads a single profile by ID.
    /// </summary>
    public RecognitionProfile? LoadById(string projectRootPath, string profileId)
    {
        var filePath = Path.Combine(GetProfilesDirectory(projectRootPath),
            $"{profileId}.json");

        if (!File.Exists(filePath))
        {
            Log.Warning("Profil nicht gefunden: {Id}", profileId);
            return null;
        }

        var json = File.ReadAllText(filePath);
        var profile = JsonSerializer.Deserialize<RecognitionProfile>(json, JsonOptions);
        if (profile is null)
            return null;

        if (profile.SchemaVersion != CurrentSchemaVersion)
        {
            Log.Error("Profil verworfen (LoadById): {Id} — SchemaVersion {Version}, erwartet {Expected}.",
                profileId, profile.SchemaVersion, CurrentSchemaVersion);
            return null;
        }

        if (!IsProfileLoadable(profile, out var reason))
        {
            Log.Error("Profil verworfen (LoadById): {Id} — {Reason}", profileId, reason);
            return null;
        }

        ComputeHealth(profile);
        return profile;
    }

    /// <summary>
    /// Profil-Minimum-Validierung (BPM-082.05, Konsens R3 Punkt 12).
    /// Prueft Identitaet (Id, DocumentTypeName), Tokenization-Vorhandensein,
    /// Recognition-Count und alle Rule.IsValid()-Checks.
    /// </summary>
    private static bool IsProfileLoadable(RecognitionProfile profile, out string reason)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            reason = "Id fehlt.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(profile.DocumentTypeName))
        {
            reason = "DocumentTypeName fehlt.";
            return false;
        }
        if (profile.Tokenization is null)
        {
            reason = "Tokenization fehlt.";
            return false;
        }
        if (profile.Recognition.Count == 0)
        {
            reason = "Keine Recognition-Regeln vorhanden.";
            return false;
        }
        foreach (var rule in profile.Recognition)
        {
            if (!rule.IsValid(out reason))
                return false;
        }

        reason = "";
        return true;
    }

    /// <summary>
    /// Berechnet <see cref="RecognitionProfile.Health"/> und befuellt
    /// <see cref="RecognitionProfile.MissingSegmentTypeIds"/>.
    /// </summary>
    /// <remarks>
    /// Ohne <see cref="ISegmentTypeCatalog"/> bleibt Health = Valid (Tests koennen den Catalog
    /// optional injizieren). Mit Catalog werden alle <c>fieldTypeId</c>-Referenzen aus
    /// <c>segments</c>/<c>identityFields</c>/<c>folderHierarchy</c>/<c>renameSchema</c> geprueft.
    /// </remarks>
    private void ComputeHealth(RecognitionProfile profile)
    {
        if (_segmentTypeCatalog is null)
        {
            profile.Health = ProfileHealth.Valid;
            profile.MissingSegmentTypeIds = [];
            return;
        }

        var missing = new HashSet<string>(StringComparer.Ordinal);
        var snapshot = _segmentTypeCatalog.SnapshotIncludingDeleted();

        // segments[].fieldTypeId
        foreach (var seg in profile.Segments)
        {
            if (string.IsNullOrWhiteSpace(seg.FieldTypeId)) continue;
            if (!snapshot.ContainsKey(seg.FieldTypeId)) missing.Add(seg.FieldTypeId);
        }

        // identityFields (ohne DocumentTypeIdentityKey-Systemkey)
        foreach (var id in profile.IdentityFields)
        {
            if (id == DocumentTypeIdentityKey) continue;
            if (!snapshot.ContainsKey(id)) missing.Add(id);
        }

        // folderHierarchy
        foreach (var id in profile.FolderHierarchy)
        {
            if (!snapshot.ContainsKey(id)) missing.Add(id);
        }

        // indexExtraction.segmentSelector
        var selector = profile.IndexExtraction?.SegmentSelector;
        if (!string.IsNullOrWhiteSpace(selector) && !snapshot.ContainsKey(selector))
        {
            missing.Add(selector);
        }

        // renameSchema {token_key}-Platzhalter — token_key liegt auf segment_types
        var renameTokens = ExtractRenameTokens(profile.RenameSchema);
        if (renameTokens.Count > 0)
        {
            var knownTokens = new HashSet<string>(snapshot.Values.Select(t => t.TokenKey), StringComparer.Ordinal);
            foreach (var token in renameTokens)
            {
                if (!knownTokens.Contains(token))
                    missing.Add($"{{{token}}}"); // Markiere als Token-Referenz fuer Diagnose
            }
        }

        profile.MissingSegmentTypeIds = missing.ToList();
        profile.Health = missing.Count == 0 ? ProfileHealth.Valid : ProfileHealth.MissingSegmentTypes;
    }

    /// <summary>
    /// Extrahiert <c>{token}</c>-Platzhalter aus einem Rename-Schema-String.
    /// </summary>
    internal static List<string> ExtractRenameTokens(string renameSchema)
    {
        if (string.IsNullOrWhiteSpace(renameSchema)) return [];
        var tokens = new List<string>();
        var span = renameSchema.AsSpan();
        var i = 0;
        while (i < span.Length)
        {
            if (span[i] == '{')
            {
                var close = span[i..].IndexOf('}');
                if (close > 1)
                {
                    var name = span.Slice(i + 1, close - 1).ToString();
                    if (!string.IsNullOrWhiteSpace(name)) tokens.Add(name);
                    i += close + 1;
                    continue;
                }
            }
            i++;
        }
        return tokens;
    }

    /// <summary>
    /// Saves a profile to .bpm/profiles/{id}.json.
    /// Generates a new ULID if the profile has no ID yet.
    /// </summary>
    public void Save(string projectRootPath, RecognitionProfile profile)
    {
        if (string.IsNullOrEmpty(profile.Id))
            profile.Id = _idGenerator.NewId();

        // Schema bei Save zwingend setzen (Frühphase = Reset, kein Toleranz-Save)
        profile.SchemaVersion = CurrentSchemaVersion;

        var now = DateTime.UtcNow.ToString("o");
        if (string.IsNullOrEmpty(profile.CreatedAt))
            profile.CreatedAt = now;
        profile.UpdatedAt = now;

        var dir = GetProfilesDirectory(projectRootPath);
        var filePath = Path.Combine(dir, $"{profile.Id}.json");

        // Atomic write: temp file → replace
        var tempPath = filePath + ".tmp";
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);

        // BPM-107: registriere bei IPersistenceRegistry
        _persistenceRegistry?.Register(new PersistenceEntry(
            DisplayName: $".bpm/profiles/{profile.DocumentTypeName}",
            AbsolutePath: filePath,
            Type: PersistenceType.Config,
            Scope: PersistenceScope.ProjectLocal,
            Description: $"RecognitionProfile fuer Dokumenttyp '{profile.DocumentTypeName}' (ADR-010)"));

        Log.Information("Profil gespeichert: {Name} ({Id}) → {Path}",
            profile.DocumentTypeName, profile.Id, filePath);
    }

    /// <summary>
    /// Deletes a profile by ID.
    /// </summary>
    public bool Delete(string projectRootPath, string profileId)
    {
        var filePath = Path.Combine(GetProfilesDirectory(projectRootPath),
            $"{profileId}.json");

        if (!File.Exists(filePath))
        {
            Log.Warning("Profil zum Loeschen nicht gefunden: {Id}", profileId);
            return false;
        }

        File.Delete(filePath);
        _persistenceRegistry?.Unregister(filePath);
        Log.Information("Profil geloescht: {Id}", profileId);
        return true;
    }

    /// <summary>
    /// Builds a RecognitionProfile from the current wizard state.
    /// Called by ProfileWizardViewModel.SaveProfile().
    /// </summary>
    /// <remarks>
    /// BPM-108 Phase C: Wizard liefert <see cref="FileNameSegment"/> mit bereits gesetzter
    /// <see cref="FileNameSegment.FieldTypeId"/> (stabile <c>segment_types.id</c>). Pflicht-
    /// und Identity-Logik wird ueber den <see cref="ISegmentTypeCatalog"/> aufgeloest
    /// (<see cref="SegmentSemanticRole.PlanNumber"/> = Pflicht; <see cref="SegmentSemanticRole.PlanNumber"/>
    /// oder <see cref="SegmentSemanticRole.Spatial"/> = identitaetsbildend).
    /// </remarks>
    public RecognitionProfile BuildFromWizard(
        string documentTypeName,
        string targetFolder,
        IndexSourceType indexSource,
        bool indexModeOptional,
        bool indexCaseInsensitive,
        List<FileNameSegment> segments,
        List<string> delimiters,
        List<string> folderHierarchy,
        List<RecognitionRule> recognition,
        int recognitionPriority,
        string? existingProfileId = null,
        string? documentTypeId = null)
    {
        var snapshot = _segmentTypeCatalog?.SnapshotIncludingDeleted();

        var profileSegments = segments
            .Where(s => !string.IsNullOrEmpty(s.FieldTypeId))
            .Select(s =>
            {
                var role = LookupRole(snapshot, s.FieldTypeId);
                return new ProfileSegment
                {
                    Position = s.Position,
                    FieldTypeId = s.FieldTypeId!,
                    Required = role == SegmentSemanticRole.PlanNumber,
                    IncludeInIdentity = role is SegmentSemanticRole.PlanNumber or SegmentSemanticRole.Spatial
                };
            })
            .ToList();

        // Build identityFields (v4): documentType-System-Key + alle identitaetsbildenden Segmente
        var identityFields = new List<string> { DocumentTypeIdentityKey };
        foreach (var seg in profileSegments.Where(s => s.IncludeInIdentity).OrderBy(s => s.Position))
        {
            identityFields.Add(seg.FieldTypeId);
        }

        // BPM-113.06 Slice 0.6b: Bevorzugt die stabile, vom Wizard gewaehlte type.Id
        // (resolverbar via DocumentTargetPathResolver). Fallback auf den normalisierten
        // Namen nur fuer Aufrufer ohne Stammdaten-Auswahl (Tests/Legacy).
        var resolvedDocumentTypeId = string.IsNullOrWhiteSpace(documentTypeId)
            ? NormalizeTypeId(documentTypeName)
            : documentTypeId;

        return new RecognitionProfile
        {
            Id = existingProfileId ?? string.Empty,
            SchemaVersion = CurrentSchemaVersion,
            DocumentTypeId = resolvedDocumentTypeId,
            DocumentTypeName = documentTypeName,
            TargetFolder = targetFolder,
            IndexSource = indexSource,
            IndexMode = indexModeOptional ? "optional" : "required",
            IndexComparison = new IndexComparisonConfig
            {
                Mode = "alphabetic",
                CaseInsensitive = indexCaseInsensitive
            },
            Tokenization = new TokenizationConfig { Delimiters = delimiters },
            Segments = profileSegments,
            IdentityFields = identityFields,
            Recognition = recognition,
            RecognitionPriority = recognitionPriority,
            ConflictPolicy = "askUser",
            Grouping = new GroupingConfig { Mode = "identity" },
            FolderHierarchy = folderHierarchy
        };
    }

    private static SegmentSemanticRole LookupRole(
        IReadOnlyDictionary<string, SegmentTypeDefinition>? snapshot,
        string? fieldTypeId)
    {
        if (snapshot is null || string.IsNullOrEmpty(fieldTypeId)) return SegmentSemanticRole.None;
        return snapshot.TryGetValue(fieldTypeId, out var def)
            ? def.SemanticRole ?? SegmentSemanticRole.None
            : SegmentSemanticRole.None;
    }

    /// <summary>
    /// Normalizes a document type display name to a stable TypeId.
    /// Lowercase, no umlauts, no spaces, no special chars.
    /// </summary>
    internal static string NormalizeTypeId(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "unknown";

        var id = displayName.ToLowerInvariant().Trim();
        id = id.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        id = id.Replace(" ", "_").Replace("-", "_");
        // Remove plural trailing 'e' for common German patterns (Pläne→Plan)
        if (id.EndsWith("plaene"))
            id = id[..^1]; // plaene → plaen... actually just keep it simple
        return id;
    }
}

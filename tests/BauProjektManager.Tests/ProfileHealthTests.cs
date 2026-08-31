using System.IO;
using System.Text.Json;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="ProfileHealth"/>-Berechnung in <see cref="ProfileManager"/>
/// (BPM-108 Phase B). Prueft Missing-ID-Erkennung in segments/identityFields/
/// folderHierarchy/renameSchema/indexExtraction.
/// </summary>
public class ProfileHealthTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _profilesDir;
    private readonly FakeCatalog _catalog;
    private readonly ProfileManager _sut;

    private sealed class StubIdGenerator : IIdGenerator
    {
        public string NewId() => "STUB";
    }

    /// <summary>In-memory ISegmentTypeCatalog mit konfigurierbarer Known-Set.</summary>
    private sealed class FakeCatalog : ISegmentTypeCatalog
    {
        private readonly Dictionary<string, SegmentTypeDefinition> _byId = new();

        public void Add(string id, string tokenKey)
        {
            _byId[id] = new SegmentTypeDefinition { Id = id, TokenKey = tokenKey, Name = id };
        }

        public IReadOnlyList<SegmentTypeDefinition> GetEffectiveActive() => _byId.Values.ToList();
        public SegmentTypeDefinition? GetIncludingDeleted(string id) =>
            _byId.TryGetValue(id, out var def) ? def : null;
        public IReadOnlyDictionary<string, SegmentTypeDefinition> SnapshotIncludingDeleted() => _byId;
        public IReadOnlyList<SegmentTypeGroupDefinition> GetActiveGroups() => [];
        public void Invalidate() { }
        public event EventHandler? Changed { add { } remove { } }
    }

    public ProfileHealthTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "bpm-health-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _profilesDir = Path.Combine(_tempRoot, ".bpm", "profiles");
        Directory.CreateDirectory(_profilesDir);
        _catalog = new FakeCatalog();
        var fs = new LocalFileSystem();
        _sut = new ProfileManager(new StubIdGenerator(), fs, fs, fs,
            persistenceRegistry: null, segmentTypeCatalog: _catalog);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { }
    }

    private void WriteProfile(string fileName, string json)
    {
        File.WriteAllText(Path.Combine(_profilesDir, fileName), json);
    }

    private const string ProfileWithSegment = """
        {
          "schemaVersion": 5,
          "id": "PROF-1",
          "documentTypeId": "test",
          "documentTypeName": "TestType",
          "tokenization": { "delimiters": ["-"] },
          "segments": [
            { "position": 1, "fieldTypeId": "plan_number", "required": true, "includeInIdentity": true }
          ],
          "identityFields": ["documentType", "plan_number"],
          "folderHierarchy": ["geschoss"],
          "renameSchema": "{plan_number}-{plan_index}",
          "indexExtraction": { "source": "segment", "segmentSelector": "plan_number", "pattern": "" },
          "recognition": [ { "method": "segment", "pattern": "P", "segmentPosition": 0 } ]
        }
        """;

    [Fact]
    public void Load_AllSegmentTypesKnown_HealthValid()
    {
        _catalog.Add("plan_number", "plan_number");
        _catalog.Add("plan_index", "plan_index");
        _catalog.Add("geschoss", "geschoss");
        WriteProfile("PROF-1.json", ProfileWithSegment);

        var profile = _sut.LoadById(_tempRoot, "PROF-1");

        Assert.NotNull(profile);
        Assert.Equal(ProfileHealth.Valid, profile!.Health);
        Assert.Empty(profile.MissingSegmentTypeIds);
    }

    [Fact]
    public void Load_MissingFieldTypeInSegments_HealthMissingSegmentTypes()
    {
        _catalog.Add("plan_index", "plan_index");
        _catalog.Add("geschoss", "geschoss");
        // plan_number absichtlich NICHT im Catalog
        WriteProfile("PROF-1.json", ProfileWithSegment);

        var profile = _sut.LoadById(_tempRoot, "PROF-1");

        Assert.NotNull(profile);
        Assert.Equal(ProfileHealth.MissingSegmentTypes, profile!.Health);
        Assert.Contains("plan_number", profile.MissingSegmentTypeIds);
    }

    [Fact]
    public void Load_MissingFieldTypeInFolderHierarchy_HealthMissingSegmentTypes()
    {
        _catalog.Add("plan_number", "plan_number");
        _catalog.Add("plan_index", "plan_index");
        // geschoss fehlt
        WriteProfile("PROF-1.json", ProfileWithSegment);

        var profile = _sut.LoadById(_tempRoot, "PROF-1");

        Assert.NotNull(profile);
        Assert.Equal(ProfileHealth.MissingSegmentTypes, profile!.Health);
        Assert.Contains("geschoss", profile.MissingSegmentTypeIds);
    }

    [Fact]
    public void Load_MissingFieldTypeInRenameSchema_HealthMissingSegmentTypes()
    {
        _catalog.Add("plan_number", "plan_number");
        // plan_index fehlt im Catalog → {plan_index} im RenameSchema referenziert unbekannten Token
        _catalog.Add("geschoss", "geschoss");
        WriteProfile("PROF-1.json", ProfileWithSegment);

        var profile = _sut.LoadById(_tempRoot, "PROF-1");

        Assert.NotNull(profile);
        Assert.Equal(ProfileHealth.MissingSegmentTypes, profile!.Health);
        // RenameSchema-Token wird als {token}-Diagnose markiert
        Assert.Contains("{plan_index}", profile.MissingSegmentTypeIds);
    }

    [Fact]
    public void Load_DocumentTypeIdentityKey_NotCheckedAsSegmentType()
    {
        // documentType ist reservierter System-Key, kein Segmenttyp → kein Missing
        _catalog.Add("plan_number", "plan_number");
        _catalog.Add("plan_index", "plan_index");
        _catalog.Add("geschoss", "geschoss");
        WriteProfile("PROF-1.json", ProfileWithSegment);

        var profile = _sut.LoadById(_tempRoot, "PROF-1");

        Assert.NotNull(profile);
        Assert.DoesNotContain("documentType", profile!.MissingSegmentTypeIds);
    }

    [Fact]
    public void Load_WithoutCatalog_HealthAlwaysValid()
    {
        // ProfileManager ohne Catalog → keine Health-Berechnung
        var fs2 = new LocalFileSystem();
        var managerOhneCatalog = new ProfileManager(new StubIdGenerator(), fs2, fs2, fs2);
        WriteProfile("PROF-1.json", ProfileWithSegment);

        var profile = managerOhneCatalog.LoadById(_tempRoot, "PROF-1");

        Assert.NotNull(profile);
        Assert.Equal(ProfileHealth.Valid, profile!.Health);
        Assert.Empty(profile.MissingSegmentTypeIds);
    }

    [Fact]
    public void Load_SchemaVersion3_IsDiscarded()
    {
        // BPM-108: alte v3-Profile werden vom Loader strikt verworfen
        var v3Profile = ProfileWithSegment.Replace("\"schemaVersion\": 5", "\"schemaVersion\": 3");
        WriteProfile("v3.json", v3Profile);

        var profile = _sut.LoadById(_tempRoot, "PROF-1");

        Assert.Null(profile);
    }

    [Fact]
    public void ExtractRenameTokens_ParsesPlaceholders()
    {
        var tokens = ProfileManager.ExtractRenameTokens("{plan_number}-{plan_index}_{geschoss}");

        Assert.Equal(["plan_number", "plan_index", "geschoss"], tokens);
    }

    [Fact]
    public void ExtractRenameTokens_EmptyOrNoBraces_ReturnsEmpty()
    {
        Assert.Empty(ProfileManager.ExtractRenameTokens(""));
        Assert.Empty(ProfileManager.ExtractRenameTokens("plain text"));
    }
}

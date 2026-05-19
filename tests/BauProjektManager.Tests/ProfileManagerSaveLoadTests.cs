using System.IO;
using System.Text.Json;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="ProfileManager.Save"/>, <see cref="ProfileManager.LoadAll"/>
/// und <see cref="ProfileManager.BuildFromWizard"/> (BPM-082.06b).
///
/// Schwerpunkt: Save-Roundtrip, Profil-JSON-Format mit SchemaVersion=4 (BPM-108 Phase B),
/// segment-Rules korrekt geschrieben+gelesen, BuildFromWizard-Output.
///
/// Pro Test ein frisches Temp-Verzeichnis (IDisposable Cleanup).
/// </summary>
public class ProfileManagerSaveLoadTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly ProfileManager _sut;

    private sealed class FixedIdGenerator(string id) : IIdGenerator
    {
        private readonly string _id = id;
        public string NewId() => _id;
    }

    public ProfileManagerSaveLoadTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "bpm-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _sut = new ProfileManager(new FixedIdGenerator("TEST-ID-01"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch { /* ignore cleanup races */ }
    }

    // === BuildFromWizard ===

    [Fact]
    public void BuildFromWizard_SetsSchemaVersion4()
    {
        var profile = _sut.BuildFromWizard(
            documentTypeName: "Bauprotokoll",
            targetFolder: "01 Planunterlagen",
            indexSource: IndexSourceType.FileName,
            indexModeOptional: true,
            indexCaseInsensitive: true,
            segments: [],
            delimiters: ["-", "_"],
            folderHierarchy: [],
            recognition: [],
            recognitionPriority: 100);

        Assert.Equal(4, profile.SchemaVersion);
    }

    [Fact]
    public void BuildFromWizard_PreservesRecognitionRulesAsGiven()
    {
        var rules = new List<RecognitionRule>
        {
            new() { Method = "segment", Pattern = "PROT", SegmentPosition = 1 }
        };

        var profile = _sut.BuildFromWizard(
            documentTypeName: "Bauprotokoll",
            targetFolder: "01 Planunterlagen",
            indexSource: IndexSourceType.FileName,
            indexModeOptional: true,
            indexCaseInsensitive: true,
            segments: [],
            delimiters: ["-", "_"],
            folderHierarchy: [],
            recognition: rules,
            recognitionPriority: 100);

        Assert.Single(profile.Recognition);
        Assert.Equal("segment", profile.Recognition[0].Method);
        Assert.Equal("PROT", profile.Recognition[0].Pattern);
        Assert.Equal(1, profile.Recognition[0].SegmentPosition);
    }

    [Fact]
    public void BuildFromWizard_NormalizesDocumentTypeId()
    {
        var profile = _sut.BuildFromWizard(
            documentTypeName: "Bauprotokoll",
            targetFolder: "01 Planunterlagen",
            indexSource: IndexSourceType.FileName,
            indexModeOptional: true,
            indexCaseInsensitive: true,
            segments: [],
            delimiters: ["-", "_"],
            folderHierarchy: [],
            recognition: [],
            recognitionPriority: 100);

        Assert.Equal("bauprotokoll", profile.DocumentTypeId);
        Assert.Equal("Bauprotokoll", profile.DocumentTypeName);
    }

    [Fact]
    public void BuildFromWizard_KeepsExistingProfileId()
    {
        var profile = _sut.BuildFromWizard(
            documentTypeName: "X",
            targetFolder: "X",
            indexSource: IndexSourceType.FileName,
            indexModeOptional: true,
            indexCaseInsensitive: true,
            segments: [],
            delimiters: [],
            folderHierarchy: [],
            recognition: [],
            recognitionPriority: 100,
            existingProfileId: "KEEP-ME-42");

        Assert.Equal("KEEP-ME-42", profile.Id);
    }

    [Fact]
    public void BuildFromWizard_NoExistingId_ResultIsEmpty_SaveAssignsLater()
    {
        var profile = _sut.BuildFromWizard(
            documentTypeName: "X",
            targetFolder: "X",
            indexSource: IndexSourceType.FileName,
            indexModeOptional: true,
            indexCaseInsensitive: true,
            segments: [],
            delimiters: [],
            folderHierarchy: [],
            recognition: [],
            recognitionPriority: 100);

        Assert.Equal(string.Empty, profile.Id);
    }

    [Fact]
    public void BuildFromWizard_TokenizationConfigUsesGivenDelimiters()
    {
        var profile = _sut.BuildFromWizard(
            documentTypeName: "X",
            targetFolder: "X",
            indexSource: IndexSourceType.FileName,
            indexModeOptional: true,
            indexCaseInsensitive: true,
            segments: [],
            delimiters: ["-", "_", "."],
            folderHierarchy: [],
            recognition: [],
            recognitionPriority: 100);

        Assert.NotNull(profile.Tokenization);
        Assert.Equal(["-", "_", "."], profile.Tokenization.Delimiters);
    }

    // === Save → Disk ===

    [Fact]
    public void Save_NewProfile_GeneratesIdAndWritesFile()
    {
        var profile = MakeMinimalProfile(id: "");

        _sut.Save(_tempRoot, profile);

        Assert.Equal("TEST-ID-01", profile.Id);
        var expectedPath = Path.Combine(_tempRoot, ".bpm", "profiles", "TEST-ID-01.json");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public void Save_KeepsExistingId()
    {
        var profile = MakeMinimalProfile(id: "ABC-123");

        _sut.Save(_tempRoot, profile);

        Assert.Equal("ABC-123", profile.Id);
        var expectedPath = Path.Combine(_tempRoot, ".bpm", "profiles", "ABC-123.json");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public void Save_SetsCreatedAtOnFirstSave()
    {
        var profile = MakeMinimalProfile(id: "ABC");

        _sut.Save(_tempRoot, profile);

        Assert.False(string.IsNullOrEmpty(profile.CreatedAt));
        Assert.False(string.IsNullOrEmpty(profile.UpdatedAt));
    }

    [Fact]
    public void Save_KeepsCreatedAt_UpdatesUpdatedAt()
    {
        var profile = MakeMinimalProfile(id: "ABC");
        profile.CreatedAt = "2020-01-01T00:00:00Z";

        _sut.Save(_tempRoot, profile);

        Assert.Equal("2020-01-01T00:00:00Z", profile.CreatedAt);
        Assert.NotEqual(profile.CreatedAt, profile.UpdatedAt);
    }

    [Fact]
    public void Save_WritesValidJson_WithSchemaVersion4()
    {
        var profile = MakeMinimalProfile(id: "ABC");
        profile.SchemaVersion = 4;

        _sut.Save(_tempRoot, profile);

        var json = File.ReadAllText(
            Path.Combine(_tempRoot, ".bpm", "profiles", "ABC.json"));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(4, doc.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public void Save_WritesSegmentRuleWithPositionAndPattern()
    {
        var profile = MakeMinimalProfile(id: "ABC");
        profile.Recognition.Clear();
        profile.Recognition.Add(new RecognitionRule
        {
            Method = "segment",
            Pattern = "PROT",
            SegmentPosition = 1
        });

        _sut.Save(_tempRoot, profile);

        var json = File.ReadAllText(
            Path.Combine(_tempRoot, ".bpm", "profiles", "ABC.json"));
        using var doc = JsonDocument.Parse(json);
        var rule = doc.RootElement.GetProperty("recognition")[0];
        Assert.Equal("segment", rule.GetProperty("method").GetString());
        Assert.Equal("PROT", rule.GetProperty("pattern").GetString());
        Assert.Equal(1, rule.GetProperty("segmentPosition").GetInt32());
    }

    // === Save → LoadById Roundtrip ===

    [Fact]
    public void SaveLoadById_Roundtrip_PreservesRecognitionRules()
    {
        var profile = MakeMinimalProfile(id: "ROUND-1");
        profile.Recognition.Clear();
        profile.Recognition.Add(new RecognitionRule
        {
            Method = "segment",
            Pattern = "PROT",
            SegmentPosition = 1
        });
        profile.Recognition.Add(new RecognitionRule
        {
            Method = "segment",
            Pattern = "EG",
            SegmentPosition = 3
        });

        _sut.Save(_tempRoot, profile);
        var loaded = _sut.LoadById(_tempRoot, "ROUND-1");

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Recognition.Count);
        Assert.Equal("segment", loaded.Recognition[0].Method);
        Assert.Equal("PROT", loaded.Recognition[0].Pattern);
        Assert.Equal(1, loaded.Recognition[0].SegmentPosition);
        Assert.Equal("EG", loaded.Recognition[1].Pattern);
        Assert.Equal(3, loaded.Recognition[1].SegmentPosition);
    }

    [Fact]
    public void SaveLoadById_Roundtrip_PreservesSchemaVersion()
    {
        var profile = MakeMinimalProfile(id: "ROUND-2");
        profile.SchemaVersion = 4;

        _sut.Save(_tempRoot, profile);
        var loaded = _sut.LoadById(_tempRoot, "ROUND-2");

        Assert.NotNull(loaded);
        Assert.Equal(4, loaded!.SchemaVersion);
    }

    [Fact]
    public void LoadById_NonExistent_ReturnsNull()
    {
        var loaded = _sut.LoadById(_tempRoot, "DOES-NOT-EXIST");

        Assert.Null(loaded);
    }

    // === LoadAll ===

    [Fact]
    public void LoadAll_ReturnsAllSavedProfiles()
    {
        var p1 = MakeMinimalProfile(id: "P1");
        p1.DocumentTypeName = "Polierplan";
        var p2 = MakeMinimalProfile(id: "P2");
        p2.DocumentTypeName = "Bauprotokoll";

        _sut.Save(_tempRoot, p1);
        _sut.Save(_tempRoot, p2);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Equal(2, all.Count);
        Assert.Contains(all, p => p.Id == "P1");
        Assert.Contains(all, p => p.Id == "P2");
    }

    [Fact]
    public void LoadAll_ReturnsSortedByDocumentTypeName()
    {
        var p1 = MakeMinimalProfile(id: "P1");
        p1.DocumentTypeName = "Polierplan";
        var p2 = MakeMinimalProfile(id: "P2");
        p2.DocumentTypeName = "Bauprotokoll";

        _sut.Save(_tempRoot, p1);
        _sut.Save(_tempRoot, p2);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Equal("Bauprotokoll", all[0].DocumentTypeName);
        Assert.Equal("Polierplan", all[1].DocumentTypeName);
    }

    [Fact]
    public void LoadAll_EmptyDirectory_ReturnsEmptyList()
    {
        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    // === Delete ===

    [Fact]
    public void Delete_ExistingProfile_ReturnsTrueAndRemovesFile()
    {
        var profile = MakeMinimalProfile(id: "DEL-1");
        _sut.Save(_tempRoot, profile);
        var path = Path.Combine(_tempRoot, ".bpm", "profiles", "DEL-1.json");
        Assert.True(File.Exists(path));

        var result = _sut.Delete(_tempRoot, "DEL-1");

        Assert.True(result);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Delete_NonExistent_ReturnsFalse()
    {
        var result = _sut.Delete(_tempRoot, "NOT-THERE");

        Assert.False(result);
    }

    // === Helper ===

    private static RecognitionProfile MakeMinimalProfile(string id)
    {
        return new RecognitionProfile
        {
            Id = id,
            SchemaVersion = 4,
            DocumentTypeId = "test",
            DocumentTypeName = "TestType",
            TargetFolder = "01 Planunterlagen",
            Tokenization = new TokenizationConfig { Delimiters = ["-", "_"] },
            Recognition =
            {
                new RecognitionRule
                {
                    Method = "segment",
                    Pattern = "X",
                    SegmentPosition = 0
                }
            },
            RecognitionPriority = 100
        };
    }
}

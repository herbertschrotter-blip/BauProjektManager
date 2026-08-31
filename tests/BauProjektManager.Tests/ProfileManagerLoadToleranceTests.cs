using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für die Load-Toleranz des <see cref="ProfileManager"/> (BPM-082.06c).
///
/// IsProfileLoadable verwirft Profile mit fehlender Identitaet, fehlender
/// Tokenization, leerer Recognition oder ungueltigen Rules. Andere Profile
/// im selben Verzeichnis bleiben erhalten (Konsens R3 Punkt 10+12).
///
/// Stresst auch defekte JSONs, leere Files, fremde Files im profiles-Dir.
/// </summary>
public class ProfileManagerLoadToleranceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _profilesDir;
    private readonly ProfileManager _sut;

    private sealed class StubIdGenerator : IIdGenerator
    {
        public string NewId() => "STUB-ID";
    }

    public ProfileManagerLoadToleranceTests()
    {
        _tempRoot = Path.Combine(
            Path.GetTempPath(),
            "bpm-tol-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _profilesDir = Path.Combine(_tempRoot, ".bpm", "profiles");
        Directory.CreateDirectory(_profilesDir);
        var fs = new LocalFileSystem();
        _sut = new ProfileManager(new StubIdGenerator(), fs, fs, fs);
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

    private void WriteProfile(string fileName, string json)
    {
        File.WriteAllText(Path.Combine(_profilesDir, fileName), json);
    }

    private const string ValidProfile = """
        {
          "schemaVersion": 5,
          "id": "VALID-1",
          "documentTypeId": "test",
          "documentTypeName": "TestType",
          "tokenization": { "delimiters": ["-", "_"] },
          "recognition": [
            { "method": "segment", "pattern": "PROT", "segmentPosition": 1 }
          ],
          "recognitionPriority": 100
        }
        """;

    // === Valides Profil bleibt erhalten ===

    [Fact]
    public void LoadAll_ValidProfile_IsLoaded()
    {
        WriteProfile("valid.json", ValidProfile);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Single(all);
        Assert.Equal("VALID-1", all[0].Id);
    }

    // === Profil ohne Id wird verworfen ===

    [Fact]
    public void LoadAll_ProfileWithoutId_IsDiscarded()
    {
        const string noId = """
            {
              "schemaVersion": 5,
              "id": "",
              "documentTypeName": "TestType",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "segment", "pattern": "X", "segmentPosition": 0 }
              ]
            }
            """;
        WriteProfile("no-id.json", noId);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    [Fact]
    public void LoadById_ProfileWithoutId_ReturnsNull()
    {
        const string noId = """
            {
              "schemaVersion": 5,
              "id": "",
              "documentTypeName": "TestType",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "segment", "pattern": "X", "segmentPosition": 0 }
              ]
            }
            """;
        // Datei heisst trotzdem so wie ein ID-File
        WriteProfile("FAKE-ID.json", noId);

        var loaded = _sut.LoadById(_tempRoot, "FAKE-ID");

        Assert.Null(loaded);
    }

    // === Profil ohne DocumentTypeName wird verworfen ===

    [Fact]
    public void LoadAll_ProfileWithoutDocumentTypeName_IsDiscarded()
    {
        const string noName = """
            {
              "schemaVersion": 5,
              "id": "X",
              "documentTypeName": "",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "segment", "pattern": "X", "segmentPosition": 0 }
              ]
            }
            """;
        WriteProfile("no-name.json", noName);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    // === Profil ohne Recognition wird verworfen ===

    [Fact]
    public void LoadAll_ProfileWithEmptyRecognition_IsDiscarded()
    {
        const string noRules = """
            {
              "schemaVersion": 5,
              "id": "X",
              "documentTypeName": "TestType",
              "tokenization": { "delimiters": ["-"] },
              "recognition": []
            }
            """;
        WriteProfile("no-rules.json", noRules);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    // === Profil mit invalider Rule (Legacy contains) wird verworfen ===

    [Fact]
    public void LoadAll_ProfileWithLegacyContainsRule_IsDiscarded()
    {
        const string legacyContains = """
            {
              "schemaVersion": 5,
              "id": "LEG-1",
              "documentTypeName": "LegacyType",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "contains", "pattern": "PROT" }
              ]
            }
            """;
        WriteProfile("legacy.json", legacyContains);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    [Fact]
    public void LoadAll_ProfileWithLegacyPrefixRule_IsDiscarded()
    {
        const string legacyPrefix = """
            {
              "schemaVersion": 5,
              "id": "LEG-2",
              "documentTypeName": "LegacyType",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "prefix", "pattern": "PP" }
              ]
            }
            """;
        WriteProfile("legacy-prefix.json", legacyPrefix);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    // === Profil mit segment-Rule ohne SegmentPosition wird verworfen ===

    [Fact]
    public void LoadAll_SegmentRuleWithoutPosition_IsDiscarded()
    {
        const string noPos = """
            {
              "schemaVersion": 5,
              "id": "X",
              "documentTypeName": "TestType",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "segment", "pattern": "PROT" }
              ]
            }
            """;
        WriteProfile("seg-no-pos.json", noPos);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    // === Profil mit leerem Pattern wird verworfen ===

    [Fact]
    public void LoadAll_RuleWithEmptyPattern_IsDiscarded()
    {
        const string noPattern = """
            {
              "schemaVersion": 5,
              "id": "X",
              "documentTypeName": "TestType",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "segment", "pattern": "", "segmentPosition": 1 }
              ]
            }
            """;
        WriteProfile("empty-pattern.json", noPattern);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    // === Mehrere Profile, eines invalid — die anderen bleiben ===

    [Fact]
    public void LoadAll_MixedValidAndInvalid_OnlyValidAreReturned()
    {
        WriteProfile("valid.json", ValidProfile);
        WriteProfile("bad-no-rules.json", """
            {
              "schemaVersion": 5,
              "id": "BAD",
              "documentTypeName": "Bad",
              "tokenization": { "delimiters": ["-"] },
              "recognition": []
            }
            """);
        WriteProfile("bad-legacy.json", """
            {
              "schemaVersion": 5,
              "id": "BAD2",
              "documentTypeName": "Bad2",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "contains", "pattern": "X" }
              ]
            }
            """);

        var all = _sut.LoadAll(_tempRoot);

        Assert.Single(all);
        Assert.Equal("VALID-1", all[0].Id);
    }

    // === Defekte JSON-Dateien ===

    [Fact]
    public void LoadAll_MalformedJson_IsSkipped_OtherProfilesRemain()
    {
        WriteProfile("valid.json", ValidProfile);
        WriteProfile("broken.json", "{ this is not valid json");

        var all = _sut.LoadAll(_tempRoot);

        Assert.Single(all);
        Assert.Equal("VALID-1", all[0].Id);
    }

    [Fact]
    public void LoadAll_EmptyJsonFile_IsSkipped()
    {
        WriteProfile("valid.json", ValidProfile);
        WriteProfile("empty.json", "");

        var all = _sut.LoadAll(_tempRoot);

        Assert.Single(all);
        Assert.Equal("VALID-1", all[0].Id);
    }

    [Fact]
    public void LoadAll_NullDeserializedJson_IsSkipped()
    {
        WriteProfile("valid.json", ValidProfile);
        WriteProfile("null.json", "null");

        var all = _sut.LoadAll(_tempRoot);

        Assert.Single(all);
    }

    // === Verzeichnis existiert nicht / leer ===

    [Fact]
    public void LoadAll_EmptyDirectory_ReturnsEmpty()
    {
        var all = _sut.LoadAll(_tempRoot);

        Assert.Empty(all);
    }

    [Fact]
    public void LoadAll_NonExistentProjectRoot_CreatesDirAndReturnsEmpty()
    {
        var freshRoot = Path.Combine(_tempRoot, "fresh-project");

        var all = _sut.LoadAll(freshRoot);

        Assert.Empty(all);
        Assert.True(Directory.Exists(
            Path.Combine(freshRoot, ".bpm", "profiles")));
    }

    // === Nicht-JSON-Files werden ignoriert ===

    [Fact]
    public void LoadAll_NonJsonFilesAreIgnored()
    {
        WriteProfile("valid.json", ValidProfile);
        File.WriteAllText(Path.Combine(_profilesDir, "notes.txt"), "irrelevant");
        File.WriteAllText(Path.Combine(_profilesDir, "data.xml"), "<x/>");

        var all = _sut.LoadAll(_tempRoot);

        Assert.Single(all);
    }

    // === LoadById weicht bei invalider Rule auch aus ===

    [Fact]
    public void LoadById_ProfileWithLegacyRule_ReturnsNull()
    {
        const string legacy = """
            {
              "schemaVersion": 5,
              "id": "LEG-X",
              "documentTypeName": "Legacy",
              "tokenization": { "delimiters": ["-"] },
              "recognition": [
                { "method": "contains", "pattern": "X" }
              ]
            }
            """;
        WriteProfile("LEG-X.json", legacy);

        var loaded = _sut.LoadById(_tempRoot, "LEG-X");

        Assert.Null(loaded);
    }
}

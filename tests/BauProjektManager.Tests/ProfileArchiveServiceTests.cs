using System.IO;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="ProfileArchiveService"/> (BPM-108 Phase B).
/// Verschiebt Profile/Templates mit schemaVersion != 5 nach _archiv/schema-reset-*.
/// </summary>
public class ProfileArchiveServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _profilesDir;
    private static readonly LocalFileSystem Fs = new();
    private readonly ProfileArchiveService _sut = new(Fs, Fs, Fs);

    public ProfileArchiveServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "bpm-archive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _profilesDir = Path.Combine(_tempRoot, ".bpm", "profiles");
        Directory.CreateDirectory(_profilesDir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { }
    }

    [Fact]
    public void ArchiveOutdatedProfiles_NoProfilesDir_ReturnsZero()
    {
        var emptyRoot = Path.Combine(Path.GetTempPath(), "bpm-noprofiles-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyRoot);
        try
        {
            var moved = _sut.ArchiveOutdatedProfiles(emptyRoot);
            Assert.Equal(0, moved);
        }
        finally
        {
            Directory.Delete(emptyRoot, true);
        }
    }

    [Fact]
    public void ArchiveOutdatedProfiles_AllV5_NothingMoved()
    {
        File.WriteAllText(Path.Combine(_profilesDir, "p1.json"), """{"schemaVersion":5,"id":"P1"}""");
        File.WriteAllText(Path.Combine(_profilesDir, "p2.json"), """{"schemaVersion":5,"id":"P2"}""");

        var moved = _sut.ArchiveOutdatedProfiles(_tempRoot);

        Assert.Equal(0, moved);
        Assert.True(File.Exists(Path.Combine(_profilesDir, "p1.json")));
        Assert.True(File.Exists(Path.Combine(_profilesDir, "p2.json")));
    }

    [Fact]
    public void ArchiveOutdatedProfiles_V3Profile_IsMovedToArchive()
    {
        File.WriteAllText(Path.Combine(_profilesDir, "v3.json"), """{"schemaVersion":3,"id":"V3"}""");
        File.WriteAllText(Path.Combine(_profilesDir, "v4.json"), """{"schemaVersion":5,"id":"V4"}""");

        var moved = _sut.ArchiveOutdatedProfiles(_tempRoot);

        Assert.Equal(1, moved);
        Assert.False(File.Exists(Path.Combine(_profilesDir, "v3.json")));
        Assert.True(File.Exists(Path.Combine(_profilesDir, "v4.json")));

        var archiveRoot = Path.Combine(_profilesDir, "_archiv");
        Assert.True(Directory.Exists(archiveRoot));
        // Es muss genau einen schema-reset-* Unterordner geben mit v3.json darin
        var resetDirs = Directory.GetDirectories(archiveRoot, "schema-reset-*");
        Assert.Single(resetDirs);
        Assert.True(File.Exists(Path.Combine(resetDirs[0], "v3.json")));
    }

    [Fact]
    public void ArchiveOutdatedProfiles_UnreadableJson_TreatedAsOutdated()
    {
        File.WriteAllText(Path.Combine(_profilesDir, "garbage.json"), "not json at all");
        File.WriteAllText(Path.Combine(_profilesDir, "ok.json"), """{"schemaVersion":5,"id":"OK"}""");

        var moved = _sut.ArchiveOutdatedProfiles(_tempRoot);

        Assert.Equal(1, moved);
        Assert.False(File.Exists(Path.Combine(_profilesDir, "garbage.json")));
        Assert.True(File.Exists(Path.Combine(_profilesDir, "ok.json")));
    }

    [Fact]
    public void ArchiveOutdatedPatternTemplates_AllV5_NothingMoved()
    {
        var cloudShared = Path.Combine(_tempRoot, ".AppData");
        Directory.CreateDirectory(cloudShared);
        File.WriteAllText(Path.Combine(cloudShared, "pattern-templates.json"),
            """[{"schemaVersion":5,"id":"T1","documentTypeName":"X"}]""");

        var archived = _sut.ArchiveOutdatedPatternTemplates(cloudShared);

        Assert.False(archived);
        Assert.True(File.Exists(Path.Combine(cloudShared, "pattern-templates.json")));
    }

    [Fact]
    public void ArchiveOutdatedPatternTemplates_MixedVersions_FileMoved()
    {
        var cloudShared = Path.Combine(_tempRoot, ".AppData");
        Directory.CreateDirectory(cloudShared);
        File.WriteAllText(Path.Combine(cloudShared, "pattern-templates.json"),
            """[{"schemaVersion":5,"id":"T1"},{"schemaVersion":3,"id":"T2"}]""");

        var archived = _sut.ArchiveOutdatedPatternTemplates(cloudShared);

        Assert.True(archived);
        Assert.False(File.Exists(Path.Combine(cloudShared, "pattern-templates.json")));

        var archiveRoot = Path.Combine(cloudShared, "_archiv");
        Assert.True(Directory.Exists(archiveRoot));
        var resetDirs = Directory.GetDirectories(archiveRoot, "schema-reset-*");
        Assert.Single(resetDirs);
        Assert.True(File.Exists(Path.Combine(resetDirs[0], "pattern-templates.json")));
    }

    [Fact]
    public void ArchiveOutdatedPatternTemplates_NoFile_ReturnsFalse()
    {
        var cloudShared = Path.Combine(_tempRoot, ".AppData");
        Directory.CreateDirectory(cloudShared);

        var archived = _sut.ArchiveOutdatedPatternTemplates(cloudShared);

        Assert.False(archived);
    }
}

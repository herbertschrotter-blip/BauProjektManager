using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests für <see cref="DocumentTargetPathResolver"/> (ADR-061 Slice 0.5):
/// Pfadbau aus DB-Stammdaten, Ring2-Modi (None/BuildingParts/Categories),
/// Token-Auflösung (Id/key/Name) und Fail-Fast. Temp-DB via dbPathOverride.
/// </summary>
public class DocumentTargetPathResolverTests : IDisposable
{
    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST\\user";
        public string DisplayName => "Test User";
        public UserContextSource Source => UserContextSource.Local;
    }

    private sealed class FakeDeviceContext : IDeviceContext
    {
        public string DeviceId => "01TESTDEVICE";
        public string DeviceName => "TestDevice";
    }

    private readonly string _dbPath;
    private readonly ProjectDatabase _db;
    private readonly DocumentTargetPathResolver _sut;
    private readonly string _projectId;

    public DocumentTargetPathResolverTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bpm-resolver-{Guid.NewGuid():N}.db");
        _db = new ProjectDatabase(new UlidIdGenerator(), new FakeUserContext(),
            new FakeDeviceContext(), persistenceRegistry: null, dbPathOverride: _dbPath);

        var project = new Project { Name = "Testprojekt" };
        _db.SaveProject(project);
        _projectId = project.Id;
        new DocumentTypeSeedService(_db).EnsureSeeded(_projectId);

        _sut = new DocumentTargetPathResolver(_db, new PlanValueNormalizer(), new LocalFileSystem());
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* ignore */ }
    }

    [Fact]
    public void Resolve_BuildingParts_WithPartAndLevel()
    {
        var partId = _db.InsertBuildingPart(_projectId, "Haus A");
        _db.InsertBuildingLevel(partId, "EG");

        var r = _sut.Resolve(_projectId, new DocumentTargetRequest("polierplan", "Haus A", "EG", "plan.pdf"));

        Assert.Equal(Path.Combine("01 Planunterlagen", "01 Polierpläne", "Haus A", "00 EG"), r.RelativeDirectory);
        Assert.Equal(Path.Combine("01 Planunterlagen", "01 Polierpläne", "Haus A", "00 EG", "plan.pdf"), r.RelativePath);
    }

    [Fact]
    public void Resolve_BuildingParts_WithoutLevel_OmitsRing3()
    {
        _db.InsertBuildingPart(_projectId, "Haus A");

        var r = _sut.Resolve(_projectId, new DocumentTargetRequest("polierplan", "Haus A", null, "plan.pdf"));

        Assert.Equal(Path.Combine("01 Planunterlagen", "01 Polierpläne", "Haus A"), r.RelativeDirectory);
    }

    [Fact]
    public void Resolve_CategoriesRootType_OmitsEmptyTypeFolder()
    {
        // Protokolle ist Root-Typ (folder_name leer) -> kein Typordner-Segment.
        var r = _sut.Resolve(_projectId, new DocumentTargetRequest("protokolle", "Baubesprechung", null, "prot.pdf"));

        Assert.Equal(Path.Combine("06 Protokolle", "Baubesprechung"), r.RelativeDirectory);
    }

    [Fact]
    public void Resolve_NoneType_HasNoRingSegments()
    {
        var r = _sut.Resolve(_projectId, new DocumentTargetRequest("baustelleneinrichtung", null, null, "be.pdf"));

        Assert.Equal(Path.Combine("01 Planunterlagen", "Baustelleneinrichtung"), r.RelativeDirectory);
    }

    [Fact]
    public void Resolve_ByDisplayName_AlsoWorks()
    {
        _db.InsertBuildingPart(_projectId, "Haus A");

        var r = _sut.Resolve(_projectId, new DocumentTargetRequest("Polierplan", "Haus A", null, "p.pdf"));

        Assert.Equal(Path.Combine("01 Planunterlagen", "01 Polierpläne", "Haus A"), r.RelativeDirectory);
    }

    [Fact]
    public void Resolve_MissingRequiredRing2_FailsFast()
    {
        Assert.Throws<DocumentTargetResolutionException>(() =>
            _sut.Resolve(_projectId, new DocumentTargetRequest("polierplan", null, null, "x.pdf")));
    }

    [Fact]
    public void Resolve_UnknownType_FailsFast()
    {
        Assert.Throws<DocumentTargetResolutionException>(() =>
            _sut.Resolve(_projectId, new DocumentTargetRequest("gibtsnicht", null, null, "x.pdf")));
    }

    [Fact]
    public void Resolve_UnresolvablePart_FailsFast()
    {
        Assert.Throws<DocumentTargetResolutionException>(() =>
            _sut.Resolve(_projectId, new DocumentTargetRequest("polierplan", "Unbekanntes Bauteil", null, "x.pdf")));
    }
}

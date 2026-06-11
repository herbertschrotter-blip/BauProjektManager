using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer die Dokumenttyp-Stammdaten in bpm.db (ADR-059-Addendum,
/// BPM-111.05 Slice 2a): DDL, Accessoren, folder_name-Einmal-Regel, Seed.
/// Temp-DB via dbPathOverride — beruehrt NIE die echte bpm.db.
/// </summary>
public class DocumentTypeMasterDataTests : IDisposable
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
    private readonly string ProjectId;

    public DocumentTypeMasterDataTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bpm-doctype-{Guid.NewGuid():N}.db");
        _db = new ProjectDatabase(new UlidIdGenerator(), new FakeUserContext(),
            new FakeDeviceContext(), persistenceRegistry: null, dbPathOverride: _dbPath);
        ProjectId = CreateProject("Testprojekt");
    }

    private string CreateProject(string name)
    {
        // FK document_types.project_id -> projects(id): Tests brauchen ein echtes Projekt
        var project = new Domain.Models.Project { Name = name };
        _db.SaveProject(project);
        return project.Id;
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* ignore */ }
    }

    [Fact]
    public void InsertAndGetDocumentTypes_RoundtripsWithCategories()
    {
        var typeId = _db.InsertDocumentType(ProjectId, "Protokolle", null, "#555555",
            Ring2Source.Categories, sortOrder: 10);
        _db.InsertDocumentTypeCategory(typeId, "Baubesprechung", null, 10);
        _db.InsertDocumentTypeCategory(typeId, "Sicherheit", null, 20);

        var types = _db.GetDocumentTypes(ProjectId);

        var t = Assert.Single(types);
        Assert.Equal("Protokolle", t.Name);
        Assert.Equal(Ring2Source.Categories, t.Ring2Source);
        Assert.Equal("#555555", t.ColorHex);
        Assert.Equal(2, t.Categories.Count);
        Assert.Equal("Baubesprechung", t.Categories[0].Name);
    }

    [Fact]
    public void InsertDocumentType_GeneratesFolderNameOnce()
    {
        // folder_name aus Name erzeugt (Umlaute bleiben, ungueltige Zeichen raus)
        var typeId = _db.InsertDocumentType(ProjectId, "Pläne: Sonstiges?", null, null,
            Ring2Source.None, 10);

        var t = Assert.Single(_db.GetDocumentTypes(ProjectId));
        Assert.Equal(typeId, t.Id);
        Assert.Equal("Pläne Sonstiges", t.FolderName);
    }

    [Fact]
    public void Seed_CreatesBuiltinsIdempotent()
    {
        var seeder = new DocumentTypeSeedService(_db);

        seeder.EnsureSeeded(ProjectId);
        seeder.EnsureSeeded(ProjectId); // zweiter Lauf darf nichts duplizieren

        var types = _db.GetDocumentTypes(ProjectId);
        Assert.Equal(7, types.Count);
        Assert.Equal("Polierplan", types[0].Name);
        Assert.True(types[0].IsBuiltin);
        Assert.Equal(Ring2Source.BuildingParts, types[0].Ring2Source);

        var protokolle = types.Single(t => t.Name == "Protokolle");
        Assert.Equal(Ring2Source.Categories, protokolle.Ring2Source);
        Assert.Equal(4, protokolle.Categories.Count);

        var fertigteile = types.Single(t => t.Name == "Fertigteile");
        Assert.Equal(3, fertigteile.Categories.Count);
        Assert.Equal("Wände", fertigteile.Categories[0].Name);
    }

    [Fact]
    public void Seed_IsPerProject()
    {
        var projectA = CreateProject("Projekt A");
        var projectB = CreateProject("Projekt B");
        var seeder = new DocumentTypeSeedService(_db);
        seeder.EnsureSeeded(projectA);
        seeder.EnsureSeeded(projectB);

        Assert.Equal(7, _db.GetDocumentTypes(projectA).Count);
        Assert.Equal(7, _db.GetDocumentTypes(projectB).Count);
    }
}

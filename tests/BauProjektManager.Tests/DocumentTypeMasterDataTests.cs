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
    public void Creation_GeneratesKeyAndPersistsRoot()
    {
        var svc = new DocumentTypeCreationService(_db, new PlanValueNormalizer());

        var t = svc.Create(ProjectId, "Polierplan", "01 Planunterlagen", Ring2Source.BuildingParts);

        Assert.Equal("polierplan", t.Key);
        Assert.Equal("01 Planunterlagen", t.RootRelativePath);
        Assert.Equal("Polierplan", t.FolderName);
        Assert.False(t.IsBuiltin);
    }

    [Fact]
    public void Creation_DeduplicatesKeyOnCollision()
    {
        var svc = new DocumentTypeCreationService(_db, new PlanValueNormalizer());

        var a = svc.Create(ProjectId, "Sonderplan", "01 Planunterlagen", Ring2Source.None);
        var b = svc.Create(ProjectId, "Sonderplan", "01 Planunterlagen", Ring2Source.None);

        Assert.Equal("sonderplan", a.Key);
        Assert.Equal("sonderplan-2", b.Key);
    }

    [Fact]
    public void Seed_CreatesTypesFromTemplate_Idempotent()
    {
        var seeder = new DocumentTypeSeedService(_db);

        seeder.EnsureSeeded(ProjectId);
        seeder.EnsureSeeded(ProjectId); // zweiter Lauf darf nichts duplizieren

        var types = _db.GetDocumentTypes(ProjectId);
        Assert.Equal(7, types.Count);

        // Reihenfolge = Template-Reihenfolge (Planunterlagen-Unterordner, dann Protokolle).
        Assert.Equal("Ausschreibungsplan", types[0].Name);
        Assert.True(types[0].IsBuiltin);

        // Ordner-Wahrheit (ADR-061): Polierplan unter "01 Planunterlagen" / "01 Polierpläne".
        var polier = types.Single(t => t.Key == "polierplan");
        Assert.Equal("Polierplan", polier.Name);
        Assert.Equal("01 Planunterlagen", polier.RootRelativePath);
        Assert.Equal("01 Polierpläne", polier.FolderName);
        Assert.Equal(Ring2Source.BuildingParts, polier.Ring2Source);

        // Protokolle = Root-Typ: eigener Root, folder_name leer, Kategorien.
        var protokolle = types.Single(t => t.Key == "protokolle");
        Assert.Equal("06 Protokolle", protokolle.RootRelativePath);
        Assert.Equal(string.Empty, protokolle.FolderName);
        Assert.Equal(Ring2Source.Categories, protokolle.Ring2Source);
        Assert.Equal(4, protokolle.Categories.Count);

        var fertigteile = types.Single(t => t.Key == "fertigteile");
        Assert.Equal(3, fertigteile.Categories.Count);
        Assert.Equal("Wände", fertigteile.Categories[0].Name);
    }

    [Fact]
    public void InsertDocumentType_PersistsKeyAndRootRelativePath()
    {
        // ADR-061 Slice 0.3: key + root_relative_path schreiben und lesen.
        _db.InsertDocumentType(ProjectId, "Polierplan", "01 Polierpläne", null,
            Ring2Source.BuildingParts, sortOrder: 10,
            key: "polierplan", rootRelativePath: "01 Planunterlagen");

        var t = Assert.Single(_db.GetDocumentTypes(ProjectId));
        Assert.Equal("polierplan", t.Key);
        Assert.Equal("01 Planunterlagen", t.RootRelativePath);
    }

    [Fact]
    public void InsertDocumentType_DefaultsKeyAndRootToEmpty()
    {
        // Permissive Defaults (Slice 0.2/0.3): ohne explizite Werte -> leer, kein NULL.
        _db.InsertDocumentType(ProjectId, "Sonstiges", null, null, Ring2Source.None, 10);

        var t = Assert.Single(_db.GetDocumentTypes(ProjectId));
        Assert.Equal(string.Empty, t.Key);
        Assert.Equal(string.Empty, t.RootRelativePath);
    }

    [Fact]
    public void InsertBuildingLevel_SetsFolderNameFromPrefixAndName()
    {
        // ADR-061 Slice 0.3: folder_name wird beim Insert EINMAL aus Prefix(0)+Name erzeugt.
        var partId = _db.InsertBuildingPart(ProjectId, "Haus A");
        _db.InsertBuildingLevel(partId, "EG");

        var part = Assert.Single(_db.GetBuildingParts(ProjectId));
        var level = Assert.Single(part.Levels);
        Assert.Equal("00 EG", level.FolderName);
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

using System.IO;
using System.Text.Json;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// "Mit Claude sortieren" (PlanManager-Seite): Auftrags-Builder (FakeFileStore +
/// Temp-DBs), Entwurf-Pruefer (gueltig, falsche ID, nicht aufloesbar, fremde
/// datei-id, Bucket-A-Datei, Vollstaendigkeit) und Uebernahme als Vorschlag.
/// </summary>
public partial class ClaudeSortierungTests : IDisposable
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

    private sealed class FakePdfText(IReadOnlyList<PdfWord> words) : IPdfTextService
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<PdfWord>> GetWordsAsync(Stream pdf, int pageIndex, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(words);
        }
    }

    private const string Root = @"C:\proj";
    private const string AuftragsOrdner = @"C:\appdata\claude\A-0001";

    private readonly string _bpmDbPath;
    private readonly ProjectDatabase _bpmDb;
    private readonly PlanManagerDatabase _planDb;
    private readonly FakeFileStore _fs = new();
    private readonly string _projectId;
    private readonly string _polierplanId;
    private readonly string _partId;
    private readonly string _levelId;
    private readonly List<CaptureItem> _analyse;

    public ClaudeSortierungTests()
    {
        _bpmDbPath = Path.Combine(Path.GetTempPath(), $"bpm-claudesort-{Guid.NewGuid():N}.db");
        _bpmDb = new ProjectDatabase(new UlidIdGenerator(), new FakeUserContext(),
            new FakeDeviceContext(), persistenceRegistry: null, dbPathOverride: _bpmDbPath);
        var project = new Project { Name = "Testprojekt" };
        _bpmDb.SaveProject(project);
        _projectId = project.Id;
        new DocumentTypeSeedService(_bpmDb).EnsureSeeded(_projectId);

        IIdGenerator idGen = new UlidIdGenerator();
        _planDb = new PlanManagerDatabase(_projectId, idGen, dbPathOverride: TempDb.NewTempDbPath(idGen.NewId()));

        _partId = _bpmDb.InsertBuildingPart(_projectId, "Haus A");
        _bpmDb.InsertBuildingLevel(_partId, "EG");
        _levelId = _bpmDb.GetBuildingParts(_projectId).Single(p => p.Id == _partId).Levels.Single().Id;
        _polierplanId = _bpmDb.GetDocumentTypes(_projectId).Single(t => t.Key == "polierplan").Id;

        _analyse =
        [
            Item("5998-100_EG.pdf", "m1", CaptureBucket.NewCapture),
            Item("scan_001.pdf", "m2", CaptureBucket.NewCapture),
            Item("dublette.pdf", "m3", CaptureBucket.Duplicate),
            Item("5998-200-B.pdf", "m4", CaptureBucket.UpdateProposal),
        ];
        foreach (var i in _analyse)
            _fs.AddFile(Path.Combine(Root, i.File.Scan.RelativePath), [1, 2, 3]);
    }

    public void Dispose()
    {
        _bpmDb.Dispose();
        using (var pc = new SqliteConnection($"Data Source={_bpmDbPath}"))
            SqliteConnection.ClearPool(pc);
        TempDb.Delete(_bpmDbPath);
        var planPath = _planDb.GetDatabasePath();
        _planDb.Dispose();
        using (var pc = new SqliteConnection($"Data Source={planPath}"))
            SqliteConnection.ClearPool(pc);
        TempDb.Delete(planPath);
    }

    private static CaptureItem Item(string fileName, string md5, CaptureBucket bucket)
    {
        var scan = new ScannedFile(Path.Combine("_Eingang", fileName), fileName,
            Path.GetExtension(fileName), 2048, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        return new CaptureItem(new FingerprintedFile(scan, md5),
            new LightweightPlanExtractor().ExtractCandidates(fileName), bucket, null, null);
    }

    private ClaudeSortAuftragBuilder Builder(IPdfTextService? pdf = null)
        => new(_bpmDb, _planDb, _fs, _fs, _fs, pdf);

    private ClaudeSortEntwurfPruefer Pruefer()
    {
        var normalizer = new PlanValueNormalizer();
        return new ClaudeSortEntwurfPruefer(_bpmDb,
            new DocumentTargetPathResolver(_bpmDb, normalizer, _fs), _fs, _fs, normalizer);
    }

    private Task<ClaudeSortAuftrag> ErstelleAuftragAsync(IPdfTextService? pdf = null, bool plankopf = true)
        => Builder(pdf).ErstellenAsync(new ClaudeSortAuftragAnfrage(_projectId, Root, AuftragsOrdner, _analyse, plankopf));

    /// <summary>Auftrag mit eigener Analyse (alle Bucket C), Dateien liegen im Fake-Dateisystem.</summary>
    private Task<ClaudeSortAuftrag> ErstelleAuftragAsync(params string[] dateien)
    {
        var analyse = dateien.Select((d, i) => Item(d, $"x{i}", CaptureBucket.NewCapture)).ToList();
        foreach (var i in analyse)
            _fs.AddFile(Path.Combine(Root, i.File.Scan.RelativePath), [1]);
        _eigeneAnalyse = analyse;
        return Builder().ErstellenAsync(new ClaudeSortAuftragAnfrage(_projectId, Root, AuftragsOrdner, analyse, false));
    }

    private List<CaptureItem> _eigeneAnalyse = [];

    private JsonElement LeseKontext()
        => JsonDocument.Parse(_fs.ReadAllText(Path.Combine(AuftragsOrdner, ClaudeSortArten.KontextDatei))).RootElement;

    private string PolierplanZeile(string dateiId = "D-001", string? typId = null, string? bauteilId = null,
        string? geschossId = null)
        => $$"""
           {"datei_id":"{{dateiId}}","dokumenttyp_id":"{{typId ?? _polierplanId}}","bauteil_id":"{{bauteilId ?? _partId}}",
            "kategorie_id":null,"geschoss_id":"{{geschossId ?? _levelId}}","plannummer":"5998-100","index":"B",
            "bezeichnung":"Decke über EG","unsicher":["index"],"hinweise":"Index nur im Dateinamen",
            "begruendung":"Plankopf: Polierplan Haus A"}
           """;

    private static string Entwurf(string zuordnungen, string nichtZuordenbar = """{"datei_id":"D-002","grund":"Foto"}""")
        => $$"""{"zuordnungen":[{{zuordnungen}}],"nicht_zuordenbar":[{{nichtZuordenbar}}]}""";

    // ── Auftrags-Builder ─────────────────────────────────────────────

    [Fact]
    public async Task Builder_WritesAuftragFiles_WithOnlyBucketC()
    {
        var auftrag = await ErstelleAuftragAsync();

        Assert.Equal("A-0001", auftrag.Name);
        Assert.Equal(new[] { "D-001", "D-002" }, auftrag.Dateien.Keys.Order());
        Assert.Equal("5998-100_EG.pdf", auftrag.Dateien["D-001"].File.Scan.FileName);
        Assert.True(_fs.FileExists(Path.Combine(AuftragsOrdner, ClaudeSortArten.AuftragDatei)));
        Assert.True(_fs.FileExists(Path.Combine(AuftragsOrdner, ClaudeSortArten.SchemaDatei)));

        var auftragText = _fs.ReadAllText(Path.Combine(AuftragsOrdner, ClaudeSortArten.AuftragDatei));
        Assert.Contains("Rahmen für alle Aufträge", auftragText);
        Assert.Contains("Art „Planzuordnung“", auftragText);
        JsonDocument.Parse(_fs.ReadAllText(Path.Combine(AuftragsOrdner, ClaudeSortArten.SchemaDatei)));

        var kontextText = _fs.ReadAllText(Path.Combine(AuftragsOrdner, ClaudeSortArten.KontextDatei));
        Assert.DoesNotContain("dublette.pdf", kontextText);
        Assert.DoesNotContain("5998-200-B.pdf", kontextText);
        Assert.DoesNotContain(Root, kontextText);
        Assert.DoesNotContain("Test User", kontextText);
    }

    [Fact]
    public async Task Builder_Kontext_ContainsMasterDataIdsAndInputFiles()
    {
        await ErstelleAuftragAsync();
        var k = LeseKontext();

        var polier = k.GetProperty("dokumenttypen").EnumerateArray()
            .Single(t => t.GetProperty("id").GetString() == _polierplanId);
        Assert.Equal("polierplan", polier.GetProperty("key").GetString());
        Assert.Equal("bauteile", polier.GetProperty("unterteilung").GetString());
        var protokolle = k.GetProperty("dokumenttypen").EnumerateArray()
            .Single(t => t.GetProperty("key").GetString() == "protokolle");
        Assert.Equal("kategorien", protokolle.GetProperty("unterteilung").GetString());
        Assert.Contains(protokolle.GetProperty("kategorien").EnumerateArray(),
            c => c.GetProperty("name").GetString() == "Baubesprechung");

        var bauteil = Assert.Single(k.GetProperty("bauteile").EnumerateArray());
        Assert.Equal(_partId, bauteil.GetProperty("id").GetString());
        Assert.Equal(_levelId, Assert.Single(bauteil.GetProperty("geschosse").EnumerateArray()).GetProperty("id").GetString());

        var datei = k.GetProperty("eingangsdateien").EnumerateArray().First();
        Assert.Equal("D-001", datei.GetProperty("id").GetString());
        Assert.Equal("5998-100_EG.pdf", datei.GetProperty("dateiname").GetString());
        Assert.Equal(2, datei.GetProperty("groesse_kb").GetInt64());
        Assert.Equal("5998-100", datei.GetProperty("aus_dateiname").GetProperty("plannummer").GetString());
        Assert.Equal("", datei.GetProperty("plankopf_text").GetString());
    }

    [Fact]
    public async Task Builder_Kontext_ListsKnownPlansWithCurrentIndex()
    {
        var docId = _planDb.ResolveOrCreateDocument(
            projectId: _projectId, documentKey: "polierplan|5998_050|haus_a",
            documentTypeId: _polierplanId, planNumber: "5998-050",
            documentType: "Polierplan", title: "",
            targetFolder: "Polierplan", relativeDirectory: "01 Planunterlagen/01 Polierpläne/Haus A",
            buildingPartId: _partId, buildingLevelId: null);
        _planDb.InsertRevision(docId, planIndex: "C", indexSource: "FileName",
            PlanArchive.Status.Current, "2026-06-01T00:00:00Z", null, "2026-06-01T00:00:00Z", lastImportId: null);

        var auftrag = await ErstelleAuftragAsync();

        var plan = Assert.Single(LeseKontext().GetProperty("bekannte_plaene").EnumerateArray());
        Assert.Equal("5998-050", plan.GetProperty("plannummer").GetString());
        Assert.Equal("C", plan.GetProperty("index").GetString());
        Assert.Equal(_partId, plan.GetProperty("bauteil_id").GetString());
        Assert.Single(auftrag.BekanntePlaene);
    }

    [Fact]
    public async Task Builder_PlankopfText_FromPdfTextPort()
    {
        var pdf = new FakePdfText(
        [
            new PdfWord("Grundriss", 10, 10, 30, 5),
            new PdfWord("Polierplan", 800, 550, 40, 4),
            new PdfWord("Haus", 800, 556, 15, 4),
            new PdfWord("A", 817, 556, 4, 4),
        ]);

        await ErstelleAuftragAsync(pdf);

        var text = LeseKontext().GetProperty("eingangsdateien").EnumerateArray().First()
            .GetProperty("plankopf_text").GetString();
        Assert.Equal("Polierplan\r\nHaus A".ReplaceLineEndings(), text?.ReplaceLineEndings());
        Assert.Equal(2, pdf.Calls); // nur die beiden Bucket-C-PDFs
    }

    [Fact]
    public async Task Builder_WithoutBucketC_Throws()
    {
        var nurA = _analyse.Where(i => i.Bucket != CaptureBucket.NewCapture).ToList();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Builder().ErstellenAsync(new ClaudeSortAuftragAnfrage(_projectId, Root, AuftragsOrdner, nurA, false)));
    }

    [Fact]
    public async Task Builder_OccupiedFolder_Throws()
    {
        await ErstelleAuftragAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => ErstelleAuftragAsync());
    }

    [Fact]
    public void NaechsterAuftragsName_CountsUp()
    {
        Assert.Equal("A-0001", ClaudeSortAuftragBuilder.NaechsterAuftragsName([]));
        Assert.Equal("A-0004", ClaudeSortAuftragBuilder.NaechsterAuftragsName(["A-0001", "A-0003", "egal"]));
    }

    [Fact]
    public void NaechsterAuftragsOrdner_UsesExistingFolders()
    {
        _fs.CreateDirectory(@"C:\appdata\claude\A-0001");
        _fs.CreateDirectory(@"C:\appdata\claude\A-0002");

        Assert.Equal(@"C:\appdata\claude\A-0003", Builder().NaechsterAuftragsOrdner(@"C:\appdata\claude"));
    }

    // ── Entwurf-Pruefer ─────────────────────────────────────────────

    [Fact]
    public async Task Pruefer_ValidDraft_ReturnsRowsWithResolverTarget()
    {
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile()), _analyse);

        Assert.True(r.IstGueltig, r.FehlerText);
        var zeile = Assert.Single(r.GueltigeZeilen);
        Assert.Equal("D-001", zeile.DateiId);
        Assert.Equal(Path.Combine("01 Planunterlagen", "01 Polierpläne", "Haus A", "00 EG"), zeile.TargetRelativeDirectory);
        Assert.Equal(["index"], zeile.Unsicher);
        Assert.Equal("D-002", Assert.Single(r.NichtZuordenbar).DateiId);
    }

    [Fact]
    public async Task Pruefer_ReadsEntwurfFromFolder()
    {
        var auftrag = await ErstelleAuftragAsync();
        var pruefer = Pruefer();

        Assert.Contains("fehlt", Assert.Single(pruefer.PruefeOrdner(auftrag, _analyse).Fehler));

        _fs.WriteAllText(Path.Combine(AuftragsOrdner, ClaudeSortArten.EntwurfDatei), Entwurf(PolierplanZeile()));
        Assert.True(pruefer.PruefeOrdner(auftrag, _analyse).IstGueltig);
    }

    [Fact]
    public async Task Pruefer_UnknownTypeId_IsRejected()
    {
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile(typId: "gibtsnicht")), _analyse);

        Assert.False(r.IstGueltig);
        Assert.Empty(r.GueltigeZeilen);
        Assert.Contains(r.Fehler, f => f.Contains("dokumenttyp_id 'gibtsnicht' gibt es nicht"));
        Assert.StartsWith("Entwurf ungültig:", r.FehlerText);
    }

    [Fact]
    public async Task Pruefer_LevelOfOtherPart_IsRejected()
    {
        var otherPart = _bpmDb.InsertBuildingPart(_projectId, "Haus B");
        _bpmDb.InsertBuildingLevel(otherPart, "OG1");
        var otherLevel = _bpmDb.GetBuildingParts(_projectId).Single(p => p.Id == otherPart).Levels.Single().Id;
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile(geschossId: otherLevel)), _analyse);

        Assert.Empty(r.GueltigeZeilen);
        Assert.Contains(r.Fehler, f => f.Contains("gehört nicht zum Bauteil 'Haus A'"));
    }

    [Fact]
    public async Task Pruefer_Unresolvable_TakesResolverMessage()
    {
        var auftrag = await ErstelleAuftragAsync();
        using (var conn = new SqliteConnection($"Data Source={_bpmDbPath}"))
        {
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE building_parts SET folder_name = '' WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", _partId);
            cmd.ExecuteNonQuery();
        }

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile()), _analyse);

        Assert.Empty(r.GueltigeZeilen);
        Assert.Contains(r.Fehler, f => f.Contains("Ablageziel nicht auflösbar") && f.Contains("keinen Ordnernamen"));
    }

    [Fact]
    public async Task Pruefer_ForeignDateiId_IsRejected()
    {
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag,
            Entwurf(PolierplanZeile() + "," + PolierplanZeile(dateiId: "D-099")), _analyse);

        Assert.Single(r.GueltigeZeilen);
        Assert.Contains(r.Fehler, f => f.Contains("'D-099' gehört nicht zu diesem Auftrag"));
    }

    [Fact]
    public async Task Pruefer_FileNowBucketA_IsRejected()
    {
        var auftrag = await ErstelleAuftragAsync();
        // Zwischen Auftrag und Entwurf wurde der Inhalt bekannt (z. B. parallel importiert) → jetzt Dublette.
        var neu = _analyse.Select(i => i.File.Scan.FileName == "5998-100_EG.pdf"
            ? i with { Bucket = CaptureBucket.Duplicate } : i).ToList();

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile()), neu);

        Assert.Empty(r.GueltigeZeilen);
        Assert.Contains(r.Fehler, f => f.Contains("nicht mehr als neue Datei"));
    }

    [Fact]
    public async Task Pruefer_MissingFileAndDuplicateId_AreReported()
    {
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag,
            Entwurf(PolierplanZeile(), """{"datei_id":"D-001","grund":"doppelt"}"""), _analyse);

        Assert.Contains(r.Fehler, f => f.Contains("D-001 kommt mehrfach"));
        Assert.Contains(r.Fehler, f => f.Contains("D-002") && f.Contains("fehlt im Entwurf"));
    }

    [Fact]
    public async Task Pruefer_SchemaViolations_AreReported()
    {
        var auftrag = await ErstelleAuftragAsync();
        var pruefer = Pruefer();

        Assert.Contains("kein gültiges JSON", Assert.Single(pruefer.Pruefe(auftrag, "{kaputt", _analyse).Fehler));
        Assert.Contains("'zuordnungen' fehlt", Assert.Single(pruefer.Pruefe(auftrag, "{}", _analyse).Fehler.Take(1)));

        var r = pruefer.Pruefe(auftrag,
            Entwurf(PolierplanZeile().Replace("\"unsicher\":[\"index\"]", "\"unsicher\":[\"farbe\"],\"extra\":1")),
            _analyse);
        Assert.Contains(r.Fehler, f => f.Contains("unbekanntes Feld 'extra'"));
        Assert.Contains(r.Fehler, f => f.Contains("'unsicher' enthält"));
        Assert.Empty(r.GueltigeZeilen);
    }

    [Fact]
    public async Task Pruefer_CategoryType_RequiresCategoryAndNoPart()
    {
        var auftrag = await ErstelleAuftragAsync();
        var protokolle = _bpmDb.GetDocumentTypes(_projectId).Single(t => t.Key == "protokolle");
        var kategorie = protokolle.Categories.Single(c => c.Name == "Baubesprechung");
        var ok = $$"""{"datei_id":"D-002","dokumenttyp_id":"{{protokolle.Id}}","kategorie_id":"{{kategorie.Id}}","begruendung":"Protokoll"}""";
        var mitBauteil = $$"""{"datei_id":"D-002","dokumenttyp_id":"{{protokolle.Id}}","kategorie_id":"{{kategorie.Id}}","bauteil_id":"{{_partId}}","begruendung":"x"}""";

        var r = Pruefer().Pruefe(auftrag, $$"""{"zuordnungen":[{{PolierplanZeile()}},{{ok}}]}""", _analyse);
        Assert.True(r.IstGueltig, r.FehlerText);
        Assert.Equal(Path.Combine("06 Protokolle", "Baubesprechung"), r.GueltigeZeilen[1].TargetRelativeDirectory);

        var r2 = Pruefer().Pruefe(auftrag, $$"""{"zuordnungen":[{{PolierplanZeile()}},{{mitBauteil}}]}""", _analyse);
        Assert.Contains(r2.Fehler, f => f.Contains("nach Kategorien unterteilt"));
    }

    [Fact]
    public async Task Pruefer_KnownPlanNumberOtherKey_GivesHint()
    {
        var docId = _planDb.ResolveOrCreateDocument(
            projectId: _projectId, documentKey: "polierplan|5998_100|haus_a",
            documentTypeId: _polierplanId, planNumber: "5998-100", documentType: "Polierplan", title: "",
            targetFolder: "Polierplan", relativeDirectory: "x", buildingPartId: _partId, buildingLevelId: null);
        _planDb.InsertRevision(docId, planIndex: "A", indexSource: "FileName",
            PlanArchive.Status.Current, "2026-06-01T00:00:00Z", null, "2026-06-01T00:00:00Z", lastImportId: null);
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile()), _analyse);

        Assert.True(r.IstGueltig, r.FehlerText);
        Assert.Contains(r.Hinweise, h => h.Contains("5998-100 gibt es im Archiv bei anderem Typ"));
    }

    [Fact]
    public async Task Pruefer_KnownPlanSameKey_IsRejectedAsUpdate()
    {
        var key = CaptureConfirmService.BuildDocumentKey(_polierplanId, "5998-100", _partId, _levelId,
            new PlanValueNormalizer());
        var docId = _planDb.ResolveOrCreateDocument(
            projectId: _projectId, documentKey: key,
            documentTypeId: _polierplanId, planNumber: "5998-100", documentType: "Polierplan", title: "",
            targetFolder: "Polierplan", relativeDirectory: "x", buildingPartId: _partId, buildingLevelId: _levelId);
        _planDb.InsertRevision(docId, planIndex: "A", indexSource: "FileName",
            PlanArchive.Status.Current, "2026-06-01T00:00:00Z", null, "2026-06-01T00:00:00Z", lastImportId: null);
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile()), _analyse);

        Assert.Empty(r.GueltigeZeilen);
        Assert.Contains(r.Fehler, f => f.Contains("schon im Archiv (Index A") && f.Contains("Update"));
    }

    [Fact]
    public async Task Pruefer_TwoRowsSameKeyAndExtension_BothRejected()
    {
        var auftrag = await ErstelleAuftragAsync("5998-100_A.pdf", "5998-100_B.pdf");

        var r = Pruefer().Pruefe(auftrag,
            $$"""{"zuordnungen":[{{PolierplanZeile("D-001")}},{{PolierplanZeile("D-002")}}]}""", _eigeneAnalyse);

        Assert.Empty(r.GueltigeZeilen);
        Assert.Contains(r.Fehler, f => f.Contains("D-001, D-002") && f.Contains("gleiche Endung"));
    }

    [Fact]
    public async Task Pruefer_PdfDwgPair_SameAssignment_IsValid_DifferentIsRejected()
    {
        var auftrag = await ErstelleAuftragAsync("5998-300.dwg", "5998-300.pdf");
        var gleich = $$"""{"zuordnungen":[{{PolierplanZeile("D-001")}},{{PolierplanZeile("D-002")}}]}""";

        var ok = Pruefer().Pruefe(auftrag, gleich, _eigeneAnalyse);
        Assert.True(ok.IstGueltig, ok.FehlerText);
        Assert.Equal(2, ok.GueltigeZeilen.Count);

        var mitIndexC = PolierplanZeile("D-002").Replace("\"index\":\"B\"", "\"index\":\"C\"");
        var zweite = $$"""{"zuordnungen":[{{PolierplanZeile("D-001")}},{{mitIndexC}}]}""";
        var falsch = Pruefer().Pruefe(auftrag, zweite, _eigeneAnalyse);
        Assert.Empty(falsch.GueltigeZeilen);
        Assert.Contains(falsch.Fehler, f => f.Contains("dieselbe Zuordnung"));

        var halb = Pruefer().Pruefe(auftrag,
            $$"""{"zuordnungen":[{{PolierplanZeile("D-002")}}],"nicht_zuordenbar":[{"datei_id":"D-001","grund":"x"}]}""",
            _eigeneAnalyse);
        Assert.Empty(halb.GueltigeZeilen);
        Assert.Contains(halb.Fehler, f => f.Contains("beide zuordnen"));
    }

    [Fact]
    public async Task Pruefer_EntwurfTooLarge_IsRejected()
    {
        var auftrag = await ErstelleAuftragAsync();
        _fs.AddFile(Path.Combine(AuftragsOrdner, ClaudeSortArten.EntwurfDatei),
            new byte[ClaudeSortEntwurfPruefer.MaxEntwurfBytes + 1]);

        var r = Pruefer().PruefeOrdner(auftrag, _analyse);

        Assert.Contains("zu groß", Assert.Single(r.Fehler));
    }

    [Fact]
    public async Task Builder_WithoutPlankopfRelease_ReadsNoPdfText()
    {
        var pdf = new FakePdfText([new PdfWord("Polierplan", 800, 550, 40, 4)]);

        await ErstelleAuftragAsync(pdf, plankopf: false);

        Assert.Equal(0, pdf.Calls);
        Assert.All(LeseKontext().GetProperty("eingangsdateien").EnumerateArray(),
            e => Assert.Equal("", e.GetProperty("plankopf_text").GetString()));
    }

    [Fact]
    public async Task Builder_WritesRulesBesideFolder_NotInside()
    {
        var auftrag = await ErstelleAuftragAsync();

        Assert.Equal(AuftragsOrdner + ".regeln.md", auftrag.RegelnDatei);
        Assert.Contains("FRAGE", _fs.ReadAllText(auftrag.RegelnDatei));
    }

    [Theory]
    [InlineData(@"C:\proj\claude\A-0001")]
    [InlineData(@"C:\appdata\claude\Auftrag1")]
    public async Task Builder_FolderInProjectOrBadName_Throws(string ordner)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Builder().ErstellenAsync(new ClaudeSortAuftragAnfrage(_projectId, Root, ordner, _analyse, false)));
    }

    [Fact]
    public async Task Builder_FolderWithSubfolder_CountsAsOccupied()
    {
        _fs.CreateDirectory(Path.Combine(AuftragsOrdner, "alt"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => ErstelleAuftragAsync());
    }

    // ── Uebernahme ─────────────────────────────────────────────

    [Fact]
    public async Task Uebernahme_CreatesSuggestionPending_LikeCompleteCapture()
    {
        var auftrag = await ErstelleAuftragAsync();
        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile()), _analyse);
        var store = new PendingAssignmentStore();

        var erg = ClaudeSortUebernahme.Uebernehmen(r.GueltigeZeilen, store);

        var p = Assert.Single(erg.Uebernommen);
        Assert.Same(p, store.Get(Path.Combine("_Eingang", "5998-100_EG.pdf")));
        Assert.True(p.IsClaudeSuggestion);
        Assert.Equal(PendingAssignmentSource.ClaudeSuggestion, p.Source);
        Assert.Equal(CaptureBucket.NewCapture, p.SourceBucket);
        Assert.Equal((_polierplanId, "Polierplan"), (p.DocumentTypeId, p.DocumentTypeName));
        Assert.Equal(("Haus A", "EG"), (p.BuildingPart, p.Level));
        Assert.Equal((_partId, _levelId, (string?)null), (p.BuildingPartId, p.BuildingLevelId, p.CategoryId));
        Assert.Equal(("5998-100", "B", "Decke über EG"), (p.PlanNumber, p.Index, p.Title));
        Assert.Equal(Path.Combine("01 Planunterlagen", "01 Polierpläne", "Haus A", "00 EG"), p.TargetRelativeDirectory);
        Assert.Null(p.Match);
        Assert.Equal("Plankopf: Polierplan Haus A", p.Suggestion?.Reason);
        Assert.Equal(["index"], p.Suggestion?.UncertainFields);
        Assert.Equal("Index nur im Dateinamen", p.Suggestion?.Note);
    }

    [Fact]
    public async Task Uebernahme_KeepsManualAssignment_ReplacesOldSuggestion()
    {
        var auftrag = await ErstelleAuftragAsync();
        var zeile = Assert.Single(Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile()), _analyse).GueltigeZeilen);
        var store = new PendingAssignmentStore();
        var manuell = ClaudeSortUebernahme.ZuPending(zeile) with { Source = PendingAssignmentSource.Manual, Index = "X" };
        store.Assign(manuell);

        var erg = ClaudeSortUebernahme.Uebernehmen([zeile], store);

        Assert.Empty(erg.Uebernommen);
        Assert.Contains("von Hand", Assert.Single(erg.Uebersprungen).Grund);
        Assert.Same(manuell, store.Get(zeile.Item.File.Scan.RelativePath));

        store.Assign(manuell with { Source = PendingAssignmentSource.ClaudeSuggestion });
        Assert.Single(ClaudeSortUebernahme.Uebernehmen([zeile], store).Uebernommen);
        Assert.Equal("B", store.Get(zeile.Item.File.Scan.RelativePath)?.Index);
    }

    [Fact]
    public void PendingAssignment_DefaultSource_IsManual()
    {
        var p = new PendingAssignment(_analyse[0].File, CaptureBucket.NewCapture, "t", "T",
            null, null, null, null, "x", Match: null);

        Assert.Equal(PendingAssignmentSource.Manual, p.Source);
        Assert.False(p.IsClaudeSuggestion);
    }
}

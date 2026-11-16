using System.IO;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;

namespace BauProjektManager.Tests;

/// <summary>
/// "Mit Claude sortieren": Index-Datum, Segmente und Tags (BPM-131) — nichts erfunden, alles bis in den Import.
/// </summary>
public partial class ClaudeSortierungTests
{
    private sealed class FakeKatalog : ISegmentTypeCatalog
    {
        private readonly List<SegmentTypeDefinition> _typen =
        [
            new() { Id = SegmentTypeIds.PlanNumber, Name = "Plannummer", TokenKey = "plannummer" },
            new() { Id = SegmentTypeIds.Geschoss, Name = "Geschoss", TokenKey = "geschoss" },
            new() { Id = SegmentTypeIds.Ignore, Name = "Ignorieren", TokenKey = "ignore" },
        ];

        public IReadOnlyList<SegmentTypeDefinition> GetEffectiveActive() => _typen;
        public SegmentTypeDefinition? GetIncludingDeleted(string id) => _typen.FirstOrDefault(t => t.Id == id);
        public IReadOnlyDictionary<string, SegmentTypeDefinition> SnapshotIncludingDeleted() => _typen.ToDictionary(t => t.Id);
        public IReadOnlyList<SegmentTypeGroupDefinition> GetActiveGroups() => [];
        public void Invalidate() { }
        public event EventHandler? Changed { add { } remove { } }
    }

    private static readonly ISegmentTypeCatalog Katalog = new FakeKatalog();

    private string ZeileMit(string extras) =>
        PolierplanZeile().TrimEnd().TrimEnd('}') + "," + extras + "}";

    [Fact]
    public async Task Kontext_ListsSegmentTypesWithoutIgnore_AndExistingTags()
    {
        await ErstelleAuftragAsync();
        var k = LeseKontext();

        var ids = k.GetProperty("segmenttypen").EnumerateArray().Select(t => t.GetProperty("id").GetString()).ToList();
        Assert.Equal([SegmentTypeIds.PlanNumber, SegmentTypeIds.Geschoss], ids);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, k.GetProperty("vorhandene_tags").ValueKind);
    }

    [Fact]
    public async Task Pruefer_DateSegmentsTags_Valid_GoIntoPending()
    {
        var auftrag = await ErstelleAuftragAsync();
        var zeile = ZeileMit("""
            "index_datum":"2026-04-03",
            "segmente":[{"segmenttyp_id":"plan_number","wert":"5998-100"},{"segmenttyp_id":"geschoss","wert":"EG"}],
            "tags":["Decke","decke","Scan"]
            """);

        var r = Pruefer().Pruefe(auftrag, Entwurf(zeile), _analyse);

        Assert.True(r.IstGueltig, r.FehlerText);
        var z = Assert.Single(r.GueltigeZeilen);
        Assert.StartsWith("2026-04-03T00:00:00", z.IndexDatum);
        Assert.Equal(["plannummer", "geschoss"], z.Segmente!.Select(s => s.TokenKey));
        Assert.Equal(["Decke", "Scan"], z.Tags);
        Assert.Equal("Index-Datum 03.04.2026 · Plannummer: 5998-100, Geschoss: EG · Tags: Decke, Scan", z.Details);

        var p = ClaudeSortUebernahme.ZuPending(z);
        Assert.Equal(z.IndexDatum, p.ReleasedAt);
        Assert.Equal(2, p.AssignedSegments!.Count);
        Assert.Equal(["Decke", "Scan"], p.Tags);
        Assert.Equal(z.Details, p.Suggestion?.Details);
    }

    [Theory]
    [InlineData("\"segmente\":[{\"segmenttyp_id\":\"haus\",\"wert\":\"EG\"}]", "gibt es nicht")]
    [InlineData("\"segmente\":[{\"segmenttyp_id\":\"ignore\",\"wert\":\"EG\"}]", "gibt es nicht")]
    [InlineData("\"segmente\":[{\"segmenttyp_id\":\"geschoss\",\"wert\":\"OG3\"}]", "steht nicht im Dateinamen")]
    [InlineData("\"segmente\":[{\"segmenttyp_id\":\"geschoss\",\"wert\":\"EG\"},{\"segmenttyp_id\":\"geschoss\",\"wert\":\"5998\"}]", "mehrfach")]
    [InlineData("\"index_datum\":\"03.04.2026\"", "kein Datum")]
    [InlineData("\"index_datum\":\"2099-01-01\"", "Zukunft")]
    [InlineData("\"tags\":[\"a\",\"b\",\"c\",\"d\",\"e\",\"f\"]", "höchstens 5")]
    public async Task Pruefer_InventedOrInvalidExtras_AreRejected(string extras, string erwartet)
    {
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag, Entwurf(ZeileMit(extras)), _analyse);

        Assert.Contains(r.Fehler, f => f.Contains(erwartet));
    }

    [Fact]
    public async Task Import_ClaudeSuggestion_WritesDateSegmentsTagsAndOrigin()
    {
        var auftrag = await ErstelleAuftragAsync();
        var zeile = ZeileMit("""
            "index_datum":"2026-04-03",
            "segmente":[{"segmenttyp_id":"geschoss","wert":"EG"}],
            "tags":["Scan"]
            """);
        var z = Assert.Single(Pruefer().Pruefe(auftrag, Entwurf(zeile), _analyse).GueltigeZeilen);
        var pending = ClaudeSortUebernahme.ZuPending(z);

        var decisions = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer());
        var file = Assert.Single(decisions).File;
        Assert.True(file.FromClaudeSuggestion);
        Assert.Equal(["Scan"], file.Tags);

        var result = new ImportExecutionService(_planDb, new UlidIdGenerator(), _fs, _fs, _fs).Execute(decisions, Root, "_Eingang");

        Assert.Equal(0, result.Failed);
        var doc = Assert.Single(_planDb.GetCurrentDocumentLookup());
        Assert.Equal(["Scan"], _planDb.GetTagsForDocument(doc.DocumentId));
        var ereignis = Assert.Single(_planDb.GetRevisionEvents(doc.CurrentRevisionId));
        Assert.Contains("Vorschlag von Claude", ereignis.Note);
    }
}

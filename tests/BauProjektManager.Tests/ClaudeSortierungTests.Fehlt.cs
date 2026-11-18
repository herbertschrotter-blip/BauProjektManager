using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// "Mit Claude sortieren": Schnellanlage fehlender Stammdaten (Mockup 3.4 v1.3, ADR-067 E11) — Claude schlaegt vor,
/// Herbert legt in der Karte an, Claude bekommt die neuen IDs.
/// </summary>
public partial class ClaudeSortierungTests
{
    private static string Fehlt(string fehlt, string dateiId = "D-002")
        => $$"""{"datei_id":"{{dateiId}}","grund":"Bauteil fehlt","fehlt":{{fehlt}}}""";

    private ClaudeSortSchnellanlage Schnellanlage()
        => new(_bpmDb, new DocumentTypeCreationService(_bpmDb, new PlanValueNormalizer()));

    [Fact]
    public async Task Pruefer_FehltBauteil_IsSuggestion_NotAnError()
    {
        var auftrag = await ErstelleAuftragAsync();
        var json = Entwurf(PolierplanZeile(),
            Fehlt("""{"art":"bauteil","name":"TG","beschreibung":"Tiefgarage","geschosse":["UG2","UG1","ug2"]}"""));

        var r = Pruefer().Pruefe(auftrag, json, _analyse);

        Assert.True(r.IstGueltig, r.FehlerText);
        var f = Assert.Single(r.NichtZuordenbar).Fehlt!;
        Assert.Equal(ClaudeSortFehltArt.Bauteil, f.Art);
        Assert.Equal("TG", f.Name);
        Assert.Equal("Tiefgarage", f.Beschreibung);
        Assert.Equal(["UG2", "UG1"], f.Geschosse);
    }

    [Theory]
    [InlineData("""{"art":"bauteil","name":"haus a"}""", "gibt es schon")]
    [InlineData("""{"art":"geschoss","name":"OG3","bauteil_id":"gibts-nicht"}""", "vorhandenen Bauteils")]
    [InlineData("""{"art":"geschoss","name":"eg","bauteil_id":"PART"}""", "gibt es bei Haus A schon")]
    [InlineData("""{"art":"dokumenttyp","name":"Polierplan"}""", "gibt es schon")]
    [InlineData("""{"art":"dokumenttyp","name":"Neu","unterteilung":"ordner"}""", "erlaubt: keine")]
    [InlineData("""{"art":"kategorie","name":"Brandschutz"}""", "erlaubt: bauteil, geschoss, dokumenttyp")]
    [InlineData("""{"art":"bauteil","name":"TG","farbe":"rot"}""", "unbekanntes Feld 'farbe'")]
    public async Task Pruefer_FehltInvalidOrExisting_IsRejected(string fehlt, string erwartet)
    {
        var auftrag = await ErstelleAuftragAsync();

        var r = Pruefer().Pruefe(auftrag, Entwurf(PolierplanZeile(), Fehlt(fehlt.Replace("PART", _partId))), _analyse);

        Assert.Contains(r.Fehler, f => f.Contains(erwartet));
        Assert.Null(Assert.Single(r.NichtZuordenbar).Fehlt);
    }

    [Fact]
    public void Schnellanlage_Bauteil_WithLevelsAndDescription_ReturnsIdsForClaude()
    {
        var anlage = new ClaudeSortFehlt(ClaudeSortFehltArt.Bauteil, " TG ", "Tiefgarage", null, ["UG2", "UG1"], Ring2Source.None);

        var r = Schnellanlage().Anlegen(_projectId, anlage);

        Assert.True(r.Ok, r.Anzeige);
        var part = _bpmDb.GetBuildingParts(_projectId).Single(p => p.ShortName == "TG");
        Assert.Equal("Tiefgarage", part.Description);
        Assert.Equal(["UG2", "UG1"], part.Levels.Select(l => l.Name));
        Assert.Contains($"bauteil_id {part.Id}", r.FuerClaude);
        Assert.Contains($"UG1 (geschoss_id {part.Levels[1].Id})", r.FuerClaude);
        Assert.Equal("Bauteil TG „Tiefgarage“ mit Geschossen UG2, UG1 angelegt", r.Anzeige);

        Assert.False(Schnellanlage().Anlegen(_projectId, anlage).Ok); // gibt es jetzt schon
    }

    [Fact]
    public void Schnellanlage_GeschossAndDokumenttyp()
    {
        var geschoss = Schnellanlage().Anlegen(_projectId,
            new ClaudeSortFehlt(ClaudeSortFehltArt.Geschoss, "OG3", "3. Obergeschoss", _partId, [], Ring2Source.None));
        var typ = Schnellanlage().Anlegen(_projectId,
            new ClaudeSortFehlt(ClaudeSortFehltArt.Dokumenttyp, "Statik-Berechnung", null, null, [], Ring2Source.None));

        Assert.True(geschoss.Ok, geschoss.Anzeige);
        var level = _bpmDb.GetBuildingParts(_projectId).Single(p => p.Id == _partId).Levels.Single(l => l.Name == "OG3");
        Assert.Equal("3. Obergeschoss", level.Description);
        Assert.True(typ.Ok, typ.Anzeige);
        var types = _bpmDb.GetDocumentTypes(_projectId);
        var neu = types.Single(t => t.Name == "Statik-Berechnung");
        Assert.Equal(Ring2Source.None, neu.Ring2Source);
        Assert.Equal(ClaudeSortSchnellanlage.AblageFuer(types.Where(t => t.Id != neu.Id).ToList()), neu.RootRelativePath);
        Assert.Contains($"dokumenttyp_id {neu.Id}", typ.FuerClaude);
        Assert.False(Schnellanlage().Anlegen(_projectId,
            new ClaudeSortFehlt(ClaudeSortFehltArt.Geschoss, "X", null, null, [], Ring2Source.None)).Ok);
    }

    [Fact]
    public async Task Builder_KontextAktualisieren_AddsNewMasterData_KeepsInputFiles()
    {
        var auftrag = await ErstelleAuftragAsync();
        var vorher = LeseKontext().GetProperty("eingangsdateien").GetRawText();
        Schnellanlage().Anlegen(_projectId,
            new ClaudeSortFehlt(ClaudeSortFehltArt.Bauteil, "TG", null, null, ["UG1"], Ring2Source.None));

        Builder().KontextAktualisieren(auftrag);

        var k = LeseKontext();
        Assert.Contains(k.GetProperty("bauteile").EnumerateArray(), b => b.GetProperty("kurzname").GetString() == "TG");
        Assert.Equal(vorher, k.GetProperty("eingangsdateien").GetRawText());
    }

    [Fact]
    public async Task Chat_Fehlt_ShowsCard_CreateSendsIdsToClaude()
    {
        var claude = new FakeAssistant();
        var host = new FakeHost(_analyse) { ProjectId = _projectId, BuildingParts = _bpmDb.GetBuildingParts(_projectId) };
        var vm = new ClaudeSortViewModel(claude, Builder(), Pruefer(), host, () => true, JobRoot, Schnellanlage());
        await vm.StartCommand.ExecuteAsync(null);
        SchreibeEntwurf(claude, Entwurf(PolierplanZeile(),
            Fehlt("""{"art":"bauteil","name":"TG","beschreibung":"Tiefgarage","geschosse":["UG1"]}""")));

        await vm.NachRundeAsync();

        var karte = Assert.Single(vm.Entries, e => e.Kind == ClaudeChatEntryKind.Missing).Fehlt!;
        Assert.Equal("Bauteil „Tiefgarage“ anlegen?", karte.Titel);
        Assert.Equal(["D-002"], karte.DateiIds);
        Assert.Equal(ClaudeRowMarkKind.Missing, host.Marks[Path.Combine("_Eingang", "scan_001.pdf")].Kind);
        Assert.Contains("1 fehlt im Projekt", vm.DraftSummary);
        await vm.NachRundeAsync(); // gleicher Vorschlag erneut: keine zweite offene Karte
        Assert.Single(vm.Entries, e => e.Kind == ClaudeChatEntryKind.Missing);

        karte.GeschosseText = "UG2, UG1";
        await vm.CreateMissingCommand.ExecuteAsync(karte);

        Assert.True(karte.IsDone);
        Assert.Equal(1, host.StammdatenGeladen);
        var tg = _bpmDb.GetBuildingParts(_projectId).Single(p => p.ShortName == "TG");
        Assert.Equal(2, tg.Levels.Count);
        Assert.Contains($"bauteil_id {tg.Id}", claude.Session.Sent[^1]);
        Assert.Contains("D-002", claude.Session.Sent[^1]);
        Assert.Contains(LeseKontextVon(claude).GetProperty("bauteile").EnumerateArray(),
            b => b.GetProperty("id").GetString() == tg.Id);

    }

    [Fact]
    public async Task Chat_Fehlt_SkipAndDuplicateName()
    {
        var claude = new FakeAssistant();
        var host = new FakeHost(_analyse) { ProjectId = _projectId };
        var vm = new ClaudeSortViewModel(claude, Builder(), Pruefer(), host, () => true, JobRoot, Schnellanlage());
        await vm.StartCommand.ExecuteAsync(null);
        SchreibeEntwurf(claude, Entwurf(PolierplanZeile(), Fehlt("""{"art":"bauteil","name":"TG"}""")));
        await vm.NachRundeAsync();
        var karte = vm.Entries.Single(e => e.Kind == ClaudeChatEntryKind.Missing).Fehlt!;
        var gesendet = claude.Session.Sent.Count;

        vm.Phase = ClaudeSortPhase.Running;
        await vm.CreateMissingCommand.ExecuteAsync(karte);
        Assert.Contains("arbeitet noch", karte.ErgebnisText);
        Assert.DoesNotContain(_bpmDb.GetBuildingParts(_projectId), p => p.ShortName == "TG");
        vm.Phase = ClaudeSortPhase.Waiting;

        karte.Name = "Haus A";
        await vm.CreateMissingCommand.ExecuteAsync(karte);
        Assert.False(karte.IsDone);
        Assert.Contains("gibt es schon", karte.ErgebnisText);

        vm.SkipMissingCommand.Execute(karte);
        Assert.True(karte.IsDone);
        Assert.Equal("nicht angelegt", karte.ErgebnisText);
        Assert.Equal(gesendet, claude.Session.Sent.Count);
        Assert.Contains(vm.Entries, e => e.Kind == ClaudeChatEntryKind.System && e.Text.Contains("D-002 bleibt ohne Vorschlag"));
    }

    private System.Text.Json.JsonElement LeseKontextVon(FakeAssistant claude)
        => System.Text.Json.JsonDocument.Parse(_fs.ReadAllText(
            Path.Combine(claude.Request!.WorkingDirectory, ClaudeSortArten.KontextDatei))).RootElement;
}

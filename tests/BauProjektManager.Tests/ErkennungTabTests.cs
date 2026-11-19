using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.Tests;

/// <summary>
/// Tab „Erkennung“ (Seitenliste 3.6 v2.1, BPM-121.00): Status je Dokumenttyp, Regex-Regel mit Pruefung und Live-Test,
/// Loeschen nur der Erkennung, Wizard mit vorgewaehltem Typ und vorhandenem Profil.
/// </summary>
public class ErkennungTabTests
{
    private sealed class FakeProfileManager(params RecognitionProfile[] vorhanden) : IProfileManager
    {
        public RecognitionProfile? Saved { get; private set; }
        public string? DeletedId { get; private set; }
        public Exception? Fehler { get; init; }
        public List<RecognitionProfile> LoadAll(string projectRootPath) => [.. vorhanden];
        public RecognitionProfile? LoadById(string projectRootPath, string profileId) => null;

        public bool Delete(string projectRootPath, string profileId)
        {
            DeletedId = profileId;
            return true;
        }

        public void Save(string projectRootPath, RecognitionProfile profile)
        {
            if (Fehler is not null)
                throw Fehler;
            Saved = profile;
        }
    }

    private static readonly PlanDocumentType Bewehrung =
        new("TYP-B", "Bewehrungsplan", "02 Bewehrungsplaene", null, Ring2Source.BuildingParts, 10, true, []);

    private static RecognitionProfile Profil(params RecognitionRule[] regeln) => new()
    {
        Id = "P-1",
        DocumentTypeId = "TYP-B",
        DocumentTypeName = "Bewehrungsplan",
        Recognition = [.. regeln]
    };

    private static RecognitionRule Segment(string pattern) => new() { Method = "segment", Pattern = pattern };

    private static RecognitionRule Regex(string pattern) => new() { Method = "regex", Pattern = pattern };

    // ── Status je Dokumenttyp ────────────────────────────────────

    [Fact]
    public void Row_WithoutProfile_IsNotLearned()
    {
        var row = new DocumentTypeRow(Bewehrung);

        Assert.Equal(RecognitionStatus.NotLearned, row.Status);
        Assert.False(row.HasProfile);
        Assert.Equal("○ Nicht angelernt", row.StatusText);
        Assert.Equal("", row.RuleText);
    }

    [Fact]
    public void Row_ValidProfile_IsActive_AndListsRules()
    {
        var row = new DocumentTypeRow(Bewehrung, Profil(Segment("Bewehrung"), Regex(@"^B-\d+")));

        Assert.Equal(RecognitionStatus.Active, row.Status);
        Assert.Equal(@"Erkennung: enthält „Bewehrung“ · Regex ^B-\d+", row.RuleText);
    }

    [Fact]
    public void Row_MissingSegmentTypes_IsDefect_AndNamesTheMissingType()
    {
        var profil = Profil(Segment("Schalung"));
        profil.Health = ProfileHealth.MissingSegmentTypes;
        profil.MissingSegmentTypeIds = ["haus"];

        var row = new DocumentTypeRow(Bewehrung, profil);

        Assert.Equal(RecognitionStatus.Defect, row.Status);
        Assert.Equal("● Defekt – Segmenttyp „haus“ fehlt", row.StatusText);
    }

    [Fact]
    public void DeleteRecognition_RemovesOnlyTheProfile()
    {
        var pm = new FakeProfileManager();
        var vm = new DocumentTypeOverviewViewModel("P1", null, pm, @"C:\bpm-erkennung-test-nicht-da");

        Assert.True(vm.DeleteRecognition(new DocumentTypeRow(Bewehrung, Profil(Segment("Bewehrung")))));
        Assert.Equal("P-1", pm.DeletedId);
        Assert.False(vm.DeleteRecognition(new DocumentTypeRow(Bewehrung))); // ohne Erkennung nichts zu loeschen
    }

    // ── Regex-Regel ──────────────────────────────────────────────

    [Fact]
    public void Regex_InvalidPattern_ShowsError_AndBlocksSave()
    {
        var vm = new RegexRuleViewModel(new FakeProfileManager(), "C:\\x", Profil(Segment("B")), "B-101_Bewehrung_OG1.pdf")
        {
            Pattern = @"^(B-\d+"
        };

        Assert.StartsWith("Ungültiges Regex-Pattern:", vm.ValidationError);
        Assert.False(vm.CanSave);
        Assert.False(vm.Save());
    }

    [Theory]
    [InlineData(@"^B-\d{3}", true)]
    [InlineData(@"OG1$", true)]            // gegen den Namen ohne Endung
    [InlineData(@"\.pdf$", false)]         // die Endung gehoert nicht dazu
    [InlineData(@"^S-", false)]
    public void Regex_LiveTest_ChecksTheNameWithoutExtension(string pattern, bool treffer)
    {
        var vm = new RegexRuleViewModel(new FakeProfileManager(), "C:\\x", Profil(Segment("B")), "B-101_Bewehrung_OG1.pdf")
        {
            Pattern = pattern
        };

        Assert.True(vm.CanSave);
        Assert.Equal(treffer, vm.TestSuccess);
        Assert.Equal(treffer ? "✓ Treffer bei „B-101_Bewehrung_OG1.pdf“" : "Kein Treffer bei „B-101_Bewehrung_OG1.pdf“",
            vm.TestResult);
    }

    [Fact]
    public void Regex_Save_ReplacesTheRegexRule_KeepsWizardRules()
    {
        var pm = new FakeProfileManager();
        var vm = new RegexRuleViewModel(pm, "C:\\x", Profil(Segment("Bewehrung"), Regex("^ALT")), "")
        {
            Pattern = @"^B-\d+"
        };
        Assert.Equal("Regex ^ALT", vm.ExistingRuleText);

        Assert.True(vm.Save());

        Assert.Equal([("segment", "Bewehrung"), ("regex", @"^B-\d+")],
            pm.Saved!.Recognition.Select(r => (r.Method, r.Pattern)));
    }

    [Fact]
    public void Regex_SaveError_IsShown()
    {
        var pm = new FakeProfileManager { Fehler = new InvalidOperationException("Profil ist ungueltig: x") };
        var vm = new RegexRuleViewModel(pm, "C:\\x", Profil(Segment("B")), "") { Pattern = "^B" };

        Assert.False(vm.Save());
        Assert.Contains("Profil ist ungueltig", vm.SaveError);
    }

    [Fact]
    public void Recognizer_Regex_MatchesTheNameWithoutExtension()
    {
        var profil = Profil(Regex(@"OG1$"));
        profil.Tokenization = new TokenizationConfig();

        var result = new DocumentTypeRecognizer().Recognize("B-101_Bewehrung_OG1.pdf", [profil]);

        Assert.Same(profil, result.MatchedProfile);
    }

    // ── Wizard aus dem Tab: anlernen / bearbeiten ─────────────────

    private static ProfileWizardViewModel Wizard(FakeProfileManager pm)
    {
        var project = new Project { Id = "P1", Name = "Test", Paths = new ProjectPaths { Root = @"C:\bpm-erkennung-test-nicht-da" } };
        var vm = new ProfileWizardViewModel(project, pm) { DocumentTypeOptions = [Bewehrung] };
        return vm;
    }

    [Fact]
    public void Wizard_Preselect_SetsTheDocumentType_AndTakesOverIndexSettings()
    {
        var vorhanden = Profil(Segment("Bewehrung"));
        vorhanden.IndexSource = IndexSourceType.None;
        vorhanden.IndexComparison.CaseInsensitive = false;
        var vm = Wizard(new FakeProfileManager(vorhanden));

        vm.Preselect("TYP-B");

        Assert.Same(Bewehrung, vm.SelectedDocumentType);
        Assert.Equal(IndexSourceType.None, vm.SelectedIndexSource);
        Assert.False(vm.IndexCaseInsensitive);
        Assert.Contains("wird beim Speichern ersetzt", vm.ExistingProfileNote);
    }

    [Fact]
    public void Wizard_Edit_AssignsExistingSegmentTypesByPosition()
    {
        var vorhanden = Profil(Segment("Bewehrung"));
        vorhanden.Tokenization = new TokenizationConfig { Delimiters = ["_"] };
        vorhanden.Segments =
        [
            new ProfileSegment { Position = 0, FieldTypeId = "plan_number" },
            new ProfileSegment { Position = 1, FieldTypeId = "planart" },
            new ProfileSegment { Position = 2, FieldTypeId = "geschoss" }
        ];
        var vm = Wizard(new FakeProfileManager(vorhanden));
        vm.Preselect("TYP-B");

        vm.SampleFileName = "B-205_Bewehrung_EG.pdf";
        Assert.Equal([("plan_number", "B-205"), ("planart", "Bewehrung"), ("geschoss", "EG")],
            vm.ExistingEditorAssignments()!.Select(a => (a.SegmentTypeId, a.RawValue)));

        vm.SampleFileName = "B-205_Bewehrung.pdf"; // anders aufgebaut → Editor startet leer
        Assert.Null(vm.ExistingEditorAssignments());
    }
}

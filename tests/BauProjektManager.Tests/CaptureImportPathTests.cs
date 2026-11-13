using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// R01-007 (Audit Review 01): der manuelle V1-Importpfad schreibt korrekte Felder und
/// verwirft keine Eingaben stumm — Bucket D nicht per Radial (K14-03), MD5-Pflicht
/// (LU4-01), echte Dokumenttyp-ID bei Update-Uebernahmen (PM-04), Text-Zuweisung nur
/// fuer wirksame Felder (K28-03).
/// </summary>
public class CaptureImportPathTests
{
    private static CaptureRowViewModel Row(CaptureBucket bucket, KnownPlanDocument? match = null)
    {
        var scan = new ScannedFile("_Eingang/5998-100_EG.pdf", "5998-100_EG.pdf", ".pdf", 100, DateTime.UtcNow);
        var candidates = new PlanFileCandidates(
            scan.FileName, "5998-100", Index: null, RevisionKind.None,
            Level: null, BuildingPartHint: null, TypeKeywords: [],
            DateCandidate: null, HasCopyMarker: false, IsCombi: false);
        return new CaptureRowViewModel(new CaptureItem(
            new FingerprintedFile(scan, "md5-a"), candidates, bucket, match, Reason: null));
    }

    private static KnownPlanDocument Doc() =>
        new("doc-ulid", "polierplan|5998_100|bt1", "5998-100", "Polierplan", "Pläne",
            "Pläne/Polierplan/Haus 1", "A", "rev1", "polierplan", "bt1", "gs1");

    // ── 007.01 / K14-03: nur Bucket C oeffnet das Radial ──

    [Theory]
    [InlineData(CaptureBucket.NewCapture, true)]
    [InlineData(CaptureBucket.Duplicate, false)]
    [InlineData(CaptureBucket.UpdateProposal, false)]
    [InlineData(CaptureBucket.Conflict, false)]
    public void OnlyBucketC_OpensRadial(CaptureBucket bucket, bool expected)
        => Assert.Equal(expected, Row(bucket).CanOpenRadial);

    [Fact]
    public void Conflict_WithSingleMatch_CanBeTakenInPanel_WithoutMatch_Not()
    {
        Assert.True(new CaptureDetailViewModel(Row(CaptureBucket.Conflict, Doc()), []).CanTakeUpdate);
        Assert.False(new CaptureDetailViewModel(Row(CaptureBucket.Conflict), []).CanTakeUpdate);
        Assert.Equal("⬆ Als neue Revision übernehmen",
            new CaptureDetailViewModel(Row(CaptureBucket.Conflict, Doc()), []).TakeUpdateLabel);
    }

    // ── 007.02 / LU4-01: MD5-Pflicht ──

    [Fact]
    public void Classify_FingerprintError_IsConflictWithoutMatch()
    {
        var scan = new ScannedFile("_Eingang/5998-100_EG.pdf", "5998-100_EG.pdf", ".pdf", 100, DateTime.UtcNow);
        var result = ManualFirstCaptureService.Classify(
            [new FingerprintResult(new FingerprintedFile(scan, ""), "Datei gesperrt")],
            [], new Dictionary<string, string>(), new LightweightPlanExtractor(), new PlanValueNormalizer());

        var item = Assert.Single(result.Items);
        Assert.Equal(CaptureBucket.Conflict, item.Bucket);
        Assert.Null(item.Match);
        Assert.Contains("MD5", item.Reason);
    }

    [Fact]
    public void Execute_DecisionWithoutMd5_FailsWithoutJournal()
    {
        IIdGenerator idGen = new UlidIdGenerator();
        var projectId = idGen.NewId();
        var dbPath = TempDb.NewTempDbPath(projectId);
        var db = new PlanManagerDatabase(projectId, idGen, dbPathOverride: dbPath);
        try
        {
            var fake = new FakeFileStore();
            var root = Path.Combine(Path.GetTempPath(), "bpm-r01007-virtual");
            fake.AddFile(Path.Combine(root, "_Eingang", "5998-100_EG.pdf"));
            var scan = new ScannedFile(Path.Combine("_Eingang", "5998-100_EG.pdf"), "5998-100_EG.pdf",
                ".pdf", 12, DateTime.UtcNow);
            var pending = new PendingAssignment(new FingerprintedFile(scan, ""), CaptureBucket.NewCapture,
                "polierplan", "Polierplan", null, null, "5998-100", null, "Pläne/Polierplan", Match: null);
            var decisions = CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer());

            var result = new ImportExecutionService(db, idGen, fake, fake, fake).Execute(decisions, root, "_Eingang");

            Assert.Equal(1, result.Failed);
            Assert.Contains("MD5", Assert.Single(result.Errors));
            Assert.False(db.HasPendingImports());
            Assert.True(fake.FileExists(Path.Combine(root, "_Eingang", "5998-100_EG.pdf")));
        }
        finally
        {
            db.Dispose();
            using (var pc = new SqliteConnection($"Data Source={dbPath}"))
                SqliteConnection.ClearPool(pc);
            TempDb.Delete(dbPath);
        }
    }

    // ── 007.04 / PM-04: Update-Uebernahme journalisiert die echte Dokumenttyp-ID ──

    [Fact]
    public void UpdateDecision_CarriesDocumentTypeId_NotDocumentId()
    {
        var doc = Doc();
        var scan = new ScannedFile("_Eingang/5998-100-B_EG.pdf", "5998-100-B_EG.pdf", ".pdf", 12, DateTime.UtcNow);
        // Spiegelt ManualCaptureViewModel.AssignUpdate (R01-007.04)
        var pending = new PendingAssignment(new FingerprintedFile(scan, "md5-b"), CaptureBucket.UpdateProposal,
            doc.DocumentTypeId ?? "", doc.DocumentType, null, null, doc.PlanNumber, "B",
            doc.RelativeDirectory, doc, BuildingPartId: doc.BuildingPartId, BuildingLevelId: doc.BuildingLevelId);

        var decision = Assert.Single(CaptureConfirmService.BuildDecisions([pending], new PlanValueNormalizer()));

        Assert.Equal("polierplan", decision.File.DocumentTypeId);
        Assert.NotEqual(doc.DocumentId, decision.File.DocumentTypeId);
        Assert.Equal("bt1", decision.File.BuildingPartId);
        Assert.Equal(doc.DocumentKey, decision.DocumentKey);
    }

    // ── K28-03: Text-Zuweisung nur fuer wirksame Felder ──

    [Fact]
    public void TextAssignment_RejectedForDuplicates_AndDescriptionOnUpdates()
    {
        Assert.NotNull(ManualCaptureViewModel.TextAssignmentRejection(Row(CaptureBucket.Duplicate), false));
        Assert.NotNull(ManualCaptureViewModel.TextAssignmentRejection(Row(CaptureBucket.UpdateProposal), true));
        Assert.Null(ManualCaptureViewModel.TextAssignmentRejection(Row(CaptureBucket.UpdateProposal), false));
        Assert.Null(ManualCaptureViewModel.TextAssignmentRejection(Row(CaptureBucket.NewCapture), true));
    }
}

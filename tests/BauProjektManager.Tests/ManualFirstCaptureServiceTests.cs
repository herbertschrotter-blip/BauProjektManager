using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer die reine Bucket-Klassifikation des <see cref="ManualFirstCaptureService"/>
/// (BPM-111.03, ADR-059). Ohne DB/Dateisystem — synthetische Fingerprints +
/// bekannte Dokumente. Mapping auf die Entscheidungsmatrix (PlanManager.md Kap. 5):
/// A=SKIP_IDENTICAL, B=UPDATE_NEWER_INDEX/OLDER_REVISION, C=NEW/UNKNOWN, D=CONFLICT/CHANGED_SAME_INDEX.
/// </summary>
public class ManualFirstCaptureServiceTests
{
    private static readonly LightweightPlanExtractor _extractor = new();
    private static readonly PlanValueNormalizer _normalizer = new();

    private static FingerprintResult Fp(string fileName, string md5 = "md5-x") =>
        new(new FingerprintedFile(
            new ScannedFile($"_Eingang/{fileName}", fileName,
                System.IO.Path.GetExtension(fileName), 100, DateTime.UtcNow), md5), null);

    private static KnownPlanDocument Doc(
        string nr, string? idx, string key = "k1", string id = "doc1") =>
        new(id, key, nr, "Polierplan", "Polierplan", $"Pläne/Polierplan/Haus 1/", idx, "rev1");

    private static ManualCaptureResult Run(
        List<FingerprintResult> files,
        List<KnownPlanDocument>? docs = null,
        Dictionary<string, string>? md5 = null) =>
        ManualFirstCaptureService.Classify(
            files, docs ?? [], md5 ?? [], _extractor, _normalizer);

    [Fact]
    public void Classify_KnownMd5_BucketA_Duplicate()
    {
        var doc = Doc("5998-100", "A");
        var r = Run([Fp("irgendwas.pdf", "abc123")],
            [doc], new Dictionary<string, string> { ["abc123"] = "k1" });

        var item = Assert.Single(r.Items);
        Assert.Equal(CaptureBucket.Duplicate, item.Bucket);
        Assert.Equal(doc, item.Match);
    }

    [Fact]
    public void Classify_KnownPlanNewIndex_BucketB_UpdateProposal()
    {
        var r = Run([Fp("5998-100-B_KG_Polierplan.pdf")], [Doc("5998-100", "A")]);

        var item = Assert.Single(r.Items);
        Assert.Equal(CaptureBucket.UpdateProposal, item.Bucket);
        Assert.NotNull(item.Match);
        Assert.Null(item.Reason);
    }

    [Fact]
    public void Classify_KnownPlanLowerIndex_BucketB_WithOlderWarning()
    {
        // Eingang bringt B, aber C liegt schon -> OLDER_REVISION-Warnung
        var r = Run([Fp("5998-100-B_KG_Polierplan.pdf")], [Doc("5998-100", "C")]);

        var item = Assert.Single(r.Items);
        Assert.Equal(CaptureBucket.UpdateProposal, item.Bucket);
        Assert.Contains("OLDER_REVISION", item.Reason);
    }

    [Fact]
    public void Classify_KnownPlanSameIndex_BucketD_Conflict()
    {
        // Gleicher Index, anderer Inhalt (MD5 unbekannt) -> CHANGED_SAME_INDEX
        var r = Run([Fp("5998-100-B_KG_Polierplan.pdf")], [Doc("5998-100", "B")]);

        var item = Assert.Single(r.Items);
        Assert.Equal(CaptureBucket.Conflict, item.Bucket);
        Assert.Contains("Gleicher Index", item.Reason);
    }

    [Fact]
    public void Classify_PlanNumberInMultipleDocuments_BucketD_Conflict()
    {
        var docs = new List<KnownPlanDocument>
        {
            Doc("103", "A", key: "polierplan|103|h1", id: "d1"),
            Doc("103", null, key: "schalung|103|h2", id: "d2")
        };
        var r = Run([Fp("103_EG.pdf")], docs);

        var item = Assert.Single(r.Items);
        Assert.Equal(CaptureBucket.Conflict, item.Bucket);
        Assert.Contains("2 bekannten Dokumenten", item.Reason);
    }

    [Fact]
    public void Classify_UnknownPlanNumber_BucketC_NewCapture()
    {
        var r = Run([Fp("5998-999_OG1_Polierplan.pdf")], [Doc("5998-100", "A")]);

        Assert.Equal(CaptureBucket.NewCapture, Assert.Single(r.Items).Bucket);
    }

    [Fact]
    public void Classify_NoPlanNumberCandidate_BucketC_NewCapture()
    {
        // Protokoll ohne Plannummer -> manuelle Erstaufnahme (Radial)
        var r = Run([Fp("Baubesprechung_Notizen.pdf")], [Doc("5998-100", "A")]);

        var item = Assert.Single(r.Items);
        Assert.Equal(CaptureBucket.NewCapture, item.Bucket);
        Assert.Null(item.Match);
    }

    [Fact]
    public void Classify_PlanNumberMatch_IsSeparatorTolerant()
    {
        // Normalizer-Matching: "5998-100" im Dateinamen vs. "5998 100" im Bestand
        var r = Run([Fp("5998-100-B_KG.pdf")], [Doc("5998 100", "A")]);

        Assert.Equal(CaptureBucket.UpdateProposal, Assert.Single(r.Items).Bucket);
    }

    [Fact]
    public void Classify_Md5WinsOverPlanNumberMatch()
    {
        // Bucket-Reihenfolge: A (MD5) hat Vorrang vor B
        var r = Run([Fp("5998-100-B_KG.pdf", "dup-md5")],
            [Doc("5998-100", "A")],
            new Dictionary<string, string> { ["dup-md5"] = "k1" });

        Assert.Equal(CaptureBucket.Duplicate, Assert.Single(r.Items).Bucket);
    }

    [Fact]
    public void Classify_MixedInbox_CountsPerBucket()
    {
        var docs = new List<KnownPlanDocument> { Doc("5998-100", "A") };
        var md5 = new Dictionary<string, string> { ["dup"] = "k1" };
        var r = Run(
        [
            Fp("kopie.pdf", "dup"),                      // A
            Fp("5998-100-B_KG_Polierplan.pdf", "m1"),    // B
            Fp("5998-200_EG_Polierplan.pdf", "m2"),      // C
            Fp("BB_2026-04-15_Baubesprechung.pdf", "m3") // C
        ], docs, md5);

        Assert.Equal(1, r.DuplicateCount);
        Assert.Equal(1, r.UpdateProposalCount);
        Assert.Equal(2, r.NewCaptureCount);
        Assert.Equal(0, r.ConflictCount);
        Assert.Equal(4, r.TotalFiles);
    }
}

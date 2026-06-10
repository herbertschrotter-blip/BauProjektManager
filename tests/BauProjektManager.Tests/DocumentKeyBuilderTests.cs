using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Regressionstests fuer <see cref="DocumentKeyBuilder"/> (BPM-110 Feldkey-Fix).
/// ExtractedFields und IdentityFields sind beide mit <c>segment_types.id</c> gekeyt
/// (Built-in snake_case via <see cref="SegmentTypeIds"/>, Custom ULID) — der Builder
/// muss verbatim nachschlagen, keine Key-Umformung.
/// </summary>
public class DocumentKeyBuilderTests
{
    private readonly DocumentKeyBuilder _sut = new();

    [Fact]
    public void Build_NoProfile_ReadsPlanNumberViaSegmentTypeId()
    {
        // Vor BPM-110 las der Fallback "plannumber" (toter Key) → key=null
        var fields = new Dictionary<string, string> { [SegmentTypeIds.PlanNumber] = "011" };

        var key = _sut.Build("polierplan", fields, profile: null);

        Assert.Equal("polierplan|011", key);
    }

    [Fact]
    public void Build_Profile_SnakeCaseIdentityFields_AreResolved()
    {
        var profile = new RecognitionProfile
        {
            IdentityFields =
                [SegmentTypeIds.DocumentTypeField, SegmentTypeIds.PlanNumber, SegmentTypeIds.Haus]
        };
        var fields = new Dictionary<string, string>
        {
            [SegmentTypeIds.PlanNumber] = "011",
            [SegmentTypeIds.Haus] = "H2",
            [SegmentTypeIds.PlanIndex] = "B" // Index darf NIE in den Key
        };

        var key = _sut.Build("polierplan", fields, profile);

        Assert.Equal("polierplan|011|h2", key);
    }

    [Fact]
    public void Build_Profile_CustomUlidIdentityField_LookupIsVerbatim()
    {
        // Vor BPM-110 wurde der IdentityField-Name lowergecased → ULID-Keys
        // (Custom-Segmenttypen, Grossbuchstaben) wurden nie gefunden.
        const string customUlid = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var profile = new RecognitionProfile
        {
            IdentityFields = [SegmentTypeIds.DocumentTypeField, SegmentTypeIds.PlanNumber, customUlid]
        };
        var fields = new Dictionary<string, string>
        {
            [SegmentTypeIds.PlanNumber] = "011",
            [customUlid] = "Nord"
        };

        var key = _sut.Build("polierplan", fields, profile);

        Assert.Equal("polierplan|011|nord", key);
    }

    [Fact]
    public void Build_NoIdentityValues_ReturnsNull()
    {
        var key = _sut.Build("polierplan", new Dictionary<string, string>(), profile: null);

        Assert.Null(key);
    }

    [Fact]
    public void Build_NoDocumentTypeId_ReturnsNull()
    {
        var fields = new Dictionary<string, string> { [SegmentTypeIds.PlanNumber] = "011" };

        var key = _sut.Build(null, fields, profile: null);

        Assert.Null(key);
    }

    [Fact]
    public void DefaultIdentityFields_UseCentralConstants()
    {
        // RecognitionProfile-Default und SegmentTypeIds muessen synchron bleiben
        var profile = new RecognitionProfile();

        Assert.Equal([SegmentTypeIds.DocumentTypeField, SegmentTypeIds.PlanNumber], profile.IdentityFields);
    }
}

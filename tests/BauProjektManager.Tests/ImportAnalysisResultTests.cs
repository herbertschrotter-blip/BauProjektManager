using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests fuer <see cref="ImportAnalysisResult"/> + Health-Gating-Doku
/// (BPM-108 Phase C Teil 5).
/// </summary>
public class ImportAnalysisResultTests
{
    [Fact]
    public void Empty_HasZeroCounts()
    {
        var result = ImportAnalysisResult.Empty;

        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.UnhealthyProfileCount);
        Assert.Empty(result.UsedProfiles);
        Assert.Empty(result.UnhealthyProfiles);
    }

    [Fact]
    public void UnhealthyProfileCount_ReflectsListSize()
    {
        var unhealthy = new List<RecognitionProfile>
        {
            new() { Id = "U1", DocumentTypeName = "X", Health = ProfileHealth.MissingSegmentTypes,
                MissingSegmentTypeIds = ["plan_number"] },
            new() { Id = "U2", DocumentTypeName = "Y", Health = ProfileHealth.MissingSegmentTypes,
                MissingSegmentTypeIds = ["geschoss"] }
        };
        var result = new ImportAnalysisResult([], [], unhealthy);

        Assert.Equal(2, result.UnhealthyProfileCount);
        Assert.Equal("U1", result.UnhealthyProfiles[0].Id);
    }

    [Fact]
    public void HealthGating_ContractIsValid_NotMissingSegmentTypesIsImportable()
    {
        // Dokumentations-Smoke-Test: das Filter-Kriterium das ImportWorkflowService
        // verwendet ist `Health == Valid`. Hier verifizieren wir die Semantik der Enum-Werte.
        var valid = new RecognitionProfile { Health = ProfileHealth.Valid };
        var missing = new RecognitionProfile { Health = ProfileHealth.MissingSegmentTypes };

        Assert.True(valid.Health == ProfileHealth.Valid);
        Assert.False(missing.Health == ProfileHealth.Valid);
    }
}

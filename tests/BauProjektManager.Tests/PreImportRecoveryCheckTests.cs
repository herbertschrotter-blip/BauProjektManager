using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="PreImportRecoveryCheck.Evaluate"/>.
/// Pure Funktion — kein Setup nötig, keine Disk/DB. Siehe BPM-111.05 Slice 3d.
/// Gate vor dem manuellen "Import bestätigen": jeder pending Import blockiert.
/// </summary>
public class PreImportRecoveryCheckTests
{
    private readonly PreImportRecoveryCheck _sut = new();

    private static PendingImportInfo MakeInfo(string id)
        => new(
            Id: id,
            Timestamp: DateTime.UtcNow,
            SourcePath: "_Eingang",
            FileCount: 3,
            ProfileId: null,
            MachineName: "TEST-MACHINE",
            CompletedActions: 1,
            FailedActions: 0,
            PendingActions: 2);

    [Fact]
    public void Evaluate_NoPending_AllowsConfirm()
    {
        var result = _sut.Evaluate([]);

        Assert.True(result.CanConfirm);
        Assert.Empty(result.BlockingImports);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Evaluate_OnePending_BlocksConfirm()
    {
        var result = _sut.Evaluate([MakeInfo("imp-1")]);

        Assert.False(result.CanConfirm);
        Assert.Single(result.BlockingImports);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Evaluate_MultiplePending_BlocksConfirmWithAllListed()
    {
        var result = _sut.Evaluate([MakeInfo("imp-1"), MakeInfo("imp-2")]);

        Assert.False(result.CanConfirm);
        Assert.Equal(2, result.BlockingImports.Count);
        Assert.Contains("2", result.Message);
    }
}

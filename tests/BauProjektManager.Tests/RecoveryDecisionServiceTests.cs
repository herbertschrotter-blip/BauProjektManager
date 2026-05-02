using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Unit-Tests für <see cref="RecoveryDecisionService.Recommend"/>.
/// Pure Funktion — kein Setup nötig, keine Disk/DB. Siehe BPM-098 Stufe 1.
/// 5 Cases analog zu Docs/Test/Recovery-Szenarien.md.
/// </summary>
public class RecoveryDecisionServiceTests
{
    private readonly RecoveryDecisionService _sut = new();

    private static PendingImportInfo MakeInfo(int completed, int pending, int failed, int total = 5)
        => new(
            Id: "test-id",
            Timestamp: DateTime.UtcNow,
            SourcePath: "_Eingang",
            FileCount: total,
            ProfileId: null,
            MachineName: "TEST-MACHINE",
            CompletedActions: completed,
            FailedActions: failed,
            PendingActions: pending);

    [Fact]
    public void Recommend_Szenario1_AllPending_NoCompleted_ReturnsForwardAuto()
    {
        // 0 completed, 5 pending, 0 failed → IsRollbackTrivial → Forward + Auto
        var info = MakeInfo(completed: 0, pending: 5, failed: 0);

        var rec = _sut.Recommend(info);

        Assert.Equal(RecoveryAction.Forward, rec.Action);
        Assert.True(rec.IsAutomaticAllowed);
        Assert.Contains("sicher fortgesetzt", rec.Reason);
    }

    [Fact]
    public void Recommend_Szenario2_AllCompleted_NoPending_ReturnsForwardAuto()
    {
        // 5 completed, 0 pending, 0 failed → IsForwardTrivial → Forward + Auto
        var info = MakeInfo(completed: 5, pending: 0, failed: 0);

        var rec = _sut.Recommend(info);

        Assert.Equal(RecoveryAction.Forward, rec.Action);
        Assert.True(rec.IsAutomaticAllowed);
        Assert.Contains("Journal-Finalisierung", rec.Reason);
    }

    [Fact]
    public void Recommend_Szenario3_MixCompletedPending_ReturnsForwardManual()
    {
        // 2 completed, 3 pending, 0 failed → Forward, aber User-OK
        var info = MakeInfo(completed: 2, pending: 3, failed: 0);

        var rec = _sut.Recommend(info);

        Assert.Equal(RecoveryAction.Forward, rec.Action);
        Assert.False(rec.IsAutomaticAllowed);
        Assert.Contains("User-Bestätigung erforderlich", rec.Reason);
    }

    [Fact]
    public void Recommend_Szenario4_WithFailed_ReturnsCleanup()
    {
        // 2 completed, 2 pending, 1 failed → Cleanup (Mix-State, manuelle Untersuchung)
        var info = MakeInfo(completed: 2, pending: 2, failed: 1);

        var rec = _sut.Recommend(info);

        Assert.Equal(RecoveryAction.Cleanup, rec.Action);
        Assert.False(rec.IsAutomaticAllowed);
        Assert.Contains("fehlgeschlagene", rec.Reason);
    }

    [Fact]
    public void Recommend_EdgeCase_AllZero_ReturnsForwardAuto()
    {
        // 0 completed, 0 pending, 0 failed → IsRollbackTrivial trifft (CompletedActions==0)
        // → Forward + Auto. Theoretischer Edge-Case (sollte in Praxis nicht auftreten).
        // Forward ist defensiv ok: keine Disk-Operation weil 0 pending Actions.
        var info = MakeInfo(completed: 0, pending: 0, failed: 0, total: 0);

        var rec = _sut.Recommend(info);

        Assert.Equal(RecoveryAction.Forward, rec.Action);
        Assert.True(rec.IsAutomaticAllowed);
    }
}

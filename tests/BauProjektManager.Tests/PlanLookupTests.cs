using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.Tests;

/// <summary>
/// Tests für die IPlanLookupService-Public-API (BPM-109.05a): die berechnete Drei-Zeiten-Logik
/// im PlanLookupResult (EffectiveDate/IsDateFallback) + Fail-Fast des Stubs.
/// </summary>
public class PlanLookupTests
{
    private static PlanLookupResult Result(DateTime? releasedAt, DateTime receivedAt)
        => new(
            DocumentId: "doc1", DocumentKey: "polierplan|011|haus64|eg",
            PlanNumber: "011", DocumentType: "Polierplan", PlanIndex: "A",
            RevisionId: "rev1", PrimaryFileRelativePath: "Plans/x.pdf",
            ReleasedAt: releasedAt, ReceivedAt: receivedAt, CurrentFrom: receivedAt);

    [Fact]
    public void EffectiveDate_PrefersReleasedAt_WhenPresent()
    {
        var released = new DateTime(2025, 7, 14, 0, 0, 0, DateTimeKind.Utc);
        var received = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var r = Result(released, received);

        Assert.Equal(released, r.EffectiveDate);
        Assert.False(r.IsDateFallback);
    }

    [Fact]
    public void EffectiveDate_FallsBackToReceivedAt_WhenReleasedNull()
    {
        var received = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var r = Result(null, received);

        Assert.Equal(received, r.EffectiveDate);
        Assert.True(r.IsDateFallback);   // UI markiert Importdatum als Fallback
    }

    [Fact]
    public async Task Stub_FindCurrentPlans_ThrowsNotImplemented()
    {
        var svc = new PlanLookupService();
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            svc.FindCurrentPlansAsync("proj1", null, null, [], DateTime.UtcNow));
    }

    [Fact]
    public async Task Stub_CreatePlanContextSnapshot_ThrowsNotImplemented()
    {
        var svc = new PlanLookupService();
        var filter = new PlanContextFilter(null, null, []);
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            svc.CreatePlanContextSnapshotAsync("bautagebuch", "src1", DateTime.UtcNow, filter));
    }
}

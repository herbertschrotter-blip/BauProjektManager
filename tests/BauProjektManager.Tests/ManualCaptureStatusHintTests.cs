using System.IO;
using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using BauProjektManager.PlanManager.ViewModels;
using BauProjektManager.Tests.Fakes;
using Microsoft.Data.Sqlite;

namespace BauProjektManager.Tests;

/// <summary>
/// BPM-122: Bulk-Hinweise (⚠ Warnung / ⛔ Deckel aus <see cref="ManualCaptureViewModel.BeginCapture"/>)
/// gelten nur für die Auswahl, mit der das Radial gestartet wurde — bei
/// Auswahländerung (SetSelectedRow via SelectionChanged) kehrt die neutrale
/// Zusammenfassung zurück. Andere Statusmeldungen bleiben unberührt.
/// </summary>
public class ManualCaptureStatusHintTests : IDisposable
{
    private sealed class FakeUserContext : IUserContext
    {
        public string UserId => "TEST\\user";
        public string DisplayName => "Test User";
        public UserContextSource Source => UserContextSource.Local;
    }

    private sealed class FakeDeviceContext : IDeviceContext
    {
        public string DeviceId => "01TESTDEVICE";
        public string DeviceName => "TestDevice";
    }

    private readonly string _bpmDbPath;
    private readonly ProjectDatabase _bpmDb;
    private readonly PlanManagerDatabase _planDb;
    private readonly ManualCaptureViewModel _vm;

    public ManualCaptureStatusHintTests()
    {
        IIdGenerator idGen = new UlidIdGenerator();
        var projectId = idGen.NewId();
        // BPM-123: Test-DB unter %TEMP% via dbPathOverride — nie in LocalAppData\Projects.
        _planDb = new PlanManagerDatabase(projectId, idGen,
            dbPathOverride: TempDb.NewTempDbPath(projectId));
        _bpmDbPath = Path.Combine(Path.GetTempPath(), $"bpm-statushint-{Guid.NewGuid():N}.db");
        _bpmDb = new ProjectDatabase(new UlidIdGenerator(), new FakeUserContext(),
            new FakeDeviceContext(), persistenceRegistry: null, dbPathOverride: _bpmDbPath);
        _vm = new ManualCaptureViewModel(_planDb, _bpmDb, idGen);
    }

    public void Dispose()
    {
        var planDbPath = _planDb.GetDatabasePath();
        _planDb.Dispose();
        _bpmDb.Dispose();
        // BPM-120 T0: gezielter Pool-Clear statt ClearAllPools (Parallellast-Flaky)
        using (var pc = new SqliteConnection($"Data Source={planDbPath}"))
            SqliteConnection.ClearPool(pc);
        using (var pc = new SqliteConnection($"Data Source={_bpmDbPath}"))
            SqliteConnection.ClearPool(pc);
        TempDb.Delete(planDbPath);
        try { if (File.Exists(_bpmDbPath)) File.Delete(_bpmDbPath); } catch { }
    }

    private static CaptureRowViewModel Row(string fileName, string? planNr = null)
    {
        var scan = new ScannedFile($"_Eingang/{fileName}", fileName, ".pdf", 100, DateTime.UtcNow);
        var candidates = new PlanFileCandidates(
            fileName, planNr, Index: null, RevisionKind.None,
            Level: null, BuildingPartHint: null, TypeKeywords: [],
            DateCandidate: null, HasCopyMarker: false, IsCombi: false);
        var item = new CaptureItem(
            new FingerprintedFile(scan, "md5-" + fileName),
            candidates, CaptureBucket.NewCapture, Match: null, Reason: null);
        return new CaptureRowViewModel(item) { IsSelected = true };
    }

    private void Seed(params CaptureRowViewModel[] rows)
    {
        foreach (var row in rows)
            _vm.Rows.Add(row);
    }

    [Fact]
    public void BeginCapture_CollisionWarning_ClearsOnSelectionChange()
    {
        var a = Row("5998-305_A.pdf", "5998-305");
        var b = Row("5998-305_B.pdf", "5998-305");
        Seed(a, b);

        var controller = _vm.BeginCapture(a);

        Assert.NotNull(controller);
        Assert.StartsWith("⚠", _vm.StatusText);

        b.IsSelected = false;
        _vm.SetSelectedRow();

        Assert.DoesNotContain("⚠", _vm.StatusText);
    }

    [Fact]
    public void BeginCapture_BlockedOver20_ClearsOnSelectionChange()
    {
        var rows = Enumerable.Range(1, 21)
            .Select(i => Row($"5998-{100 + i}_EG.pdf", $"5998-{100 + i}"))
            .ToArray();
        Seed(rows);

        var controller = _vm.BeginCapture(rows[0]);

        Assert.Null(controller); // Deckel: Radial öffnet nicht
        Assert.StartsWith("⛔", _vm.StatusText);

        foreach (var row in rows.Skip(1))
            row.IsSelected = false;
        _vm.SetSelectedRow();

        Assert.DoesNotContain("⛔", _vm.StatusText);
    }

    [Fact]
    public void OtherStatusMessages_SurviveSelectionChange()
    {
        var a = Row("5998-101_EG.pdf", "5998-101");
        Seed(a);

        _vm.StatusText = "✓ 3 Datei(en) importiert (Journal → Move → DB)";
        a.IsSelected = false;
        _vm.SetSelectedRow();

        Assert.Equal("✓ 3 Datei(en) importiert (Journal → Move → DB)", _vm.StatusText);
    }
}

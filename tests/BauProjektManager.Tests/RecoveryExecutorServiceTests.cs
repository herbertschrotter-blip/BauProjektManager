using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Tests;

/// <summary>
/// Integration-Tests für <see cref="BauProjektManager.PlanManager.Services.RecoveryExecutorService"/>.
/// Verwendet <see cref="RecoveryTestFixture"/> mit echter SQLite-DB + echten Files in Temp-Folder.
/// 5 Cases analog zu Docs/Test/Recovery-Szenarien.md. Siehe BPM-098 Stufe 2.
/// </summary>
public class RecoveryExecutorServiceTests
{
    [Fact]
    public void ExecuteForward_AllPending_MovesFilesAndCompletesJournal()
    {
        // Setup: 3 Inbox-Dateien, 3 Actions alle 'pending'
        using var f = new RecoveryTestFixture();
        var importId = f.CreateJournal(fileCount: 3);
        for (int i = 0; i < 3; i++)
        {
            var fileName = $"plan-{i}.pdf";
            var srcRel = f.SeedInboxFile(fileName, $"content-{i}");
            var dstRel = Path.Combine(f.PlansRel, fileName);
            f.Db.InsertImportAction(importId, i, "new",
                documentKey: $"k{i}", planNumber: $"P{i}", planIndex: "00",
                oldIndex: null, sourcePath: srcRel, destinationPath: dstRel,
                archivePath: null);
        }

        // Act
        var result = f.Executor.ExecuteForward(importId, f.ProjectRoot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal("completed", f.GetJournalStatus(importId));
        for (int i = 0; i < 3; i++)
        {
            Assert.False(f.FileExistsInInbox($"plan-{i}.pdf"));
            Assert.True(f.FileExistsInPlans($"plan-{i}.pdf"));
        }
    }

    [Fact]
    public void ExecuteForward_AllCompleted_OnlyFinalizesJournal()
    {
        // Setup: 3 Actions alle 'completed', Journal 'pending'
        using var f = new RecoveryTestFixture();
        var importId = f.CreateJournal(fileCount: 3);
        for (int i = 0; i < 3; i++)
        {
            // Datei ist bereits in Plans (Action war erfolgreich)
            f.SeedPlansFile($"plan-{i}.pdf", $"content-{i}");
            var dstRel = Path.Combine(f.PlansRel, $"plan-{i}.pdf");
            var srcRel = Path.Combine(f.InboxRel, $"plan-{i}.pdf");
            var actionId = f.Db.InsertImportAction(importId, i, "new",
                documentKey: $"k{i}", planNumber: $"P{i}", planIndex: "00",
                oldIndex: null, sourcePath: srcRel, destinationPath: dstRel,
                archivePath: null);
            f.SetActionStatus(actionId, "completed");
        }

        // Act
        var result = f.Executor.ExecuteForward(importId, f.ProjectRoot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ProcessedCount); // keine pending Actions mehr
        Assert.Equal(0, result.FailedCount);
        Assert.Equal("completed", f.GetJournalStatus(importId));
    }

    [Fact]
    public void ExecuteForward_MixState_ProcessesPendingAndKeepsCompleted()
    {
        // Setup: 5 Actions, 2 'completed' (Files in Plans), 3 'pending' (Files in Inbox)
        using var f = new RecoveryTestFixture();
        var importId = f.CreateJournal(fileCount: 5);
        for (int i = 0; i < 5; i++)
        {
            var fileName = $"plan-{i}.pdf";
            var srcRel = Path.Combine(f.InboxRel, fileName);
            var dstRel = Path.Combine(f.PlansRel, fileName);
            var actionId = f.Db.InsertImportAction(importId, i, "new",
                documentKey: $"k{i}", planNumber: $"P{i}", planIndex: "00",
                oldIndex: null, sourcePath: srcRel, destinationPath: dstRel,
                archivePath: null);
            if (i < 2)
            {
                // Schon completed: Datei in Plans
                f.SeedPlansFile(fileName);
                f.SetActionStatus(actionId, "completed");
            }
            else
            {
                // Pending: Datei in Inbox
                f.SeedInboxFile(fileName);
            }
        }

        // Act
        var result = f.Executor.ExecuteForward(importId, f.ProjectRoot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal("completed", f.GetJournalStatus(importId));
        for (int i = 0; i < 5; i++)
        {
            Assert.False(f.FileExistsInInbox($"plan-{i}.pdf"));
            Assert.True(f.FileExistsInPlans($"plan-{i}.pdf"));
        }
    }

    [Fact]
    public void ExecuteRollback_CompletedActions_RestoresFilesToInbox()
    {
        // Setup: 3 Actions alle 'completed', Files in Plans
        using var f = new RecoveryTestFixture();
        var importId = f.CreateJournal(fileCount: 3);
        for (int i = 0; i < 3; i++)
        {
            var fileName = $"plan-{i}.pdf";
            f.SeedPlansFile(fileName, $"content-{i}");
            var actionId = f.Db.InsertImportAction(importId, i, "new",
                documentKey: $"k{i}", planNumber: $"P{i}", planIndex: "00",
                oldIndex: null,
                sourcePath: Path.Combine(f.InboxRel, fileName),
                destinationPath: Path.Combine(f.PlansRel, fileName),
                archivePath: null);
            f.SetActionStatus(actionId, "completed");
        }

        // Act
        var result = f.Executor.ExecuteRollback(importId, f.ProjectRoot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal("failed", f.GetJournalStatus(importId));
        for (int i = 0; i < 3; i++)
        {
            Assert.True(f.FileExistsInInbox($"plan-{i}.pdf"));
            Assert.False(f.FileExistsInPlans($"plan-{i}.pdf"));
        }
    }

    [Fact]
    public void ExecuteCleanup_PendingActions_MarksJournalFailedNoDiskOp()
    {
        // Setup: 3 Actions, 2 'completed' (Files in Plans), 1 'pending' (File in Inbox)
        using var f = new RecoveryTestFixture();
        var importId = f.CreateJournal(fileCount: 3);
        for (int i = 0; i < 3; i++)
        {
            var fileName = $"plan-{i}.pdf";
            var srcRel = Path.Combine(f.InboxRel, fileName);
            var dstRel = Path.Combine(f.PlansRel, fileName);
            var actionId = f.Db.InsertImportAction(importId, i, "new",
                documentKey: $"k{i}", planNumber: $"P{i}", planIndex: "00",
                oldIndex: null, sourcePath: srcRel, destinationPath: dstRel,
                archivePath: null);
            if (i < 2)
            {
                f.SeedPlansFile(fileName);
                f.SetActionStatus(actionId, "completed");
            }
            else
            {
                f.SeedInboxFile(fileName);
                // Bleibt pending
            }
        }

        // Act
        var result = f.Executor.ExecuteCleanup(importId, "test reason");

        // Assert
        Assert.Equal(RecoveryAction.Cleanup, result.Action);
        Assert.Equal("failed", f.GetJournalStatus(importId));
        // Disk: unverändert
        Assert.True(f.FileExistsInPlans("plan-0.pdf"));
        Assert.True(f.FileExistsInPlans("plan-1.pdf"));
        Assert.True(f.FileExistsInInbox("plan-2.pdf"));
    }
}

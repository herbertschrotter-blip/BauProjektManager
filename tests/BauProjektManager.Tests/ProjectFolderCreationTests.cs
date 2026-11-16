using System.IO;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Tests.Fakes;

namespace BauProjektManager.Tests;

/// <summary>
/// R01-004.06 (Audit Review 01, SE-07): Unterordner der Projektvorlage werden beim
/// Anlegen eines neuen Projekts rekursiv erzeugt — SubFolderEntry ist verschachtelt
/// (DB-SCHEMA 10.5), die Vorschau zeigt tiefere Ebenen bereits.
/// </summary>
public class ProjectFolderCreationTests
{
    [Fact]
    public void CreateSubFolders_TwoLevelTemplate_CreatesNestedFolders()
    {
        var fs = new FakeFileStore();
        var parent = Path.Combine(Path.GetTempPath(), "bpm-r01004-virtual", "01 Planunterlagen");
        fs.CreateDirectory(parent);
        List<SubFolderEntry> subs =
        [
            new() { Name = "Polierpläne", SubFolders = [ new() { Name = "Vorabzug", HasPrefix = false } ] },
            new() { Name = "Schalpläne" }
        ];

        ProjectFolderService.CreateSubFolders(fs, fs, fs, parent, subs);

        var first = subs[0].GetDisplayName(0);
        Assert.True(fs.DirectoryExists(Path.Combine(parent, first)));
        Assert.True(fs.DirectoryExists(Path.Combine(parent, first, "Vorabzug")));
        Assert.True(fs.DirectoryExists(Path.Combine(parent, subs[1].GetDisplayName(1))));
    }
}

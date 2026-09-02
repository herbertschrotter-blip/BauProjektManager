using System.IO;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.Tests.Fakes;

namespace BauProjektManager.Tests;

/// <summary>
/// BPM-066: <c>IFileSystemReader.IsHidden</c> — Adapter liest das Hidden-Attribut,
/// der Fake liefert es deterministisch. Grundlage dafür, dass der versteckte
/// .bpm/-Ordner nicht als Projektordner in der Ordnerstruktur erscheint.
/// </summary>
public class FileSystemHiddenTests
{
    [Fact]
    public void LocalFileSystem_IsHidden_ReflectsAttribute()
    {
        var root = Path.Combine(Path.GetTempPath(), "bpm-hidden-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var hidden = Path.Combine(root, ".bpm");
            var visible = Path.Combine(root, "01 Planunterlagen");
            Directory.CreateDirectory(hidden);
            Directory.CreateDirectory(visible);
            File.SetAttributes(hidden, FileAttributes.Hidden | FileAttributes.Directory);

            var fs = new LocalFileSystem();

            Assert.True(fs.IsHidden(hidden));
            Assert.False(fs.IsHidden(visible));
            Assert.False(fs.IsHidden(Path.Combine(root, "gibt-es-nicht")));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void FakeFileStore_IsHidden_OnlyForMarkedPaths()
    {
        var fake = new FakeFileStore();
        fake.CreateDirectory(@"C:\P\.bpm");
        fake.CreateDirectory(@"C:\P\01 Planunterlagen");
        fake.SetHidden(@"C:\P\.bpm");

        Assert.True(fake.IsHidden(@"C:\P\.bpm"));
        Assert.False(fake.IsHidden(@"C:\P\01 Planunterlagen"));
    }

    [Fact]
    public void FakeFileStore_IsHidden_IsCaseInsensitiveLikeWindows()
    {
        var fake = new FakeFileStore();
        fake.CreateDirectory(@"C:\P\.bpm");
        fake.SetHidden(@"C:\P\.BPM");

        Assert.True(fake.IsHidden(@"C:\P\.bpm"));
    }
}

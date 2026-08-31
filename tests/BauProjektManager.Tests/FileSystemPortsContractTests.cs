using System.IO;
using System.Text;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.Tests.Fakes;

namespace BauProjektManager.Tests;

/// <summary>
/// Gemeinsamer Verhaltensvertrag der Dateisystem-Ports (ADR-060, BPM-112).
/// Laeuft identisch gegen den echten <see cref="LocalFileSystem"/> (Temp-
/// Verzeichnis) und gegen <see cref="FakeFileStore"/> — so bleibt der Fake
/// verhaltenstreu zum Adapter (die uebrigen Slices verlassen sich darauf).
/// </summary>
public abstract class FileSystemPortsContractTests
{
    protected abstract IFileSystemReader Reader { get; }
    protected abstract IFileSystemWriter Writer { get; }
    protected abstract IPathService Path { get; }
    protected abstract string Root { get; }

    /// <summary>Legt eine Quelldatei an — pro Implementierung (echt vs. Fake) anders.</summary>
    protected abstract void SeedFile(string path, byte[] content);

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    // --- CreateDirectory ---

    [Fact]
    public void CreateDirectory_MakesDirectoryExist()
    {
        var dir = Path.Combine(Root, "neu");

        Writer.CreateDirectory(dir);

        Assert.True(Reader.DirectoryExists(dir));
    }

    [Fact]
    public void CreateDirectory_IsIdempotent()
    {
        var dir = Path.Combine(Root, "doppelt");

        Writer.CreateDirectory(dir);
        Writer.CreateDirectory(dir);

        Assert.True(Reader.DirectoryExists(dir));
    }

    // --- Exists ---

    [Fact]
    public void FileExists_FalseForUnknown()
    {
        Assert.False(Reader.FileExists(Path.Combine(Root, "gibtsnicht.txt")));
    }

    // --- ReadAllText / WriteAllText (BPM-112.01, JSON-Konfigs) ---

    [Fact]
    public void WriteAllText_ReadAllText_RoundtripsUtf8()
    {
        Writer.CreateDirectory(Root);
        var file = Path.Combine(Root, "konfig.json");

        Writer.WriteAllText(file, "{\"name\":\"Pläne ÄÖÜ\"}");

        Assert.True(Reader.FileExists(file));
        Assert.Equal("{\"name\":\"Pläne ÄÖÜ\"}", Reader.ReadAllText(file));
    }

    [Fact]
    public void WriteAllText_OverwritesExistingContent()
    {
        Writer.CreateDirectory(Root);
        var file = Path.Combine(Root, "konfig2.json");
        Writer.WriteAllText(file, "alt");

        Writer.WriteAllText(file, "neu");

        Assert.Equal("neu", Reader.ReadAllText(file));
    }

    [Fact]
    public void WriteAllText_MissingDirectory_Throws()
    {
        var file = Path.Combine(Root, "fehlt", "konfig.json");

        Assert.Throws<DirectoryNotFoundException>(
            () => Writer.WriteAllText(file, "x"));
    }

    // --- Move ---

    [Fact]
    public void MoveFile_RelocatesContent()
    {
        var src = Path.Combine(Root, "a.txt");
        var destDir = Path.Combine(Root, "ziel");
        var dst = Path.Combine(destDir, "a.txt");
        SeedFile(src, Bytes("inhalt"));
        Writer.CreateDirectory(destDir);

        Writer.MoveFile(src, dst);

        Assert.False(Reader.FileExists(src));
        Assert.True(Reader.FileExists(dst));
        Assert.Equal("inhalt", ReadAll(dst));
    }

    [Fact]
    public void MoveFile_ThrowsWhenDestExists_WithoutOverwrite()
    {
        var src = Path.Combine(Root, "src.txt");
        var dst = Path.Combine(Root, "dst.txt");
        SeedFile(src, Bytes("neu"));
        SeedFile(dst, Bytes("alt"));

        Assert.Throws<IOException>(() => Writer.MoveFile(src, dst));
    }

    [Fact]
    public void MoveFile_OverwritesWhenRequested()
    {
        var src = Path.Combine(Root, "ov-src.txt");
        var dst = Path.Combine(Root, "ov-dst.txt");
        SeedFile(src, Bytes("neu"));
        SeedFile(dst, Bytes("alt"));

        Writer.MoveFile(src, dst, overwrite: true);

        Assert.False(Reader.FileExists(src));
        Assert.Equal("neu", ReadAll(dst));
    }

    // --- Copy ---

    [Fact]
    public void CopyFile_KeepsSourceAndCreatesDest()
    {
        var src = Path.Combine(Root, "c-src.txt");
        var dst = Path.Combine(Root, "c-dst.txt");
        SeedFile(src, Bytes("kopie"));

        Writer.CopyFile(src, dst);

        Assert.True(Reader.FileExists(src));
        Assert.Equal("kopie", ReadAll(dst));
    }

    // --- Delete ---

    [Fact]
    public void DeleteFile_RemovesFile()
    {
        var path = Path.Combine(Root, "weg.txt");
        SeedFile(path, Bytes("x"));

        Writer.DeleteFile(path);

        Assert.False(Reader.FileExists(path));
    }

    [Fact]
    public void DeleteFile_MissingIsNoOp()
    {
        var path = Path.Combine(Root, "nie-da.txt");

        Writer.DeleteFile(path); // darf nicht werfen

        Assert.False(Reader.FileExists(path));
    }

    // --- Enumerate ---

    [Fact]
    public void EnumerateFiles_TopLevelVsRecursive()
    {
        var sub = Path.Combine(Root, "tief");
        var top = Path.Combine(Root, "top.txt");
        var nested = Path.Combine(sub, "nested.txt");
        SeedFile(top, Bytes("1"));
        Writer.CreateDirectory(sub);
        SeedFile(nested, Bytes("2"));

        var topOnly = Reader.EnumerateFiles(Root).Select(Path.GetFileName).ToList();
        var all = Reader.EnumerateFiles(Root, "*", recursive: true).Select(Path.GetFileName).ToList();

        Assert.Contains("top.txt", topOnly);
        Assert.DoesNotContain("nested.txt", topOnly);
        Assert.Contains("top.txt", all);
        Assert.Contains("nested.txt", all);
    }

    [Fact]
    public void EnumerateFiles_HonorsSearchPattern()
    {
        SeedFile(Path.Combine(Root, "plan.pdf"), Bytes("a"));
        SeedFile(Path.Combine(Root, "notiz.txt"), Bytes("b"));

        var pdfs = Reader.EnumerateFiles(Root, "*.pdf").Select(Path.GetFileName).ToList();

        Assert.Contains("plan.pdf", pdfs);
        Assert.DoesNotContain("notiz.txt", pdfs);
    }

    [Fact]
    public void EnumerateDirectories_ListsChildren()
    {
        Writer.CreateDirectory(Path.Combine(Root, "d1"));
        Writer.CreateDirectory(Path.Combine(Root, "d2"));

        var dirs = Reader.EnumerateDirectories(Root).Select(Path.GetFileName).ToList();

        Assert.Contains("d1", dirs);
        Assert.Contains("d2", dirs);
    }

    // --- GetFileInfo / OpenRead ---

    [Fact]
    public void GetFileInfo_ReportsExistenceAndLength()
    {
        var path = Path.Combine(Root, "info.txt");
        SeedFile(path, Bytes("12345"));

        var info = Reader.GetFileInfo(path);

        Assert.True(info.Exists);
        Assert.Equal(5, info.Length);
    }

    [Fact]
    public void GetFileInfo_MissingHasExistsFalse()
    {
        var info = Reader.GetFileInfo(Path.Combine(Root, "fehlt.txt"));

        Assert.False(info.Exists);
        Assert.Equal(0, info.Length);
    }

    [Fact]
    public void OpenRead_ReturnsContent()
    {
        var path = Path.Combine(Root, "lesen.txt");
        SeedFile(path, Bytes("hallo welt"));

        Assert.Equal("hallo welt", ReadAll(path));
    }

    // --- IPathService ---

    [Fact]
    public void Path_OperationsBehaveLikeSystemPath()
    {
        var combined = Path.Combine(Root, "ordner", "datei.pdf");

        Assert.Equal("datei.pdf", Path.GetFileName(combined));
        Assert.Equal(".pdf", Path.GetExtension(combined));
        Assert.Equal(System.IO.Path.Combine("ordner", "datei.pdf"), Path.GetRelativePath(Root, combined));
    }

    private string ReadAll(string path)
    {
        using var stream = Reader.OpenRead(path);
        using var sr = new StreamReader(stream, Encoding.UTF8);
        return sr.ReadToEnd();
    }
}

/// <summary>
/// Vertrag gegen den echten <see cref="LocalFileSystem"/> in einem temporaeren
/// Verzeichnis (echte Move-/Overwrite-/Enumerate-Semantik).
/// </summary>
public sealed class LocalFileSystemContractTests : FileSystemPortsContractTests, IDisposable
{
    private readonly LocalFileSystem _fs = new();
    private readonly string _root;

    public LocalFileSystemContractTests()
    {
        _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bpm-fs-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_root);
    }

    protected override IFileSystemReader Reader => _fs;
    protected override IFileSystemWriter Writer => _fs;
    protected override IPathService Path => _fs;
    protected override string Root => _root;

    protected override void SeedFile(string path, byte[] content)
    {
        var dir = System.IO.Path.GetDirectoryName(path)!;
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllBytes(path, content);
    }

    public void Dispose()
    {
        try { System.IO.Directory.Delete(_root, recursive: true); }
        catch { /* best effort cleanup */ }
    }
}

/// <summary>
/// Derselbe Vertrag gegen den In-Memory-<see cref="FakeFileStore"/>.
/// </summary>
public sealed class FakeFileStoreContractTests : FileSystemPortsContractTests
{
    private readonly FakeFileStore _fs = new();

    protected override IFileSystemReader Reader => _fs;
    protected override IFileSystemWriter Writer => _fs;
    protected override IPathService Path => _fs;
    protected override string Root => @"C:\bpm-fake-root";

    protected override void SeedFile(string path, byte[] content) => _fs.AddFile(path, content);
}

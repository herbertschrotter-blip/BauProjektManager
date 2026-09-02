using System.IO;
using System.Text.Json;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Persistence;

namespace BauProjektManager.Tests;

/// <summary>
/// BPM-046 (ADR-046): Manifest-Split im .bpm/-Ordner — schlanker Ausweis manifest.json,
/// Vollexport project.json, Vorwärtsmigration aus .bpm-manifest und manifest.json v1.
/// </summary>
public class BpmFolderManifestTests
{
    private sealed class TempRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "bpm-manifest-test-" + Guid.NewGuid().ToString("N"));

        public TempRoot() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true); } catch { }
        }
    }

    private static (ManifestService manifest, ProjectExportService export) CreateServices()
    {
        var export = new ProjectExportService();
        return (new ManifestService(export), export);
    }

    private static Project SampleProject() => new()
    {
        Id = "01HV8M2Q9AJ3W1XK7R4F5N6T8C",
        ProjectNumber = "202512",
        Name = "Dobl-Zwaring",
        FullName = "ÖWG Dobl-Zwaring BA1",
        Client = new Client { Company = "ÖWG", ContactPerson = "Max Muster", Email = "max@example.at" },
        Participants =
        {
            new ProjectParticipant { Role = "Statik", Company = "Statik GmbH", ContactPerson = "Erika Beispiel", Phone = "0664/1234567" }
        },
        BuildingParts =
        {
            new BuildingPart { ShortName = "H1", Description = "Haus 1", Levels = { new BuildingLevel { Prefix = 0, Name = "EG" } } }
        },
        Timeline = new ProjectTimeline { ProjectStart = new DateTime(2025, 12, 1) }
    };

    private static JsonElement ReadRoot(string path)
        => JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();

    [Fact]
    public void WriteBoth_SlimManifestWithoutPersonData_FullExportWithParticipants()
    {
        using var root = new TempRoot();
        var (manifest, export) = CreateServices();
        var project = SampleProject();

        export.WriteExport(project, root.Path);
        manifest.WriteManifest(project, root.Path);

        var m = ReadRoot(Path.Combine(root.Path, ".bpm", "manifest.json"));
        Assert.Equal(2, m.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(project.Id, m.GetProperty("projectId").GetString());
        Assert.Equal("202512", m.GetProperty("projectNumber").GetString());
        Assert.True(m.GetProperty("modules").GetProperty("planManager").GetBoolean());
        Assert.False(m.TryGetProperty("participants", out _));
        Assert.False(m.TryGetProperty("client", out _));

        var e = ReadRoot(Path.Combine(root.Path, ".bpm", "project.json"));
        Assert.Equal(1, e.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(1, e.GetProperty("participants").GetArrayLength());
        Assert.Equal("ÖWG", e.GetProperty("client").GetProperty("company").GetString());

        var bpmDir = new DirectoryInfo(Path.Combine(root.Path, ".bpm"));
        Assert.True(bpmDir.Attributes.HasFlag(FileAttributes.Hidden));
    }

    [Fact]
    public void ExportRoundTrip_PreservesProjectData()
    {
        using var root = new TempRoot();
        var (_, export) = CreateServices();
        var project = SampleProject();

        export.WriteExport(project, root.Path);
        var read = export.ReadExport(root.Path);
        Assert.NotNull(read);
        var restored = export.ExportToProject(read!, root.Path);

        Assert.Equal(string.Empty, restored.Id);
        Assert.Equal(root.Path, restored.Paths.Root);
        Assert.Equal("Dobl-Zwaring", restored.Name);
        Assert.Equal("Max Muster", restored.Client.ContactPerson);
        Assert.Single(restored.Participants);
        Assert.Equal("Erika Beispiel", restored.Participants[0].ContactPerson);
        Assert.Single(restored.BuildingParts);
        Assert.Equal("EG", restored.BuildingParts[0].Levels[0].Name);
        Assert.Equal(new DateTime(2025, 12, 1), restored.Timeline.ProjectStart);
    }

    [Fact]
    public void EnsureMigrated_LegacySingleFile_WritesBothAndDeletesLegacy()
    {
        using var root = new TempRoot();
        var (manifest, export) = CreateServices();

        var legacyPath = Path.Combine(root.Path, ".bpm-manifest");
        File.WriteAllText(legacyPath, FullExportJson("Alt-Projekt"));
        File.SetAttributes(legacyPath, FileAttributes.Hidden | FileAttributes.ReadOnly);

        Assert.True(manifest.HasManifest(root.Path));
        Assert.True(manifest.EnsureMigrated(root.Path));

        Assert.False(File.Exists(legacyPath));
        var m = manifest.ReadManifest(root.Path);
        Assert.NotNull(m);
        Assert.Equal(2, m!.SchemaVersion);
        Assert.Equal("Alt-Projekt", m.Name);
        Assert.Equal(string.Empty, m.ProjectId);

        var e = export.ReadExport(root.Path);
        Assert.NotNull(e);
        Assert.Equal("Alt-Projekt", e!.Name);
        Assert.Single(e.Participants);

        Assert.False(manifest.EnsureMigrated(root.Path));
    }

    [Fact]
    public void EnsureMigrated_ManifestV1FullExport_SplitsIntoBothFiles()
    {
        using var root = new TempRoot();
        var (manifest, export) = CreateServices();

        Directory.CreateDirectory(Path.Combine(root.Path, ".bpm"));
        var manifestPath = Path.Combine(root.Path, ".bpm", "manifest.json");
        File.WriteAllText(manifestPath, FullExportJson("V1-Projekt"));

        Assert.True(manifest.EnsureMigrated(root.Path));

        var m = ReadRoot(manifestPath);
        Assert.Equal(2, m.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("V1-Projekt", m.GetProperty("name").GetString());
        Assert.False(m.TryGetProperty("participants", out _));

        var e = export.ReadExport(root.Path);
        Assert.NotNull(e);
        Assert.Equal("Statik GmbH", e!.Participants[0].Company);

        Assert.False(manifest.EnsureMigrated(root.Path));
    }

    [Fact]
    public void EnsureMigrated_DoesNotOverwriteExistingExport()
    {
        using var root = new TempRoot();
        var (manifest, export) = CreateServices();

        export.WriteExport(SampleProject(), root.Path);
        File.WriteAllText(Path.Combine(root.Path, ".bpm-manifest"), FullExportJson("Alt-Projekt"));

        Assert.True(manifest.EnsureMigrated(root.Path));

        var e = export.ReadExport(root.Path);
        Assert.Equal("Dobl-Zwaring", e!.Name);
        Assert.False(File.Exists(Path.Combine(root.Path, ".bpm-manifest")));
    }

    [Fact]
    public void HasManifest_FalseForPlainFolder_NoMigrationNoop()
    {
        using var root = new TempRoot();
        var (manifest, _) = CreateServices();

        Assert.False(manifest.HasManifest(root.Path));
        Assert.False(manifest.EnsureMigrated(root.Path));
        Assert.Null(manifest.ReadManifest(root.Path));
    }

    /// <summary>Altes Vollexport-Format (BpmManifest v1), wie es .bpm-manifest und manifest.json v1 hatten.</summary>
    private static string FullExportJson(string name) => $$"""
        {
          "schemaVersion": 1,
          "updatedAtUtc": "2026-04-10T14:30:00Z",
          "createdByMachine": "Desktop_PC",
          "projectNumber": "202401",
          "name": "{{name}}",
          "fullName": "{{name}} BA1",
          "status": "Active",
          "client": { "company": "Bauherr AG", "contactPerson": "Anna Alt" },
          "participants": [
            { "role": "Statik", "company": "Statik GmbH", "contactPerson": "Erika Beispiel", "sortOrder": 0 }
          ],
          "paths": { "plans": "01 Planunterlagen", "inbox": "01 Planunterlagen\\_Eingang" }
        }
        """;
}

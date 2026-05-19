using System.IO;
using System.Text.Json;
using BauProjektManager.Domain.Interfaces;
using Serilog;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Implementiert <see cref="IProfileArchiveService"/> (BPM-108 Phase B).
/// Verschiebt veraltete RecognitionProfile-JSON-Dateien und pattern-templates.json
/// in einen zeitstempel-praefixierten <c>_archiv/</c>-Unterordner. Strikt
/// "nur lesen → verschieben"; keine Inhaltsaenderung an den Dateien.
/// </summary>
public class ProfileArchiveService : IProfileArchiveService
{
    private static readonly JsonDocumentOptions JsonDocOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    /// <inheritdoc />
    public int ArchiveOutdatedProfiles(string projectRootPath)
    {
        var profilesDir = Path.Combine(projectRootPath, ".bpm", "profiles");
        if (!Directory.Exists(profilesDir))
        {
            Log.Information("ProfileArchiveService: kein profiles-Verzeichnis in {Root} — nichts zu tun.", projectRootPath);
            return 0;
        }

        var archiveDir = Path.Combine(
            profilesDir,
            "_archiv",
            $"schema-reset-{DateTime.UtcNow:yyyyMMdd-HHmmss}");

        int moved = 0;
        foreach (var file in Directory.GetFiles(profilesDir, "*.json"))
        {
            var version = TryReadSchemaVersion(file);
            if (version == ProfileManager.CurrentSchemaVersion)
                continue;

            try
            {
                Directory.CreateDirectory(archiveDir);
                var target = Path.Combine(archiveDir, Path.GetFileName(file));
                File.Move(file, target);
                moved++;
                Log.Information("ProfileArchiveService: archiviert {File} (SchemaVersion={Version}) → {Target}",
                    Path.GetFileName(file), version, target);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "ProfileArchiveService: konnte {File} nicht archivieren", file);
            }
        }

        Log.Information("ProfileArchiveService: {Count} Profile archiviert aus {Dir}", moved, profilesDir);
        return moved;
    }

    /// <inheritdoc />
    public bool ArchiveOutdatedPatternTemplates(string appDataCloudSharedPath)
    {
        var filePath = Path.Combine(appDataCloudSharedPath, "pattern-templates.json");
        if (!File.Exists(filePath))
        {
            Log.Information("ProfileArchiveService: keine pattern-templates.json in {Path} — nichts zu tun.", appDataCloudSharedPath);
            return false;
        }

        if (AllTemplatesCurrent(filePath))
        {
            Log.Information("ProfileArchiveService: pattern-templates.json ist v{Version}-konform — nichts zu tun.",
                PatternTemplateService.CurrentSchemaVersion);
            return false;
        }

        var archiveDir = Path.Combine(
            appDataCloudSharedPath,
            "_archiv",
            $"schema-reset-{DateTime.UtcNow:yyyyMMdd-HHmmss}");

        try
        {
            Directory.CreateDirectory(archiveDir);
            var target = Path.Combine(archiveDir, "pattern-templates.json");
            File.Move(filePath, target);
            Log.Information("ProfileArchiveService: pattern-templates.json archiviert → {Target}", target);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ProfileArchiveService: konnte pattern-templates.json nicht archivieren");
            return false;
        }
    }

    /// <summary>
    /// Liest nur das Feld <c>schemaVersion</c> aus einer Profil-JSON. Bei Fehlern liefert
    /// -1, damit der Aufrufer die Datei als veraltet behandelt.
    /// </summary>
    private static int TryReadSchemaVersion(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var doc = JsonDocument.Parse(stream, JsonDocOptions);
            if (doc.RootElement.TryGetProperty("schemaVersion", out var versionEl)
                && versionEl.ValueKind == JsonValueKind.Number)
            {
                return versionEl.GetInt32();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ProfileArchiveService: SchemaVersion-Lesen aus {File} fehlgeschlagen", filePath);
        }
        return -1;
    }

    /// <summary>
    /// Prueft ob alle Templates in <c>pattern-templates.json</c> die aktuelle Schema-Version haben.
    /// </summary>
    private static bool AllTemplatesCurrent(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var doc = JsonDocument.Parse(stream, JsonDocOptions);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("schemaVersion", out var versionEl)
                    || versionEl.ValueKind != JsonValueKind.Number
                    || versionEl.GetInt32() != PatternTemplateService.CurrentSchemaVersion)
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ProfileArchiveService: pattern-templates.json konnte nicht analysiert werden");
            return false;
        }
    }
}

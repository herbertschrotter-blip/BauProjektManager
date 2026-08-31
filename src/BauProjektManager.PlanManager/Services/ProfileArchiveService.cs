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

    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;

    // BPM-112.01 (ADR-060 Slice 1): Archiv-Moves laufen ueber die FS-Ports.
    public ProfileArchiveService(
        IFileSystemReader reader, IFileSystemWriter writer, IPathService path)
    {
        _reader = reader;
        _writer = writer;
        _path = path;
    }

    /// <inheritdoc />
    public int ArchiveOutdatedProfiles(string projectRootPath)
    {
        var profilesDir = _path.Combine(projectRootPath, ".bpm", "profiles");
        if (!_reader.DirectoryExists(profilesDir))
        {
            Log.Information("ProfileArchiveService: kein profiles-Verzeichnis in {Root} — nichts zu tun.", projectRootPath);
            return 0;
        }

        var archiveDir = _path.Combine(
            profilesDir,
            "_archiv",
            $"schema-reset-{DateTime.UtcNow:yyyyMMdd-HHmmss}");

        int moved = 0;
        foreach (var file in _reader.EnumerateFiles(profilesDir, "*.json"))
        {
            var version = TryReadSchemaVersion(file);
            if (version == ProfileManager.CurrentSchemaVersion)
                continue;

            try
            {
                _writer.CreateDirectory(archiveDir);
                var target = _path.Combine(archiveDir, _path.GetFileName(file));
                _writer.MoveFile(file, target);
                moved++;
                Log.Information("ProfileArchiveService: archiviert {File} (SchemaVersion={Version}) → {Target}",
                    _path.GetFileName(file), version, target);
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
        var filePath = _path.Combine(appDataCloudSharedPath, "pattern-templates.json");
        if (!_reader.FileExists(filePath))
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

        var archiveDir = _path.Combine(
            appDataCloudSharedPath,
            "_archiv",
            $"schema-reset-{DateTime.UtcNow:yyyyMMdd-HHmmss}");

        try
        {
            _writer.CreateDirectory(archiveDir);
            var target = _path.Combine(archiveDir, "pattern-templates.json");
            _writer.MoveFile(filePath, target);
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
    private int TryReadSchemaVersion(string filePath)
    {
        try
        {
            using var stream = _reader.OpenRead(filePath);
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
    private bool AllTemplatesCurrent(string filePath)
    {
        try
        {
            using var stream = _reader.OpenRead(filePath);
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

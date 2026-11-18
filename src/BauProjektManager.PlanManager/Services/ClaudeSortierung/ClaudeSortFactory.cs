using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.ViewModels;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Factory fuer „Mit Claude sortieren“ je Projekt (ADR-067, CLAUDE.md DI-Regel „Container bzw. Factory je Projekt“):
/// bekommt die app-weiten Dienste aus dem DI-Container und setzt mit der Plan-DB des Projekts Builder, Pruefer,
/// Schnellanlage und das Chatfenster zusammen. Die Views reichen nur noch diese Factory durch.
/// </summary>
public sealed class ClaudeSortFactory
{
    private readonly IClaudeAssistant _assistant;
    private readonly ProjectDatabase _bpmDb;
    private readonly IFileSystemReader _reader;
    private readonly IFileSystemWriter _writer;
    private readonly IPathService _path;
    private readonly IPlanValueNormalizer _normalizer;
    private readonly AppSettingsService _settings;
    private readonly string _jobRoot;
    private readonly IPdfTextService? _pdfText;
    private readonly ISegmentTypeCatalog? _segmentTypes;

    /// <param name="jobRoot">Basisordner der Auftraege (ClaudeAssistantOptions.JobRootDirectory).</param>
    public ClaudeSortFactory(
        IClaudeAssistant assistant, ProjectDatabase bpmDb,
        IFileSystemReader reader, IFileSystemWriter writer, IPathService path,
        IPlanValueNormalizer normalizer, AppSettingsService settings, string jobRoot,
        IPdfTextService? pdfText = null, ISegmentTypeCatalog? segmentTypes = null)
    {
        _assistant = assistant;
        _bpmDb = bpmDb;
        _reader = reader;
        _writer = writer;
        _path = path;
        _normalizer = normalizer;
        _settings = settings;
        _jobRoot = jobRoot;
        _pdfText = pdfText;
        _segmentTypes = segmentTypes;
    }

    /// <summary>Chatfenster fuer den Tab „Manuell sortieren“ eines Projekts.</summary>
    public ClaudeSortViewModel Create(IClaudeSortHost host, PlanManagerDatabase planDb)
    {
        var builder = new ClaudeSortAuftragBuilder(_bpmDb, planDb, _reader, _writer, _path, _pdfText,
            segmentTypes: _segmentTypes);
        var pruefer = new ClaudeSortEntwurfPruefer(_bpmDb,
            new DocumentTargetPathResolver(_bpmDb, _normalizer, _path), _reader, _path, _normalizer, _segmentTypes);
        var schnellanlage = new ClaudeSortSchnellanlage(_bpmDb, new DocumentTypeCreationService(_bpmDb, _normalizer));
        return new ClaudeSortViewModel(_assistant, builder, pruefer, host,
            () => _settings.LoadDevice().Claude.SortingEnabled, _jobRoot, schnellanlage,
            () => Domain.Models.ClaudeSettings.NormalizeModel(_settings.LoadDevice().Claude.Model));
    }
}

using System.IO;
using System.Collections.ObjectModel;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Profil-Wizard (Seitenliste 3.8, Mockup 0.2, BPM-130) in 4 Schritten:
/// 1 Dokumenttyp + Beispieldatei · 2 Segmente (SegmentEditorControl) · 3 Index · 4 Erkennung + Speichern.
/// Die Erkennung haengt am Dokumenttyp (ADR-065 P.7): kein eigener Name, kein Zielordner-Schritt (ADR-061).
/// Schritt 2 und 4 liegen in den Partial-Dateien <c>.Segmente</c> und <c>.Erkennung</c>.
/// </summary>
public partial class ProfileWizardViewModel : ObservableObject
{
    /// <summary>Anzahl der Schritte (Mockup 3.8).</summary>
    public const int StepCount = 4;

    // BPM-112.05 (ADR-060 Slice 5): FS-Ports statt direktem System.IO in der VM.
    private static readonly Infrastructure.Services.LocalFileSystem _fs = new();

    private readonly IProfileManager? _profileManager;
    private readonly PatternTemplateService? _templateService;
    private readonly ISegmentTypeCatalog? _segmentTypeCatalog;
    private readonly Project? _project;
    private readonly string? _appDataPath;
    private readonly ProjectDatabase? _bpmDb;
    private RecognitionProfile? _existingProfile;

    [ObservableProperty]
    private int _currentStep = 1;

    public int TotalSteps => StepCount;

    [ObservableProperty]
    private string _stepTitle = "";

    [ObservableProperty]
    private string _dialogTitle = "Erkennung einrichten";

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _canGoBack;

    // === Schritt 1: Dokumenttyp + Beispieldatei ===

    [ObservableProperty]
    private ObservableCollection<PlanDocumentType> _documentTypeOptions = [];

    [ObservableProperty]
    private PlanDocumentType? _selectedDocumentType;

    /// <summary>Hinweis, wenn der Dokumenttyp schon eine Erkennung hat (sie wird ersetzt).</summary>
    [ObservableProperty]
    private string _existingProfileNote = "";

    [ObservableProperty]
    private ObservableCollection<string> _inboxFiles = [];

    [ObservableProperty]
    private string? _selectedInboxFile;

    [ObservableProperty]
    private bool _hasInboxFiles;

    /// <summary>Gewaehlte Beispieldatei wie im Eingang (ggf. mit Kopiermarker).</summary>
    [ObservableProperty]
    private string _sampleFileName = "";

    /// <summary>Beispieldatei ohne Kopiermarker — das bekommt der Segment-Editor.</summary>
    [ObservableProperty]
    private string _editorFileName = "";

    /// <summary>Abgetrennter Kopiermarker („_(1)“, „ - Kopie“), leer = keiner. Ist nie ein Segment.</summary>
    [ObservableProperty]
    private string _copyMarker = "";

    /// <summary>True, wenn das Profil gespeichert wurde (DialogResult).</summary>
    public bool ProfileSaved { get; private set; }

    /// <summary>Grund, wenn Speichern scheitert (z. B. Speicherschutz des ProfileManagers).</summary>
    [ObservableProperty]
    private string _saveError = "";

    public ProfileWizardViewModel(
        Project? project = null,
        IProfileManager? profileManager = null,
        PatternTemplateService? templateService = null,
        string? appDataPath = null,
        ISegmentTypeCatalog? segmentTypeCatalog = null,
        ProjectDatabase? bpmDb = null)
    {
        _project = project;
        _profileManager = profileManager;
        _templateService = templateService;
        _appDataPath = appDataPath;
        _segmentTypeCatalog = segmentTypeCatalog;
        _bpmDb = bpmDb;

        if (project is not null)
        {
            LoadInboxFiles(project);
            LoadDocumentTypes(project);
        }
        UpdateStepState();
    }

    /// <summary>Segmenttyp „Ignorieren“ aus dem Katalog (Sammel-Zuweisung in Schritt 2).</summary>
    public SegmentTypeDefinition? IgnoreType => _segmentTypeCatalog?.GetIncludingDeleted(SegmentTypeIds.Ignore);

    /// <summary>
    /// Liefert die <see cref="SegmentSemanticRole"/> eines Segmenttyps oder <see cref="SegmentSemanticRole.None"/>.
    /// </summary>
    internal SegmentSemanticRole GetRoleForFieldTypeId(string? fieldTypeId)
    {
        if (string.IsNullOrEmpty(fieldTypeId) || _segmentTypeCatalog is null)
            return SegmentSemanticRole.None;
        return _segmentTypeCatalog.GetIncludingDeleted(fieldTypeId)?.SemanticRole ?? SegmentSemanticRole.None;
    }

    partial void OnSelectedInboxFileChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            SampleFileName = value;
    }

    partial void OnSampleFileNameChanged(string value)
    {
        var (clean, marker) = FileNameSegmentation.StripCopyMarker(value.Trim());
        EditorFileName = clean;
        CopyMarker = marker ?? "";
        Segments = [];
        ValidateCurrentStep();
    }

    partial void OnSelectedDocumentTypeChanged(PlanDocumentType? value)
    {
        DialogTitle = value is null ? "Erkennung einrichten" : $"Erkennung für „{value.Name}“ einrichten";
        _existingProfile = value is null ? null : FindExistingProfile(value.Id);
        ExistingProfileNote = _existingProfile is null
            ? ""
            : $"„{value!.Name}“ hat schon eine Erkennung – sie wird beim Speichern ersetzt.";
        ValidateCurrentStep();
    }

    /// <summary>Vorhandenes Profil desselben Dokumenttyps (ersetzen statt verdoppeln).</summary>
    private RecognitionProfile? FindExistingProfile(string documentTypeId)
    {
        if (_profileManager is null || string.IsNullOrWhiteSpace(_project?.Paths.Root))
            return null;
        try
        {
            return _profileManager.LoadAll(_project.Paths.Root)
                .FirstOrDefault(p => string.Equals(p.DocumentTypeId, documentTypeId, StringComparison.Ordinal));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Log.Warning("Wizard: vorhandene Profile nicht lesbar ({Error})", ex.GetType().Name);
            return null;
        }
    }

    private void LoadInboxFiles(Project project)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(project.Paths.Root))
                return;
            var inboxPath = _fs.Combine(project.Paths.Root, project.Paths.Inbox);
            if (!_fs.DirectoryExists(inboxPath))
                return;
            var files = _fs.EnumerateFiles(inboxPath, "*", recursive: true)
                .Select(f => _fs.GetFileName(f))
                .OfType<string>()
                .OrderBy(f => f)
                .ToList();
            InboxFiles = new ObservableCollection<string>(files);
            HasInboxFiles = files.Count > 0;
            Log.Information("Wizard: {Count} Dateien im Eingang geladen", files.Count);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Eingang konnte nicht geladen werden");
        }
    }

    private void LoadDocumentTypes(Project project)
    {
        if (_bpmDb is null)
            return;
        try
        {
            var types = _bpmDb.GetDocumentTypes(project.Id);
            DocumentTypeOptions = new ObservableCollection<PlanDocumentType>(types);
            Log.Information("Wizard: {Count} Dokumenttypen geladen", types.Count);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Dokumenttypen konnten nicht geladen werden");
        }
    }

    // === Navigation ===

    [RelayCommand]
    private void GoNext()
    {
        if (CurrentStep >= StepCount || !CanGoNext)
            return;
        CurrentStep++;
        UpdateStepState();
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep <= 1)
            return;
        CurrentStep--;
        UpdateStepState();
    }

    private void UpdateStepState()
    {
        StepTitle = CurrentStep switch
        {
            1 => "Schritt 1: Dokumenttyp und Beispieldatei",
            2 => "Schritt 2: Segmente zuweisen",
            3 => "Schritt 3: Index",
            4 => "Schritt 4: Erkennung",
            _ => ""
        };
        CanGoBack = CurrentStep > 1;
        if (CurrentStep == 3)
            UpdateIndexWarning();
        if (CurrentStep == 4)
            BuildRecognitionSegments();
        ValidateCurrentStep();
    }

    internal void ValidateCurrentStep()
    {
        CanGoNext = CurrentStep switch
        {
            1 => ValidateStep1(),
            2 => ValidateStep2(),
            3 => true,
            4 => ValidateStep4(),
            _ => false
        };
    }

    /// <summary>Schritt 1: Dokumenttyp und Beispieldatei gewaehlt.</summary>
    private bool ValidateStep1()
        => SelectedDocumentType is not null && !string.IsNullOrWhiteSpace(EditorFileName);
}

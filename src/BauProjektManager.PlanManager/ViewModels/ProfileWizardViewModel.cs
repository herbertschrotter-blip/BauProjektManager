using System.Collections.ObjectModel;
using System.IO;
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
/// ViewModel fuer den 5-Schritt Profil-Wizard.
/// Schritt 1: Datei auswaehlen + Parsen.
/// Schritt 2: Segmente zuweisen (Segmenttyp-Chips aus dem Katalog).
/// Schritt 3: Index-Konfiguration.
/// Schritt 4: Zielordner + Ordner-Hierarchie.
/// Schritt 5: Erkennung (klickbare Segmente).
/// </summary>
/// <remarks>
/// BPM-108 Phase C: Verwendet <see cref="ISegmentTypeCatalog"/> statt der frueheren
/// hardcoded <c>FieldType</c>-Enum-Liste. Pflicht- / Index- / Hierarchie- /
/// Variable-Logik laeuft ueber <see cref="SegmentSemanticRole"/>.
/// </remarks>
public partial class ProfileWizardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private int _totalSteps = 5;

    [ObservableProperty]
    private string _stepTitle = "Schritt 1: Datei auswaehlen";

    // === Dateien im Eingang ===

    [ObservableProperty]
    private ObservableCollection<string> _inboxFiles = [];

    [ObservableProperty]
    private string? _selectedInboxFile;

    [ObservableProperty]
    private bool _hasInboxFiles;

    // === Schritt 1: Datei auswaehlen + Parsen ===

    [ObservableProperty]
    private string _sampleFileName = "";

    [ObservableProperty]
    private string _delimiterText = "- _";

    [ObservableProperty]
    private ObservableCollection<FileNameSegment> _segments = [];

    [ObservableProperty]
    private string _parseInfo = "";

    /// <summary>
    /// Verfuegbare Segmenttypen fuer Wizard-Schritt 2 — aus dem Katalog.
    /// Wird im Konstruktor + nach Catalog-<c>Changed</c>-Event aufgebaut.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FieldTypeOption> _fieldTypeOptions = [];

    // === Schritt 2: Inline-Popover "+ Eigenes" (BPM-108 Phase C Teil 2) ===

    /// <summary>Sichtbarkeit des Inline-Popovers fuer Custom-Anlage.</summary>
    [ObservableProperty]
    private bool _showCustomPopover;

    /// <summary>Name-Eingabe im Inline-Popover.</summary>
    [ObservableProperty]
    private string _customTypeName = "";

    /// <summary>Gewaehlte Farbe (Hex) im Inline-Popover. Default = neutraler Eigene-Ton.</summary>
    [ObservableProperty]
    private string _customTypeColor = "#A87142";

    /// <summary>Live-generierter Token-Preview aus dem Namen. Read-only fuer den User.</summary>
    public string CustomTypeTokenPreview =>
        TokenKeyGenerator.Normalize(CustomTypeName);

    /// <summary>Validierungs-Fehlertext (z. B. "Name ist erforderlich.").</summary>
    [ObservableProperty]
    private string _customTypeError = "";

    /// <summary>12er-Palette fuer den Color-Picker. Aktuell hardcoded analog zum Mockup.</summary>
    public IReadOnlyList<string> CustomTypePalette { get; } =
    [
        "#0F6E56", "#993C1D", "#534AB7", "#185FA5",
        "#1F7280", "#555555", "#7A1F5C", "#A87142",
        "#3D7B47", "#8B6914", "#5C3D8E", "#2E7D8A"
    ];

    /// <summary>
    /// Optional: bei Popover-Open vorgemerktes Segment, dem der neue Typ automatisch
    /// zugewiesen wird. Null = nur Chip anlegen.
    /// </summary>
    private FileNameSegment? _customAssignmentTarget;

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private bool _canGoBack;

    // === Schritt 3: IndexSource ===

    public List<IndexSourceOption> IndexSourceOptions { get; } =
    [
        new("Aus Dateiname", IndexSourceType.FileName,
            "Index wird aus einem Segment im Dateinamen gelesen (z.B. A, B, C)"),
        new("Kein Index", IndexSourceType.None,
            "Dokument hat keinen Index. Versionen werden per MD5-Hash erkannt."),
        new("Aus Plankopf (Post-V1)", IndexSourceType.PlanHeader,
            "Index wird aus dem PDF-Plankopf gelesen. Noch nicht verfuegbar.",
            isEnabled: false)
    ];

    [ObservableProperty]
    private IndexSourceType _selectedIndexSource = IndexSourceType.FileName;

    [ObservableProperty]
    private bool _showIndexModeOptions = true;

    [ObservableProperty]
    private bool _indexModeOptional = true;

    [ObservableProperty]
    private bool _indexCaseInsensitive = true;

    [ObservableProperty]
    private bool _showIndexWarning;

    // === Schritt 4: Zielordner ===

    public List<string> TargetFolderOptions { get; } =
    [
        "01 Planunterlagen",
        "02 Statik",
        "03 Dokumente",
        "04 Protokolle",
        "05 Fotos",
        "06 Sonstiges"
    ];

    [ObservableProperty]
    private string _selectedTargetFolder = "01 Planunterlagen";

    [ObservableProperty]
    private bool _useCustomFolder;

    [ObservableProperty]
    private string _customFolderName = "";

    [ObservableProperty]
    private ObservableCollection<HierarchyLevelOption> _availableHierarchyLevels = [];

    [ObservableProperty]
    private string _folderPreview = "";

    // === Schritt 4: Dokumenttyp (BPM-113.06 Slice 0.6b, ADR-061) ===
    // Loest die hardcodierten TargetFolderOptions ab: Der Zielordner kommt jetzt aus
    // den document_types-Stammdaten (root_relative_path/folder_name) und ist damit
    // resolverbar (DocumentTargetPathResolver). Die alten TargetFolder-Felder bleiben
    // additiv bis Slice 0.6c (dort Entfernung + SchemaVersion 5 + Fruehphasen-Reset).

    /// <summary>Dokumenttypen aus den Projekt-Stammdaten (bpm.db) fuer den Schritt-4-Picker.</summary>
    [ObservableProperty]
    private ObservableCollection<PlanDocumentType> _documentTypeOptions = [];

    /// <summary>
    /// Gewaehlter Dokumenttyp. Setzt beim Wechsel <see cref="DocumentTypeName"/> (Anzeige)
    /// und liefert beim Speichern die stabile <c>type.Id</c> als <c>DocumentTypeId</c>.
    /// </summary>
    [ObservableProperty]
    private PlanDocumentType? _selectedDocumentType;

    // === Schritt 5: Erkennung (klickbare Segmente) ===

    [ObservableProperty]
    private string _documentTypeName = "";

    /// <summary>
    /// Segmente als klickbare Bloecke fuer Erkennung.
    /// IsSelected = User hat dieses Segment als Erkennungsmuster gewaehlt.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RecognitionSegment> _recognitionSegments = [];

    [ObservableProperty]
    private string _recognitionPattern = "";

    [ObservableProperty]
    private string _selectedRecognitionMethod = "segment";

    [ObservableProperty]
    private int _recognitionPriority = 100;

    [ObservableProperty]
    private string _patternTestResult = "";

    [ObservableProperty]
    private bool _patternTestSuccess;

    /// <summary>
    /// BPM-082.04: Warnung im Wizard-Schritt 5 wenn ein als Erkennungsmuster
    /// markiertes Segment typischerweise variabel ist (PlanNummer, Index, Datum,
    /// rein numerisch). Kein Hard-Fail — User darf weiter speichern.
    /// Leerer String = keine Warnung anzeigen (Style-Trigger im XAML).
    /// </summary>
    [ObservableProperty]
    private string _recognitionWarning = "";

    /// <summary>True wenn ein Segment einen Typ mit <see cref="SegmentSemanticRole.PlanIndex"/> hat.</summary>
    public bool HasPlanIndexSegment => HasSegmentWithRole(SegmentSemanticRole.PlanIndex);

    /// <summary>True wenn ein Segment einen Typ mit <see cref="SegmentSemanticRole.PlanNumber"/> hat.</summary>
    public bool HasPlanNumberSegment => HasSegmentWithRole(SegmentSemanticRole.PlanNumber);

    /// <summary>
    /// True wenn das Profil erfolgreich gespeichert wurde.
    /// Wird vom Dialog abgefragt um DialogResult zu setzen.
    /// </summary>
    public bool ProfileSaved { get; private set; }

    private readonly IProfileManager? _profileManager;
    private readonly PatternTemplateService? _templateService;
    private readonly ISegmentTypeCatalog? _segmentTypeCatalog;
    private readonly ISegmentTypeRepository? _segmentTypeRepository;
    private readonly IIdGenerator? _idGenerator;
    private readonly Project? _project;
    private readonly string? _appDataPath;
    private readonly ProjectDatabase? _bpmDb;

    public ProfileWizardViewModel(
        Project? project = null,
        IProfileManager? profileManager = null,
        PatternTemplateService? templateService = null,
        string? appDataPath = null,
        ISegmentTypeCatalog? segmentTypeCatalog = null,
        ISegmentTypeRepository? segmentTypeRepository = null,
        IIdGenerator? idGenerator = null,
        ProjectDatabase? bpmDb = null)
    {
        _project = project;
        _profileManager = profileManager;
        _templateService = templateService;
        _appDataPath = appDataPath;
        _segmentTypeCatalog = segmentTypeCatalog;
        _segmentTypeRepository = segmentTypeRepository;
        _idGenerator = idGenerator;
        _bpmDb = bpmDb;

        RebuildFieldTypeOptions();
        if (_segmentTypeCatalog is not null)
            _segmentTypeCatalog.Changed += (_, _) => RebuildFieldTypeOptions();

        if (project is not null)
        {
            LoadInboxFiles(project);
            LoadDocumentTypes(project);
        }
    }

    /// <summary>
    /// Liefert die <see cref="SegmentSemanticRole"/> eines Segmenttyps oder <see cref="SegmentSemanticRole.None"/>
    /// wenn ID/Custom/Catalog fehlt.
    /// </summary>
    internal SegmentSemanticRole GetRoleForFieldTypeId(string? fieldTypeId)
    {
        if (string.IsNullOrEmpty(fieldTypeId) || _segmentTypeCatalog is null)
            return SegmentSemanticRole.None;
        var def = _segmentTypeCatalog.GetIncludingDeleted(fieldTypeId);
        return def?.SemanticRole ?? SegmentSemanticRole.None;
    }

    private bool HasSegmentWithRole(SegmentSemanticRole role)
    {
        if (_segmentTypeCatalog is null) return false;
        var snap = _segmentTypeCatalog.SnapshotIncludingDeleted();
        return Segments.Any(s =>
            s.FieldTypeId is { Length: > 0 } id
            && snap.TryGetValue(id, out var def)
            && def.SemanticRole == role);
    }

    // === OnChanged Handlers ===

    partial void OnSelectedInboxFileChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            SampleFileName = value;
            ParseFileNameCommand.Execute(null);
        }
    }

    partial void OnSelectedIndexSourceChanged(IndexSourceType value)
    {
        ShowIndexModeOptions = value == IndexSourceType.FileName;
        ValidateCurrentStep();
    }

    partial void OnSelectedTargetFolderChanged(string value)
    {
        UpdateFolderPreview();
        ValidateCurrentStep();
    }

    partial void OnUseCustomFolderChanged(bool value)
    {
        UpdateFolderPreview();
        ValidateCurrentStep();
    }

    partial void OnCustomFolderNameChanged(string value)
    {
        UpdateFolderPreview();
        ValidateCurrentStep();
    }

    partial void OnSelectedDocumentTypeChanged(PlanDocumentType? value)
    {
        if (value is not null)
            DocumentTypeName = value.Name;
        UpdateFolderPreview();
        ValidateCurrentStep();
    }

    partial void OnDocumentTypeNameChanged(string value)
    {
        ValidateCurrentStep();
    }

    partial void OnCustomTypeNameChanged(string value)
    {
        OnPropertyChanged(nameof(CustomTypeTokenPreview));
        // Fehler ausblenden sobald wieder getippt wird
        if (!string.IsNullOrWhiteSpace(value))
            CustomTypeError = "";
    }

    private void LoadInboxFiles(Project project)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(project.Paths.Root))
                return;

            var inboxPath = Path.Combine(project.Paths.Root, project.Paths.Inbox);
            if (!Directory.Exists(inboxPath))
                return;

            var files = Directory.GetFiles(inboxPath, "*", SearchOption.AllDirectories)
                .Select(Path.GetFileName)
                .Where(f => f is not null)
                .Cast<string>()
                .OrderBy(f => f)
                .ToList();

            InboxFiles = new ObservableCollection<string>(files);
            HasInboxFiles = files.Count > 0;
            Log.Information("Wizard: {Count} Dateien im Eingang geladen",
                files.Count);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Eingang konnte nicht geladen werden");
        }
    }

    /// <summary>
    /// BPM-113.06 Slice 0.6b: Laedt die Dokumenttyp-Stammdaten fuer den Schritt-4-Picker.
    /// Ohne bpm.db (z. B. isolierter Wizard/Test) bleibt die Liste leer — Schritt 4 ist
    /// dann nicht passierbar (ValidateStep4).
    /// </summary>
    private void LoadDocumentTypes(Project project)
    {
        if (_bpmDb is null) return;
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
        if (CurrentStep < TotalSteps)
        {
            CurrentStep++;
            UpdateStepState();
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            UpdateStepState();
        }
    }

    private void UpdateStepState()
    {
        StepTitle = CurrentStep switch
        {
            1 => "Schritt 1: Datei auswaehlen",
            2 => "Schritt 2: Segmente zuweisen",
            3 => "Schritt 3: Index-Konfiguration",
            4 => "Schritt 4: Zielordner",
            5 => "Schritt 5: Erkennung",
            _ => ""
        };
        CanGoBack = CurrentStep > 1;
        if (CurrentStep == 4)
            BuildHierarchyLevels();
        if (CurrentStep == 5)
            BuildRecognitionSegments();
        ValidateCurrentStep();
    }

    private void ValidateCurrentStep()
    {
        CanGoNext = CurrentStep switch
        {
            1 => ValidateStep1(),
            2 => ValidateStep2(),
            3 => ValidateStep3(),
            4 => ValidateStep4(),
            5 => ValidateStep5(),
            _ => false
        };
    }

    // === Schritt 1: Parsen ===

    [RelayCommand]
    private void ParseFileName()
    {
        if (string.IsNullOrWhiteSpace(SampleFileName))
        {
            Segments = [];
            ParseInfo = "";
            CanGoNext = false;
            return;
        }

        try
        {
            var delimiters = ParseDelimiters(DelimiterText);
            var result = FileNameParser.Parse(SampleFileName, delimiters);

            Segments = new ObservableCollection<FileNameSegment>(result.Segments);
            ParseInfo = $"{result.Segments.Count} Segmente erkannt";
            UpdateAssignedFieldTypes();
            ValidateCurrentStep();
            Log.Information("Dateiname geparst: {FileName} -> {Count} Segmente",
                SampleFileName, result.Segments.Count);
        }
        catch (Exception ex)
        {
            Segments = [];
            ParseInfo = "Fehler beim Parsen.";
            CanGoNext = false;
            Log.Warning(ex, "Fehler beim Parsen von {FileName}", SampleFileName);
        }
    }

    // === Schritt 2: Segmente zuweisen ===

    public void OnFieldTypeChanged(FileNameSegment segment, FieldTypeOption? option)
    {
        if (option is null) return;

        segment.FieldTypeId = option.FieldTypeId;
        UpdateAssignedFieldTypes();
        ValidateCurrentStep();
        OnPropertyChanged(nameof(HasPlanIndexSegment));
        OnPropertyChanged(nameof(HasPlanNumberSegment));
    }

    /// <summary>
    /// Setzt eine Segment-Zuweisung zurueck (User-Klick auf X-Button am Token).
    /// </summary>
    public void ResetSegmentFieldType(FileNameSegment segment)
    {
        segment.FieldTypeId = null;
        UpdateAssignedFieldTypes();
        ValidateCurrentStep();
        OnPropertyChanged(nameof(HasPlanIndexSegment));
        OnPropertyChanged(nameof(HasPlanNumberSegment));
    }

    // === Validierung ===

    /// <summary>Schritt 1: Mindestens 1 Segment geparst.</summary>
    private bool ValidateStep1() => Segments.Count > 0;

    /// <summary>Schritt 2: Genau ein Segment mit <see cref="SegmentSemanticRole.PlanNumber"/> Pflicht.</summary>
    private bool ValidateStep2()
    {
        if (Segments.Count == 0) return false;
        if (_segmentTypeCatalog is null) return false;
        var snap = _segmentTypeCatalog.SnapshotIncludingDeleted();
        var planNumberCount = Segments.Count(s =>
            s.FieldTypeId is { Length: > 0 } id
            && snap.TryGetValue(id, out var def)
            && def.SemanticRole == SegmentSemanticRole.PlanNumber);
        return planNumberCount == 1;
    }

    /// <summary>Schritt 3: IndexSource gueltig.</summary>
    private bool ValidateStep3()
    {
        if (SelectedIndexSource == IndexSourceType.FileName)
        {
            ShowIndexWarning = !HasPlanIndexSegment;
            return HasPlanIndexSegment;
        }
        if (SelectedIndexSource == IndexSourceType.PlanHeader)
        {
            ShowIndexWarning = false;
            return false;
        }
        ShowIndexWarning = false;
        return true;
    }

    /// <summary>Schritt 4: Ein Dokumenttyp ist gewaehlt (BPM-113.06 Slice 0.6b).</summary>
    private bool ValidateStep4() => SelectedDocumentType is not null;

    /// <summary>Schritt 5: Name + mind. 1 Segment gewaehlt.</summary>
    private bool ValidateStep5()
    {
        return !string.IsNullOrWhiteSpace(DocumentTypeName)
            && RecognitionSegments.Any(s => s.IsSelected);
    }

    // === Schritt 4: Hierarchie ===

    /// <summary>
    /// Baut die Hierarchie-Liste aus allen Profil-Segmenten deren Typ
    /// die Rolle <see cref="SegmentSemanticRole.Spatial"/> hat. BPM-108 Phase C.
    /// </summary>
    public void BuildHierarchyLevels()
    {
        var levels = new List<HierarchyLevelOption>();

        if (_segmentTypeCatalog is null)
        {
            AvailableHierarchyLevels = new ObservableCollection<HierarchyLevelOption>(levels);
            UpdateFolderPreview();
            return;
        }

        var snap = _segmentTypeCatalog.SnapshotIncludingDeleted();
        foreach (var segment in Segments.OrderBy(s => s.Position))
        {
            if (string.IsNullOrEmpty(segment.FieldTypeId)) continue;
            if (!snap.TryGetValue(segment.FieldTypeId, out var def)) continue;
            if (def.SemanticRole != SegmentSemanticRole.Spatial) continue;

            levels.Add(new HierarchyLevelOption(
                fieldTypeId: segment.FieldTypeId,
                label: def.Name,
                sampleValue: segment.RawValue));
        }

        AvailableHierarchyLevels =
            new ObservableCollection<HierarchyLevelOption>(levels);
        UpdateFolderPreview();
    }

    private void UpdateFolderPreview()
    {
        var type = SelectedDocumentType;
        if (type is null)
        {
            FolderPreview = "";
            return;
        }

        // Vorschau-Root = root_relative_path / folder_name (analog zum Resolver).
        // Die Hierarchie-Ebenen zeigen den Beispielwert; der echte Ordnername je
        // Bauteil/Geschoss kommt zur Importzeit aus dem Resolver (DB-Wahrheit).
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(type.RootRelativePath))
            parts.Add(type.RootRelativePath);
        if (!string.IsNullOrWhiteSpace(type.FolderName))
            parts.Add(type.FolderName);
        foreach (var level in AvailableHierarchyLevels)
        {
            if (level.IsSelected
                && !string.IsNullOrWhiteSpace(level.SampleValue))
                parts.Add(level.SampleValue);
        }
        FolderPreview = parts.Count > 0 ? string.Join("/", parts) + "/" : "";
    }

    public void OnHierarchyLevelChanged()
    {
        UpdateFolderPreview();
    }

    // === Schritt 5: Erkennung ===

    /// <summary>
    /// Baut klickbare Segment-Bloecke aus den Schritt-1-Segmenten.
    /// </summary>
    public void BuildRecognitionSegments()
    {
        var segments = Segments.Select(s =>
            new RecognitionSegment(s.Position, s.RawValue)).ToList();
        RecognitionSegments =
            new ObservableCollection<RecognitionSegment>(segments);
        UpdateRecognitionPattern();
    }

    /// <summary>
    /// Wird aufgerufen wenn User ein Segment an-/abklickt.
    /// </summary>
    public void OnRecognitionSegmentToggled()
    {
        UpdateRecognitionPattern();
        ValidateCurrentStep();
    }

    private void UpdateRecognitionPattern()
    {
        // BPM-082.03: Pattern-Test-Logik auf Segment-Match umgestellt.
        // Pro markiertem Segment wird (Position, RawValue) einzeln gegen den
        // FileNameParser-Output der Beispieldatei geprueft (OrdinalIgnoreCase).
        // AND-Semantik: alle markierten Segmente muessen matchen.
        var selected = RecognitionSegments
            .Where(s => s.IsSelected)
            .OrderBy(s => s.Position)
            .ToList();

        if (selected.Count == 0)
        {
            RecognitionPattern = "";
            PatternTestResult = "";
            PatternTestSuccess = false;
            RecognitionWarning = "";
            return;
        }

        // Anzeige fuer UI: "Pos N=Wert, Pos M=Wert" — macht klar dass
        // Position-basiert verglichen wird, nicht Substring.
        RecognitionPattern = string.Join(", ",
            selected.Select(s => $"Pos {s.Position}={s.RawValue}"));

        SelectedRecognitionMethod = "segment";

        // BPM-082.04 (U3): Warnung wenn markierte Segmente typischerweise
        // variabel sind (PlanNummer/Index/Datum/numerisch). Kein Hard-Fail.
        var variable = selected.Where(IsLikelyVariableSegment).ToList();
        RecognitionWarning = variable.Count == 0
            ? ""
            : "⚠ Variabel: "
              + string.Join(", ", variable.Select(s => $"Pos {s.Position}"))
              + " — Profil matcht nur Dateien mit genau diesem Wert.";

        // Test gegen Beispieldatei: gleiche Tokenisierung wie spaeter im
        // Recognizer (FileNameParser + OrdinalIgnoreCase). Damit ist die
        // Wizard-Vorschau konsistent zur Save-Logik (W1) und zum Recognizer.
        try
        {
            var delimiters = ParseDelimiters(DelimiterText);
            var parsed = FileNameParser.Parse(SampleFileName, delimiters);

            bool allMatch = selected.All(sel =>
                sel.Position < parsed.Segments.Count
                && string.Equals(
                    parsed.Segments[sel.Position].RawValue,
                    sel.RawValue,
                    StringComparison.OrdinalIgnoreCase));

            PatternTestSuccess = allMatch;
            PatternTestResult = allMatch ? "Treffer" : "Kein Treffer";
        }
        catch
        {
            PatternTestSuccess = false;
            PatternTestResult = "Test fehlgeschlagen";
        }
    }

    // === Profil speichern ===

    [RelayCommand]
    private void SaveProfile()
    {
        if (_profileManager is null || _project is null
            || string.IsNullOrWhiteSpace(_project.Paths.Root))
        {
            Log.Warning("SaveProfile: ProfileManager oder Projekt fehlt");
            return;
        }

        try
        {
            var delimiters = DelimiterText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Length == 1).Select(s => s).ToList();

            var folderHierarchy = AvailableHierarchyLevels
                .Where(h => h.IsSelected)
                .Select(h => h.FieldTypeId)
                .ToList();

            // BPM-082.03: Pro markiertem Segment eine eigene segment-Rule.
            // AND-Semantik im Recognizer matcht alle Rules gemeinsam.
            // Method=segment, Pattern=Token-Wert, SegmentPosition=0-basiert.
            var recognition = RecognitionSegments
                .Where(s => s.IsSelected && !string.IsNullOrWhiteSpace(s.RawValue))
                .OrderBy(s => s.Position)
                .Select(s => new RecognitionRule
                {
                    Method = "segment",
                    Pattern = s.RawValue,
                    SegmentPosition = s.Position
                })
                .ToList();

            // BPM-113.06 Slice 0.6b: DocumentTypeId = stabile type.Id aus den Stammdaten
            // (resolverbar in ImportPlanBuilder/DocumentTargetPathResolver). targetFolder
            // wird nur noch als Legacy-Metadatum mitgeschrieben (Entfernung in Slice 0.6c).
            var targetFolder = SelectedDocumentType?.RootRelativePath ?? "";

            var profile = _profileManager.BuildFromWizard(
                documentTypeName: DocumentTypeName,
                targetFolder: targetFolder,
                indexSource: SelectedIndexSource,
                indexModeOptional: IndexModeOptional,
                indexCaseInsensitive: IndexCaseInsensitive,
                segments: Segments.ToList(),
                delimiters: delimiters,
                folderHierarchy: folderHierarchy,
                recognition: recognition,
                recognitionPriority: RecognitionPriority,
                documentTypeId: SelectedDocumentType?.Id);

            _profileManager.Save(_project.Paths.Root, profile);
            ProfileSaved = true;

            // Save to global pattern library
            if (_templateService is not null && !string.IsNullOrEmpty(_appDataPath))
            {
                var template = _templateService.ExtractFromProfile(profile, _project.Name);
                _templateService.AddOrUpdate(_appDataPath, template);
            }

            Log.Information("Profil gespeichert: {Name} fuer Projekt {Project}",
                DocumentTypeName, _project.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Speichern des Profils");
        }
    }

    // === Helpers ===

    /// <summary>
    /// BPM-082.04 (U3): Heuristik aus Review R2-Konsens — Segment ist mit hoher
    /// Wahrscheinlichkeit variabel (waechst pro Datei mit) und damit als
    /// Erkennungs-Kriterium riskant. Warnung im Wizard, kein Hard-Fail.
    /// </summary>
    /// <remarks>
    /// BPM-108 Phase C: Trigger ist die <see cref="SegmentSemanticRole"/> des zugewiesenen
    /// Segmenttyps (<c>PlanNumber</c> / <c>PlanIndex</c> / <c>Date</c>). Fallback: rein
    /// numerisch oder als Datum parsbar.
    /// </remarks>
    internal bool IsLikelyVariableSegment(RecognitionSegment seg)
    {
        var fieldTypeId = Segments
            .FirstOrDefault(s => s.Position == seg.Position)
            ?.FieldTypeId;
        var role = GetRoleForFieldTypeId(fieldTypeId);
        if (role is SegmentSemanticRole.PlanNumber
                  or SegmentSemanticRole.PlanIndex
                  or SegmentSemanticRole.Date)
            return true;

        var value = seg.RawValue?.Trim() ?? "";
        if (string.IsNullOrEmpty(value)) return false;
        if (value.All(char.IsDigit)) return true;
        if (DateTime.TryParse(value, out _)) return true;
        return false;
    }

    private static char[] ParseDelimiters(string text)
    {
        var chars = new List<char>();
        foreach (var part in text.Split(' ',
            StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Length == 1)
                chars.Add(part[0]);
        }
        return chars.Count > 0 ? chars.ToArray() : ['-', '_'];
    }

    /// <summary>
    /// Baut die Chip-Liste fuer Wizard-Schritt 2 aus dem aktiven Segmenttyp-Katalog auf.
    /// </summary>
    /// <remarks>
    /// BPM-108 Phase C: Anstelle der hardcoded Enum-Liste werden alle aktiven Built-in-
    /// und Custom-Segmenttypen aus dem Katalog uebernommen. Der "+ Eigenes"-Chip bleibt
    /// als Sonder-Marker (FieldTypeId == null, IsCustomCreate == true) am Listenende —
    /// die Inline-Popover-Anlage folgt in einem spaeteren Commit.
    /// </remarks>
    private void RebuildFieldTypeOptions()
    {
        var options = new ObservableCollection<FieldTypeOption>();
        options.Add(new FieldTypeOption(displayName: "-- Nicht zugewiesen", fieldTypeId: null));

        if (_segmentTypeCatalog is not null)
        {
            foreach (var def in _segmentTypeCatalog.GetEffectiveActive())
            {
                options.Add(new FieldTypeOption(displayName: def.Name, fieldTypeId: def.Id));
            }
        }

        options.Add(new FieldTypeOption(
            displayName: "+ Eigenes",
            fieldTypeId: null,
            isCustomCreate: true));

        FieldTypeOptions = options;
        UpdateAssignedFieldTypes();
    }

    /// <summary>
    /// Aktualisiert IsAssigned-State pro FieldTypeOption — fuer Chip-Highlight
    /// in Wizard-Schritt 2 nach Drag&Drop-Zuweisung.
    /// </summary>
    private void UpdateAssignedFieldTypes()
    {
        var assigned = Segments
            .Where(s => !string.IsNullOrEmpty(s.FieldTypeId))
            .Select(s => s.FieldTypeId!)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var opt in FieldTypeOptions)
        {
            opt.IsAssigned = opt.FieldTypeId is { Length: > 0 } id && assigned.Contains(id);
        }
    }

    // === Inline-Popover "+ Eigenes" Commands ===

    /// <summary>
    /// Oeffnet den Inline-Popover. Optional <paramref name="assignmentTarget"/> = das aktuell
    /// markierte Segment, dem der neue Typ nach Anlage direkt zugewiesen werden soll.
    /// </summary>
    public void OpenCustomPopover(FileNameSegment? assignmentTarget = null)
    {
        _customAssignmentTarget = assignmentTarget;
        CustomTypeName = "";
        CustomTypeColor = "#A87142";
        CustomTypeError = "";
        ShowCustomPopover = true;
        OnPropertyChanged(nameof(CustomTypeTokenPreview));
    }

    [RelayCommand]
    private void CancelCustomPopover()
    {
        ShowCustomPopover = false;
        _customAssignmentTarget = null;
        CustomTypeName = "";
        CustomTypeError = "";
    }

    [RelayCommand]
    private void CreateCustomType()
    {
        // Validierung Name
        if (string.IsNullOrWhiteSpace(CustomTypeName))
        {
            CustomTypeError = "Name ist erforderlich.";
            return;
        }

        if (_segmentTypeRepository is null
            || _segmentTypeCatalog is null
            || _idGenerator is null)
        {
            CustomTypeError = "Custom-Anlage in dieser Konfiguration nicht moeglich.";
            Log.Warning("CreateCustomType: Repository/Catalog/IdGenerator fehlt — Wizard isoliert?");
            return;
        }

        var baseKey = TokenKeyGenerator.Normalize(CustomTypeName);
        if (string.IsNullOrEmpty(baseKey))
        {
            CustomTypeError = "Name enthaelt keine zulaessigen Zeichen.";
            return;
        }

        var tokenKey = TokenKeyGenerator.EnsureUnique(baseKey,
            isTaken: key => _segmentTypeRepository.TokenKeyExists(key));

        var newType = new SegmentTypeDefinition
        {
            Id = _idGenerator.NewId(),
            Name = CustomTypeName.Trim(),
            Color = CustomTypeColor,
            TokenKey = tokenKey,
            SemanticRole = null, // Custom rein dekorativ (CGR Sign-off)
            GroupId = "grp_eigene",
            SortOrder = NextCustomSortOrder(),
            IsActive = true,
            IsBuiltin = false
        };

        try
        {
            _segmentTypeRepository.SaveType(newType);
            _segmentTypeCatalog.Invalidate();

            Log.Information("BPM-108: Custom-Segmenttyp angelegt: {Name} ({Id}, token_key={Token})",
                newType.Name, newType.Id, newType.TokenKey);

            // Direkt-zuweisen wenn ein aktives Segment vorgemerkt ist
            if (_customAssignmentTarget is not null)
            {
                _customAssignmentTarget.FieldTypeId = newType.Id;
                UpdateAssignedFieldTypes();
                ValidateCurrentStep();
                OnPropertyChanged(nameof(HasPlanIndexSegment));
                OnPropertyChanged(nameof(HasPlanNumberSegment));
            }

            ShowCustomPopover = false;
            _customAssignmentTarget = null;
            CustomTypeName = "";
            CustomTypeError = "";
        }
        catch (Exception ex)
        {
            CustomTypeError = "Speichern fehlgeschlagen — Details im Log.";
            Log.Error(ex, "BPM-108: Custom-Anlage fehlgeschlagen");
        }
    }

    /// <summary>
    /// Liefert die naechste freie sort_order fuer neue Custom-Typen in <c>grp_eigene</c>
    /// (max + 10). Faellt auf 10 zurueck wenn keine vorhanden.
    /// </summary>
    private int NextCustomSortOrder()
    {
        if (_segmentTypeCatalog is null) return 10;
        var customs = _segmentTypeCatalog.GetEffectiveActive()
            .Where(t => t.GroupId == "grp_eigene")
            .ToList();
        if (customs.Count == 0) return 10;
        return customs.Max(t => t.SortOrder) + 10;
    }
}

// === Helper-Klassen ===

/// <summary>
/// Eine Option in der Chip-Liste von Wizard-Schritt 2 (BPM-108 Phase C).
/// </summary>
/// <remarks>
/// Spezialfaelle:
/// <list type="bullet">
/// <item><see cref="FieldTypeId"/> = null + <see cref="IsCustomCreate"/> = false → "-- Nicht zugewiesen" (Reset-Option).</item>
/// <item><see cref="FieldTypeId"/> = null + <see cref="IsCustomCreate"/> = true → "+ Eigenes" (Inline-Popover-Trigger, Phase 4 commit).</item>
/// <item>Sonst: regulaerer Segmenttyp aus dem Catalog.</item>
/// </list>
/// </remarks>
public class FieldTypeOption : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isAssigned;

    public string DisplayName { get; }
    public string? FieldTypeId { get; }
    public bool IsCustomCreate { get; }

    /// <summary>
    /// True wenn dieser Segmenttyp bereits einem Segment zugewiesen ist (Wizard-Schritt 2).
    /// </summary>
    public bool IsAssigned
    {
        get => _isAssigned;
        set
        {
            if (_isAssigned != value)
            {
                _isAssigned = value;
                PropertyChanged?.Invoke(this,
                    new System.ComponentModel.PropertyChangedEventArgs(nameof(IsAssigned)));
            }
        }
    }

    public FieldTypeOption(string displayName, string? fieldTypeId, bool isCustomCreate = false)
    {
        DisplayName = displayName;
        FieldTypeId = fieldTypeId;
        IsCustomCreate = isCustomCreate;
    }

    public override string ToString() => DisplayName;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public class IndexSourceOption
{
    public string Label { get; }
    public IndexSourceType Value { get; }
    public string Description { get; }
    public bool IsEnabled { get; }

    public IndexSourceOption(string label, IndexSourceType value,
        string description, bool isEnabled = true)
    {
        Label = label;
        Value = value;
        Description = description;
        IsEnabled = isEnabled;
    }

    public override string ToString() => Label;
}

/// <summary>
/// Eine Hierarchie-Ebene fuer Wizard-Schritt 4. BPM-108 Phase C:
/// <see cref="FieldTypeId"/> ist die stabile <c>segment_types.id</c>
/// (z. B. "geschoss"), <see cref="Label"/> kommt aus dem Catalog.
/// </summary>
public class HierarchyLevelOption : ObservableObject
{
    public string FieldTypeId { get; }
    public string Label { get; }
    public string SampleValue { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public HierarchyLevelOption(string fieldTypeId, string label, string sampleValue)
    {
        FieldTypeId = fieldTypeId;
        Label = label;
        SampleValue = sampleValue;
    }
}

/// <summary>
/// Klickbarer Segment-Block fuer Schritt 5 (Erkennung).
/// </summary>
public class RecognitionSegment : ObservableObject
{
    public int Position { get; }
    public string RawValue { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public RecognitionSegment(int position, string rawValue)
    {
        Position = position;
        RawValue = rawValue;
    }
}

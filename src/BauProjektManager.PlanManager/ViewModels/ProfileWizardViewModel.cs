using System.Collections.ObjectModel;
using System.IO;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel fuer den 5-Schritt Profil-Wizard.
/// Schritt 1: Datei auswaehlen + Parsen.
/// Schritt 2: Segmente zuweisen (FieldType-Dropdowns).
/// Schritt 3: Index-Konfiguration.
/// Schritt 4: Zielordner + Ordner-Hierarchie.
/// Schritt 5: Erkennung (klickbare Segmente).
/// </summary>
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
    /// Verfuegbare Feldtypen fuer das Dropdown (Schritt 2).
    /// </summary>
    public List<FieldTypeOption> FieldTypeOptions { get; } = BuildFieldTypeOptions();

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
    private string _selectedRecognitionMethod = "contains";

    [ObservableProperty]
    private int _recognitionPriority = 100;

    [ObservableProperty]
    private string _patternTestResult = "";

    [ObservableProperty]
    private bool _patternTestSuccess;

    public bool IsPrefix
    {
        get => SelectedRecognitionMethod == "prefix";
        set
        {
            SelectedRecognitionMethod = value ? "prefix" : "contains";
            OnPropertyChanged();
        }
    }

    public bool HasPlanIndexSegment =>
        Segments.Any(s => s.FieldType == FieldType.PlanIndex);

    /// <summary>
    /// True wenn das Profil erfolgreich gespeichert wurde.
    /// Wird vom Dialog abgefragt um DialogResult zu setzen.
    /// </summary>
    public bool ProfileSaved { get; private set; }

    private readonly ProfileManager? _profileManager;
    private readonly PatternTemplateService? _templateService;
    private readonly Project? _project;
    private readonly string? _appDataPath;

    public ProfileWizardViewModel(
        Project? project = null,
        ProfileManager? profileManager = null,
        PatternTemplateService? templateService = null,
        string? appDataPath = null)
    {
        _project = project;
        _profileManager = profileManager;
        _templateService = templateService;
        _appDataPath = appDataPath;
        if (project is not null)
            LoadInboxFiles(project);
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

    partial void OnDocumentTypeNameChanged(string value)
    {
        ValidateCurrentStep();
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

        segment.FieldType = option.Value;
        segment.CustomFieldName = option.Value == FieldType.Custom
            ? option.DisplayName : null;
        ValidateCurrentStep();
        OnPropertyChanged(nameof(HasPlanIndexSegment));
    }

    // === Validierung ===

    /// <summary>Schritt 1: Mindestens 1 Segment geparst.</summary>
    private bool ValidateStep1() => Segments.Count > 0;

    /// <summary>Schritt 2: PlanNumber muss zugewiesen sein.</summary>
    private bool ValidateStep2() =>
        Segments.Count > 0
        && Segments.Any(s => s.FieldType == FieldType.PlanNumber);

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

    /// <summary>Schritt 4: Zielordner nicht leer.</summary>
    private bool ValidateStep4()
    {
        if (UseCustomFolder)
            return !string.IsNullOrWhiteSpace(CustomFolderName);
        return !string.IsNullOrWhiteSpace(SelectedTargetFolder);
    }

    /// <summary>Schritt 5: Name + mind. 1 Segment gewaehlt.</summary>
    private bool ValidateStep5()
    {
        return !string.IsNullOrWhiteSpace(DocumentTypeName)
            && RecognitionSegments.Any(s => s.IsSelected);
    }

    // === Schritt 4: Hierarchie ===

    public void BuildHierarchyLevels()
    {
        var hierarchyFieldTypes = new[]
        {
            (FieldType.Geschoss, "Geschoss"),
            (FieldType.Haus, "Haus"),
            (FieldType.Bauteil, "Bauteil"),
            (FieldType.Bauabschnitt, "Bauabschnitt"),
            (FieldType.Stiege, "Stiege"),
            (FieldType.Zone, "Zone"),
            (FieldType.Block, "Block"),
        };

        var levels = new List<HierarchyLevelOption>();
        foreach (var (fieldType, label) in hierarchyFieldTypes)
        {
            var segment = Segments.FirstOrDefault(
                s => s.FieldType == fieldType);
            if (segment is not null)
                levels.Add(new HierarchyLevelOption(
                    fieldType, label, segment.RawValue));
        }

        AvailableHierarchyLevels =
            new ObservableCollection<HierarchyLevelOption>(levels);
        UpdateFolderPreview();
    }

    private void UpdateFolderPreview()
    {
        var folder = UseCustomFolder ? CustomFolderName : SelectedTargetFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            FolderPreview = "";
            return;
        }

        var parts = new List<string> { folder };
        foreach (var level in AvailableHierarchyLevels)
        {
            if (level.IsSelected
                && !string.IsNullOrWhiteSpace(level.SampleValue))
                parts.Add(level.SampleValue);
        }
        FolderPreview = string.Join("/", parts) + "/";
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
        var selected = RecognitionSegments
            .Where(s => s.IsSelected)
            .OrderBy(s => s.Position)
            .ToList();

        if (selected.Count == 0)
        {
            RecognitionPattern = "";
            PatternTestResult = "";
            PatternTestSuccess = false;
            return;
        }

        // Muster aus gewaehlten Segmenten bauen
        // Trennzeichen zwischen Segmenten mitnehmen
        var pattern = string.Join("", selected.Select(s => s.RawValue));
        // Wenn benachbarte Segmente: Trennzeichen dazwischen
        var delimiters = ParseDelimiters(DelimiterText);
        if (selected.Count >= 1)
        {
            var parts = new List<string>();
            for (int i = 0; i < selected.Count; i++)
            {
                parts.Add(selected[i].RawValue);
                if (i < selected.Count - 1)
                {
                    // Trennzeichen zwischen Segmenten
                    int gap = selected[i + 1].Position
                              - selected[i].Position;
                    if (gap == 1 && delimiters.Length > 0)
                        parts.Add(delimiters[0].ToString());
                }
            }
            pattern = string.Join("", parts);
        }

        RecognitionPattern = pattern;

        // Auto-Methode: erstes Segment = prefix, sonst contains
        bool isFirst = selected[0].Position == 0;
        SelectedRecognitionMethod = isFirst ? "prefix" : "contains";
        OnPropertyChanged(nameof(IsPrefix));

        // Test gegen Beispieldatei
        bool match = isFirst
            ? SampleFileName.StartsWith(pattern,
                StringComparison.OrdinalIgnoreCase)
            : SampleFileName.Contains(pattern,
                StringComparison.OrdinalIgnoreCase);

        PatternTestSuccess = match;
        PatternTestResult = match ? "Treffer" : "Kein Treffer";
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
                .Select(h => h.FieldType.ToString().ToLowerInvariant())
                .ToList();

            var recognition = new List<RecognitionRule>();
            if (!string.IsNullOrWhiteSpace(RecognitionPattern))
            {
                recognition.Add(new RecognitionRule
                {
                    Method = SelectedRecognitionMethod,
                    Pattern = RecognitionPattern
                });
            }

            var targetFolder = UseCustomFolder ? CustomFolderName : SelectedTargetFolder;

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
                recognitionPriority: RecognitionPriority);

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

    private static List<FieldTypeOption> BuildFieldTypeOptions()
    {
        return
        [
            new("-- Nicht zugewiesen", null),
            new("Plannummer", FieldType.PlanNumber),
            new("Index", FieldType.PlanIndex),
            new("Projektnummer", FieldType.ProjectNumber),
            new("Bezeichnung", FieldType.Description),
            new("Ignorieren", FieldType.Ignore),
            new("Datum", FieldType.Datum),
            new("Geschoss", FieldType.Geschoss),
            new("Haus", FieldType.Haus),
            new("Planart", FieldType.Planart),
            new("Objekt", FieldType.Objekt),
            new("Bauteil", FieldType.Bauteil),
            new("Bauabschnitt", FieldType.Bauabschnitt),
            new("Stiege", FieldType.Stiege),
            new("Achse", FieldType.Achse),
            new("Zone", FieldType.Zone),
            new("Block", FieldType.Block),
        ];
    }
}

// === Helper-Klassen ===

public class FieldTypeOption
{
    public string DisplayName { get; }
    public FieldType? Value { get; }

    public FieldTypeOption(string displayName, FieldType? value)
    {
        DisplayName = displayName;
        Value = value;
    }

    public override string ToString() => DisplayName;
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

public class HierarchyLevelOption : ObservableObject
{
    public FieldType FieldType { get; }
    public string Label { get; }
    public string SampleValue { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public HierarchyLevelOption(FieldType fieldType, string label,
        string sampleValue)
    {
        FieldType = fieldType;
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

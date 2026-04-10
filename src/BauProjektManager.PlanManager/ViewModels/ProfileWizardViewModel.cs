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
/// ViewModel für den 4-Schritt Profil-Wizard.
/// Schritt 1: Dateiname → Segmente zuweisen.
/// Schritt 2: IndexSource → FileName/None, indexMode, indexComparison.
/// Schritt 3: Zielordner + Ordner-Hierarchie.
/// Schritt 4: kommt in späterer Version.
/// </summary>
public partial class ProfileWizardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private int _totalSteps = 4;

    [ObservableProperty]
    private string _stepTitle = "Schritt 1: Segmente zuweisen";

    // === Dateien im Eingang ===

    [ObservableProperty]
    private ObservableCollection<string> _inboxFiles = [];

    [ObservableProperty]
    private string? _selectedInboxFile;

    [ObservableProperty]
    private bool _hasInboxFiles;

    // === Schritt 1: Dateiname + Segmente ===

    [ObservableProperty]
    private string _sampleFileName = "";

    [ObservableProperty]
    private string _delimiterText = "- _";

    [ObservableProperty]
    private ObservableCollection<FileNameSegment> _segments = [];

    [ObservableProperty]
    private string _parseInfo = "";

    /// <summary>
    /// Verfügbare Feldtypen für das Dropdown.
    /// </summary>
    public List<FieldTypeOption> FieldTypeOptions { get; } = BuildFieldTypeOptions();

    /// <summary>
    /// Wird true wenn der aktuelle Schritt gültig ist.
    /// </summary>
    [ObservableProperty]
    private bool _canGoNext;

    /// <summary>
    /// Wird true ab Schritt 2 (Zurück-Button sichtbar).
    /// </summary>
    [ObservableProperty]
    private bool _canGoBack;

    // === Schritt 2: IndexSource ===

    /// <summary>
    /// Verfügbare IndexSource-Optionen für RadioButtons.
    /// </summary>
    public List<IndexSourceOption> IndexSourceOptions { get; } =
    [
        new("Aus Dateiname", IndexSourceType.FileName,
            "Index wird aus einem Segment im Dateinamen gelesen (z.B. A, B, C)"),
        new("Kein Index", IndexSourceType.None,
            "Dokument hat keinen Index. Versionen werden per MD5-Hash erkannt."),
        new("Aus Plankopf (Post-V1)", IndexSourceType.PlanHeader,
            "Index wird aus dem PDF-Plankopf gelesen. Noch nicht verfügbar.", isEnabled: false)
    ];

    [ObservableProperty]
    private IndexSourceType _selectedIndexSource = IndexSourceType.FileName;

    /// <summary>
    /// Ob IndexMode-Optionen sichtbar sind (nur bei FileName).
    /// </summary>
    [ObservableProperty]
    private bool _showIndexModeOptions = true;

    [ObservableProperty]
    private bool _indexModeOptional = true;

    [ObservableProperty]
    private bool _indexCaseInsensitive = true;

    /// <summary>
    /// Warnung wenn FileName gewählt aber kein PlanIndex-Segment zugewiesen.
    /// </summary>
    [ObservableProperty]
    private bool _showIndexWarning;

    // === Schritt 3: Zielordner ===

    /// <summary>
    /// Standard-Ordner aus typischer BPM-Projektstruktur.
    /// </summary>
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

    /// <summary>
    /// Verfügbare Hierarchie-Ebenen — nur FieldTypes die in Schritt 1 zugewiesen wurden.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<HierarchyLevelOption> _availableHierarchyLevels = [];

    /// <summary>
    /// Live-Vorschau des Zielpfads.
    /// </summary>
    [ObservableProperty]
    private string _folderPreview = "";

    /// <summary>
    /// Ob PlanIndex-Segment in Schritt 1 zugewiesen wurde.
    /// </summary>
    public bool HasPlanIndexSegment => Segments.Any(s => s.FieldType == FieldType.PlanIndex);

    public ProfileWizardViewModel(Project? project = null)
    {
        if (project is not null)
            LoadInboxFiles(project);
    }

    /// <summary>
    /// Wird aufgerufen wenn der User eine Datei aus der Eingang-Liste anklickt.
    /// </summary>
    partial void OnSelectedInboxFileChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            SampleFileName = value;
            ParseFileNameCommand.Execute(null);
        }
    }

    /// <summary>
    /// Aktualisiert UI wenn IndexSource gewechselt wird.
    /// </summary>
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

    private void LoadInboxFiles(Project project)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(project.Paths.Root))
                return;

            var inboxPath = Path.Combine(project.Paths.Root, project.Paths.Inbox);
            if (!Directory.Exists(inboxPath))
                return;

            var files = Directory.GetFiles(inboxPath)
                .Select(Path.GetFileName)
                .Where(f => f is not null)
                .Cast<string>()
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
            1 => "Schritt 1: Segmente zuweisen",
            2 => "Schritt 2: Index-Konfiguration",
            3 => "Schritt 3: Zielordner",
            4 => "Schritt 4: Erkennung",
            _ => ""
        };
        CanGoBack = CurrentStep > 1;
        if (CurrentStep == 3)
            BuildHierarchyLevels();
        ValidateCurrentStep();
    }

    private void ValidateCurrentStep()
    {
        CanGoNext = CurrentStep switch
        {
            1 => ValidateStep1(),
            2 => ValidateStep2(),
            3 => ValidateStep3(),
            _ => false // Schritt 4 kommt noch
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
            ParseInfo = $"{result.Segments.Count} Segmente erkannt · Trennzeichen: {string.Join(" ", result.UsedDelimiters)}";
            ValidateCurrentStep();
            Log.Information("Dateiname geparst: {FileName} → {Count} Segmente",
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

    public void OnFieldTypeChanged(FileNameSegment segment, FieldTypeOption? option)
    {
        if (option is null) return;

        segment.FieldType = option.Value;
        segment.CustomFieldName = option.Value == FieldType.Custom ? option.DisplayName : null;
        ValidateCurrentStep();
        OnPropertyChanged(nameof(HasPlanIndexSegment));
    }

    private bool ValidateStep1()
    {
        return Segments.Count > 0
            && Segments.Any(s => s.FieldType == FieldType.PlanNumber);
    }

    private bool ValidateStep2()
    {
        // FileName erfordert PlanIndex-Segment aus Schritt 1
        if (SelectedIndexSource == IndexSourceType.FileName)
        {
            ShowIndexWarning = !HasPlanIndexSegment;
            return HasPlanIndexSegment;
        }

        // PlanHeader ist Post-V1, nicht wählbar
        if (SelectedIndexSource == IndexSourceType.PlanHeader)
        {
            ShowIndexWarning = false;
            return false;
        }

        // None ist immer gültig
        ShowIndexWarning = false;
        return true;
    }

    private bool ValidateStep3()
    {
        if (UseCustomFolder)
            return !string.IsNullOrWhiteSpace(CustomFolderName);

        return !string.IsNullOrWhiteSpace(SelectedTargetFolder);
    }

    /// <summary>
    /// Baut die Hierarchie-Ebenen basierend auf den in Schritt 1 zugewiesenen Segmenten.
    /// Wird beim Wechsel zu Schritt 3 aufgerufen.
    /// </summary>
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
            var segment = Segments.FirstOrDefault(s => s.FieldType == fieldType);
            if (segment is not null)
            {
                levels.Add(new HierarchyLevelOption(fieldType, label, segment.RawValue));
            }
        }

        AvailableHierarchyLevels = new ObservableCollection<HierarchyLevelOption>(levels);
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
            if (level.IsSelected && !string.IsNullOrWhiteSpace(level.SampleValue))
                parts.Add(level.SampleValue);
        }

        FolderPreview = string.Join("/", parts) + "/";
    }

    /// <summary>
    /// Wird vom Code-Behind aufgerufen wenn eine Hierarchie-Checkbox geändert wird.
    /// </summary>
    public void OnHierarchyLevelChanged()
    {
        UpdateFolderPreview();
    }

    private static char[] ParseDelimiters(string text)
    {
        var chars = new List<char>();
        foreach (var part in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Length == 1)
                chars.Add(part[0]);
        }
        return chars.Count > 0 ? chars.ToArray() : ['-', '_'];
    }

    private static List<FieldTypeOption> BuildFieldTypeOptions()
    {
        var options = new List<FieldTypeOption>
        {
            new("— Nicht zugewiesen", null),
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
        };
        return options;
    }
}

/// <summary>
/// Option für das FieldType-Dropdown mit deutschem Anzeigenamen.
/// </summary>
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

/// <summary>
/// Option für IndexSource-RadioButtons.
/// </summary>
public class IndexSourceOption
{
    public string Label { get; }
    public IndexSourceType Value { get; }
    public string Description { get; }
    public bool IsEnabled { get; }

    public IndexSourceOption(string label, IndexSourceType value, string description, bool isEnabled = true)
    {
        Label = label;
        Value = value;
        Description = description;
        IsEnabled = isEnabled;
    }

    public override string ToString() => Label;
}

/// <summary>
/// Option fuer Unterordner-Hierarchie-Checkbox.
/// </summary>
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

    public HierarchyLevelOption(FieldType fieldType, string label, string sampleValue)
    {
        FieldType = fieldType;
        Label = label;
        SampleValue = sampleValue;
    }
}

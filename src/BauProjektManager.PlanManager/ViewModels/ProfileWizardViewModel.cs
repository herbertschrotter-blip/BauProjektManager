using System.Collections.ObjectModel;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// ViewModel für den 4-Schritt Profil-Wizard.
/// Schritt 1: Dateiname → Segmente zuweisen.
/// Schritte 2–4: kommen in späteren Versionen.
/// </summary>
public partial class ProfileWizardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private int _totalSteps = 4;

    [ObservableProperty]
    private string _stepTitle = "Schritt 1: Segmente zuweisen";

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
    /// Wird true wenn Schritt 1 gültig ist (mind. 1 Segment + PlanNumber zugewiesen).
    /// </summary>
    [ObservableProperty]
    private bool _canGoNext;

    public ProfileWizardViewModel()
    {
    }

    /// <summary>
    /// Parst den Beispiel-Dateinamen mit den gewählten Trennzeichen.
    /// Wird aufgerufen wenn der User den Dateinamen ändert oder Enter drückt.
    /// </summary>
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
            ValidateStep1();
            Log.Information("Dateiname geparst: {FileName} → {Count} Segmente", SampleFileName, result.Segments.Count);
        }
        catch (Exception ex)
        {
            Segments = [];
            ParseInfo = "Fehler beim Parsen.";
            CanGoNext = false;
            Log.Warning(ex, "Fehler beim Parsen von {FileName}", SampleFileName);
        }
    }

    /// <summary>
    /// Wird aufgerufen wenn der User einen FieldType für ein Segment wählt.
    /// </summary>
    public void OnFieldTypeChanged(FileNameSegment segment, FieldTypeOption? option)
    {
        if (option is null) return;

        segment.FieldType = option.Value;
        segment.CustomFieldName = option.Value == FieldType.Custom ? option.DisplayName : null;
        ValidateStep1();
    }

    private void ValidateStep1()
    {
        CanGoNext = Segments.Count > 0
            && Segments.Any(s => s.FieldType == FieldType.PlanNumber);
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
            new("Geschoß", FieldType.Geschoss),
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

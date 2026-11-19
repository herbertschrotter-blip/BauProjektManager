using System.IO;
using System.Collections.ObjectModel;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Schritt 4 des Profil-Wizards: Erkennung (Kacheln anklicken) und Speichern am Dokumenttyp. Die Regeln sind
/// positionsfrei (Token an beliebiger Stelle, ADR-065 / BPM-121 Positions-Falle).
/// </summary>
public partial class ProfileWizardViewModel
{
    [ObservableProperty]
    private ObservableCollection<RecognitionSegment> _recognitionSegments = [];

    [ObservableProperty]
    private string _recognitionPattern = "";

    [ObservableProperty]
    private string _patternTestResult = "";

    [ObservableProperty]
    private bool _patternTestSuccess;

    /// <summary>Warnung bei typischerweise variablen Kacheln (Plannummer, Index, Datum, Zahl) — kein Hard-Fail.</summary>
    [ObservableProperty]
    private string _recognitionWarning = "";

    [ObservableProperty]
    private int _recognitionPriority = 100;

    /// <summary>
    /// Kacheln aus Schritt 2 als klickbare Bloecke. Bereits gewaehlte Werte bleiben gewaehlt; beim ersten Mal ist
    /// die Kachel vorgewaehlt, die wie der Dokumenttyp heisst (Mockup 3.8: „Polierplan“).
    /// </summary>
    public void BuildRecognitionSegments()
    {
        var vorher = RecognitionSegments.Where(s => s.IsSelected).Select(s => s.RawValue)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (vorher.Count == 0 && _existingProfile is not null)
            // „Erkennung bearbeiten“: die Kacheln der vorhandenen Segment-Regeln vorwaehlen
            vorher = _existingProfile.Recognition.Where(r => !IsRegexRule(r)).Select(r => r.Pattern)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var typName = SelectedDocumentType?.Name;
        RecognitionSegments = new ObservableCollection<RecognitionSegment>(Segments
            .Where(s => GetRoleForFieldTypeId(s.FieldTypeId) != SegmentSemanticRole.Ignore)
            .Select(s => new RecognitionSegment(s.Position, s.RawValue)
            {
                IsSelected = vorher.Count > 0
                    ? vorher.Contains(s.RawValue)
                    : string.Equals(s.RawValue, typName, StringComparison.OrdinalIgnoreCase)
            }));
        UpdateRecognitionPattern();
    }

    /// <summary>Nach jedem Klick auf eine Kachel.</summary>
    public void OnRecognitionSegmentToggled()
    {
        UpdateRecognitionPattern();
        ValidateCurrentStep();
    }

    /// <summary>Schritt 4: mindestens eine Kachel gewaehlt.</summary>
    private bool ValidateStep4() => RecognitionSegments.Any(s => s.IsSelected);

    private void UpdateRecognitionPattern()
    {
        var selected = RecognitionSegments.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
        {
            (RecognitionPattern, PatternTestResult, PatternTestSuccess, RecognitionWarning) = ("", "", false, "");
            return;
        }

        RecognitionPattern = "Erkennung: Dateiname enthält " +
                             string.Join(" und ", selected.Select(s => $"„{s.RawValue}“")) +
                             " – an beliebiger Stelle (ein fehlender Index verschiebt alles dahinter).";

        var variable = selected.Where(IsLikelyVariableSegment).ToList();
        RecognitionWarning = variable.Count == 0
            ? ""
            : "⚠ Variabel: " + string.Join(", ", variable.Select(s => $"„{s.RawValue}“")) +
              " — die Erkennung trifft nur Dateien mit genau diesem Wert.";

        // Gegenprobe wie der Recognizer: dieselbe Zerlegung, Token irgendwo im Namen
        var tokens = string.IsNullOrWhiteSpace(EditorFileName)
            ? []
            : FileNameParser.Parse(EditorFileName, [.. Delimiters.Select(d => d[0])]).Segments.Select(s => s.RawValue).ToList();
        PatternTestSuccess = selected.All(sel =>
            tokens.Any(t => string.Equals(t, sel.RawValue, StringComparison.OrdinalIgnoreCase)));
        PatternTestResult = PatternTestSuccess ? "Treffer bei der Beispieldatei" : "Kein Treffer bei der Beispieldatei";
    }

    private static bool IsRegexRule(RecognitionRule rule)
        => string.Equals(rule.Method, "regex", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// BPM-082.04: Heuristik — Kachel ist vermutlich variabel (waechst pro Datei mit). Warnung, kein Hard-Fail.
    /// </summary>
    internal bool IsLikelyVariableSegment(RecognitionSegment seg)
    {
        var fieldTypeId = Segments.FirstOrDefault(s => s.Position == seg.Position)?.FieldTypeId;
        if (GetRoleForFieldTypeId(fieldTypeId) is SegmentSemanticRole.PlanNumber
            or SegmentSemanticRole.PlanIndex or SegmentSemanticRole.Date)
            return true;
        var value = seg.RawValue?.Trim() ?? "";
        if (string.IsNullOrEmpty(value))
            return false;
        return value.All(char.IsDigit) || DateTime.TryParse(value, out _);
    }

    /// <summary>Profil bauen (<see cref="ProfileBuilder"/>) und am Dokumenttyp speichern; ersetzt ein vorhandenes.</summary>
    [RelayCommand]
    private void SaveProfile()
    {
        SaveError = "";
        if (_profileManager is null || _project is null || string.IsNullOrWhiteSpace(_project.Paths.Root)
            || SelectedDocumentType is null)
        {
            SaveError = "Speichern nicht möglich: Projekt oder Dokumenttyp fehlt.";
            return;
        }

        var recognition = RecognitionSegments
            .Where(s => s.IsSelected && !string.IsNullOrWhiteSpace(s.RawValue))
            .Select(s => new RecognitionRule { Method = "segment", Pattern = s.RawValue, SegmentPosition = null })
            .ToList();
        // Regex-Regel aus dem Tab „Erkennung“ (BPM-121.00) bleibt beim Ersetzen erhalten
        if (_existingProfile is not null)
            recognition.AddRange(_existingProfile.Recognition.Where(IsRegexRule));
        var profile = ProfileBuilder.Build(_segmentTypeCatalog, SelectedDocumentType, SelectedIndexSource,
            indexModeOptional: true, IndexCaseInsensitive, [.. Segments], Delimiters, recognition,
            RecognitionPriority, _existingProfile?.Id);

        try
        {
            _profileManager.Save(_project.Paths.Root, profile);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            SaveError = "Speichern fehlgeschlagen: " + ex.Message;
            Log.Warning("Wizard: Profil nicht gespeichert ({Error})", ex.GetType().Name);
            return;
        }
        ProfileSaved = true;

        if (_templateService is not null && !string.IsNullOrEmpty(_appDataPath))
            _templateService.AddOrUpdate(_appDataPath, _templateService.ExtractFromProfile(profile, _project.Name));

        Log.Information("Wizard: Erkennung fuer Dokumenttyp {TypeId} gespeichert ({Mode})",
            profile.DocumentTypeId, _existingProfile is null ? "neu" : "ersetzt");
    }
}

/// <summary>Klickbare Kachel in Schritt 4 (Erkennung).</summary>
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

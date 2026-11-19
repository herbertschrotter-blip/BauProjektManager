using System.IO;
using System.Text.RegularExpressions;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Dialog „Regex-Regel…“ im Tab „Erkennung“ (Seitenliste 3.6 v2.1, BPM-121.00): eine Regel mit
/// <c>Method = "regex"</c> am Erkennungsprofil eines Dokumenttyps anlegen oder ersetzen. Das Muster wird vor dem
/// Speichern mit derselben Zeitgrenze wie im <see cref="DocumentTypeRecognizer"/> geprueft (ReDoS-Schutz);
/// <see cref="RecognitionRule.IsValid"/> bleibt syntaxfrei. Der Live-Test prueft wie der Recognizer den Namen ohne Endung.
/// </summary>
public partial class RegexRuleViewModel : ObservableObject
{
    private readonly IProfileManager _profileManager;
    private readonly string _projectRoot;
    private readonly RecognitionProfile _profile;

    public RegexRuleViewModel(
        IProfileManager profileManager, string projectRoot, RecognitionProfile profile, string sampleFileName)
    {
        _profileManager = profileManager;
        _projectRoot = projectRoot;
        _profile = profile;
        var existing = profile.Recognition.FirstOrDefault(IsRegex);
        ExistingRuleText = existing is null ? "" : $"Regex {existing.Pattern}";
        _sampleFileName = sampleFileName;
        Pattern = existing?.Pattern ?? "";
    }

    public string Title => $"Regex-Regel für „{_profile.DocumentTypeName}“";

    /// <summary>Vorhandene Regex-Regel („wird ersetzt“), leer = keine.</summary>
    public string ExistingRuleText { get; }

    [ObservableProperty]
    private string _pattern = "";

    [ObservableProperty]
    private string _sampleFileName;

    /// <summary>„Ungültiges Regex-Pattern: …“, leer = Muster gueltig (oder leer).</summary>
    [ObservableProperty]
    private string _validationError = "";

    [ObservableProperty]
    private string _testResult = "";

    [ObservableProperty]
    private bool _testSuccess;

    [ObservableProperty]
    private bool _canSave;

    [ObservableProperty]
    private string _saveError = "";

    partial void OnPatternChanged(string value) => Evaluate();

    partial void OnSampleFileNameChanged(string value) => Evaluate();

    private void Evaluate()
    {
        TestResult = "";
        TestSuccess = false;
        if (string.IsNullOrWhiteSpace(Pattern))
        {
            (ValidationError, CanSave) = ("", false);
            return;
        }
        Regex regex;
        try
        {
            regex = new Regex(Pattern, RegexOptions.IgnoreCase, DocumentTypeRecognizer.RegexTimeout);
        }
        catch (ArgumentException ex)
        {
            (ValidationError, CanSave) = ($"Ungültiges Regex-Pattern: {ex.Message}", false);
            return;
        }
        (ValidationError, CanSave) = ("", true);
        if (string.IsNullOrWhiteSpace(SampleFileName))
            return;
        try
        {
            TestSuccess = regex.IsMatch(DocumentTypeRecognizer.RegexSubject(SampleFileName.Trim()));
            TestResult = TestSuccess
                ? $"✓ Treffer bei „{SampleFileName.Trim()}“"
                : $"Kein Treffer bei „{SampleFileName.Trim()}“";
        }
        catch (RegexMatchTimeoutException)
        {
            TestResult = "Zeitüberschreitung – das Muster ist zu aufwendig.";
        }
    }

    /// <summary>Ersetzt die Regex-Regel des Profils (die Regeln aus dem Wizard bleiben) und speichert.</summary>
    public bool Save()
    {
        SaveError = "";
        if (!CanSave)
            return false;
        var bisher = _profile.Recognition;
        _profile.Recognition = [.. bisher.Where(r => !IsRegex(r)),
            new RecognitionRule { Method = "regex", Pattern = Pattern.Trim() }];
        try
        {
            _profileManager.Save(_projectRoot, _profile);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            _profile.Recognition = bisher; // Anzeige bleibt beim gespeicherten Stand
            SaveError = "Speichern fehlgeschlagen: " + ex.Message;
            Log.Warning("Regex-Regel nicht gespeichert ({Error})", ex.GetType().Name);
            return false;
        }
        Log.Information("Regex-Regel fuer Dokumenttyp {TypeId} gespeichert", _profile.DocumentTypeId);
        return true;
    }

    private static bool IsRegex(RecognitionRule r) => string.Equals(r.Method, "regex", StringComparison.OrdinalIgnoreCase);
}

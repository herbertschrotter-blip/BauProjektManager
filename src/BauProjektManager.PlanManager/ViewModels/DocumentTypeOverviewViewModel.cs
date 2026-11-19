using System.Collections.ObjectModel;
using System.IO;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Tab „Erkennung“ (Seitenliste 3.6 v2.1, BPM-121.00; Zielbild ADR-065 Punkt 7): `document_types` ist das fachliche
/// Hauptobjekt, das Erkennungsprofil haengt 0..1 am Dokumenttyp. Der Tab ist eine Dokumenttyp-Ansicht mit dem Status
/// der Erkennung, KEINE getrennte Profil-Verwaltung. Ring 2 (Bauteil/Geschoss) bleibt draussen — Stammdaten ohne
/// Erkennung (Teil 51). „Inaktiv“/„Lernend“ folgen mit BPM-121.02.
/// </summary>
public partial class DocumentTypeOverviewViewModel : ObservableObject
{
    private readonly ProjectDatabase? _bpmDb;
    private readonly IProfileManager? _profileManager;
    private readonly string _projectId;
    private readonly string? _projectRoot;

    public DocumentTypeOverviewViewModel(
        string projectId, ProjectDatabase? bpmDb, IProfileManager? profileManager = null, string? projectRoot = null)
    {
        _projectId = projectId;
        _bpmDb = bpmDb;
        _profileManager = profileManager;
        _projectRoot = projectRoot;
    }

    /// <summary>Dokumenttypen, gruppiert nach Ablagebereich (root_relative_path).</summary>
    public ObservableCollection<DocumentTypeGroup> Groups { get; } = [];

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _hasTypes;

    [RelayCommand]
    public void Load()
    {
        Groups.Clear();
        if (_bpmDb is null)
        {
            HasTypes = false;
            StatusText = "Keine Projektdatenbank verfügbar.";
            return;
        }

        List<PlanDocumentType> types;
        try
        {
            types = _bpmDb.GetDocumentTypes(_projectId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dokumenttypen nicht ladbar");
            HasTypes = false;
            StatusText = "Dokumenttypen konnten nicht geladen werden.";
            return;
        }

        var profiles = LoadProfiles(types);
        foreach (var group in types
                     .GroupBy(t => t.RootRelativePath.Length > 0 ? t.RootRelativePath : "(kein Ablagebereich)")
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            Groups.Add(new DocumentTypeGroup(
                group.Key,
                [.. group.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(t => new DocumentTypeRow(t, profiles.GetValueOrDefault(t.Id)))]));
        }

        HasTypes = types.Count > 0;
        var learned = profiles.Count;
        var defect = profiles.Values.Count(p => p.Health != ProfileHealth.Valid);
        StatusText = types.Count == 0
            ? "Noch keine Dokumenttypen — sie entstehen beim Projekt-Setup oder über „+ Neu…“ im Radialmenü."
            : $"{types.Count} Dokumenttyp(en) in {Groups.Count} Ablagebereich(en) · {learned} mit Erkennung"
              + (defect > 0 ? $", {defect} defekt" : "");
    }

    /// <summary>Profile je Dokumenttyp (nur Typen dieses Projekts; Profile ohne passenden Typ bleiben aussen vor).</summary>
    private Dictionary<string, RecognitionProfile> LoadProfiles(IReadOnlyList<PlanDocumentType> types)
    {
        if (_profileManager is null || string.IsNullOrWhiteSpace(_projectRoot))
            return [];
        var ids = types.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        try
        {
            return _profileManager.LoadAll(_projectRoot)
                .Where(p => ids.Contains(p.DocumentTypeId))
                .GroupBy(p => p.DocumentTypeId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Log.Warning("Erkennungsprofile nicht lesbar ({Error})", ex.GetType().Name);
            return [];
        }
    }

    /// <summary>
    /// Entfernt nur die Erkennung (Profil). Dokumenttyp, Ablageordner und Dokumente bleiben (asymmetrisch, P.7).
    /// </summary>
    public bool DeleteRecognition(DocumentTypeRow row)
    {
        if (row.Profile is null || _profileManager is null || string.IsNullOrWhiteSpace(_projectRoot))
            return false;
        bool deleted;
        try
        {
            deleted = _profileManager.Delete(_projectRoot, row.Profile.Id);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Erkennung nicht geloescht ({Error})", ex.GetType().Name);
            return false;
        }
        Load();
        return deleted;
    }
}

/// <summary>Ablagebereich mit seinen Dokumenttypen (Gruppen-Kopf der Liste).</summary>
public sealed record DocumentTypeGroup(string RootPath, IReadOnlyList<DocumentTypeRow> Types);

/// <summary>Status der Erkennung eines Dokumenttyps (Mockup 3.6 v2.1).</summary>
public enum RecognitionStatus
{
    NotLearned,
    Active,
    Defect
}

/// <summary>Anzeige-Zeile eines Dokumenttyps mit seiner Erkennung (0..1 Profil).</summary>
public sealed class DocumentTypeRow(PlanDocumentType type, RecognitionProfile? profile = null)
{
    public PlanDocumentType Type { get; } = type;

    public RecognitionProfile? Profile { get; } = profile;

    public string Name => Type.Name;

    /// <summary>Typordner unter dem Ablagebereich; leer = Root-Typ (z. B. Protokolle).</summary>
    public string FolderText => Type.FolderName.Length > 0
        ? Type.FolderName
        : "— direkt im Ablagebereich";

    /// <summary>Anzahl Kategorien, falls der Typ nach Kategorien ablegt.</summary>
    public string CategoryText => Type.Categories.Count == 0
        ? ""
        : $"{Type.Categories.Count} Kategorie(n)";

    public bool IsBuiltin => Type.IsBuiltin;

    public bool HasProfile => Profile is not null;

    public RecognitionStatus Status => Profile is null ? RecognitionStatus.NotLearned
        : Profile.Health == ProfileHealth.Valid ? RecognitionStatus.Active
        : RecognitionStatus.Defect;

    public string StatusText => Status switch
    {
        RecognitionStatus.Active => "● Aktiv",
        RecognitionStatus.Defect => Profile!.MissingSegmentTypeIds.Count > 0
            ? $"● Defekt – Segmenttyp {string.Join(", ", Profile.MissingSegmentTypeIds.Select(id => $"„{id}“"))} fehlt"
            : "● Defekt",
        _ => "○ Nicht angelernt"
    };

    /// <summary>Woran die Erkennung den Typ erkennt, z. B. „Erkennung: enthält „Polierplan“ · Regex ^B-\d+“.</summary>
    public string RuleText => Profile is null || Profile.Recognition.Count == 0
        ? ""
        : "Erkennung: " + string.Join(" · ", Profile.Recognition.Select(Describe));

    private static string Describe(RecognitionRule rule)
        => string.Equals(rule.Method, "regex", StringComparison.OrdinalIgnoreCase)
            ? $"Regex {rule.Pattern}"
            : rule.SegmentPosition is { } pos
                ? $"„{rule.Pattern}“ an Stelle {pos + 1}"
                : $"enthält „{rule.Pattern}“";
}

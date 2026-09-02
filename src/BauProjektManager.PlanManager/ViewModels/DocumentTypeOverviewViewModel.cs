using System.Collections.ObjectModel;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Dokumenttyp-Übersicht des Profile-Tabs (BPM-006, Zielbild ADR-065 Punkt 7):
/// `document_types` ist das fachliche Hauptobjekt — der Tab ist eine reine
/// DocumentType-View, KEINE getrennte Profil-Verwaltung.
///
/// Bewusst NICHT enthalten: Ring 2 (Bauteil/Geschoss) — das sind Stammdaten,
/// die keine Erkennung brauchen (Klarstellung Herbert, Teil 51). Die Spalte
/// „Erkennung: nicht angelernt / lernend / aktiv" folgt mit BPM-121 Stufe B.
/// </summary>
public partial class DocumentTypeOverviewViewModel : ObservableObject
{
    private readonly ProjectDatabase? _bpmDb;
    private readonly string _projectId;

    public DocumentTypeOverviewViewModel(string projectId, ProjectDatabase? bpmDb)
    {
        _projectId = projectId;
        _bpmDb = bpmDb;
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

        foreach (var group in types
                     .GroupBy(t => t.RootRelativePath.Length > 0 ? t.RootRelativePath : "(kein Ablagebereich)")
                     .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            Groups.Add(new DocumentTypeGroup(
                group.Key,
                [.. group.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(t => new DocumentTypeRow(t))]));
        }

        HasTypes = types.Count > 0;
        StatusText = types.Count == 0
            ? "Noch keine Dokumenttypen — sie entstehen beim Projekt-Setup oder über „+ Neu…“ im Radialmenü."
            : $"{types.Count} Dokumenttyp(en) in {Groups.Count} Ablagebereich(en)";
    }
}

/// <summary>Ablagebereich mit seinen Dokumenttypen (Gruppen-Kopf der Liste).</summary>
public sealed record DocumentTypeGroup(string RootPath, IReadOnlyList<DocumentTypeRow> Types);

/// <summary>Anzeige-Zeile eines Dokumenttyps.</summary>
public sealed class DocumentTypeRow(PlanDocumentType type)
{
    public PlanDocumentType Type { get; } = type;

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

    /// <summary>
    /// Erkennungs-Status — Platzhalter bis BPM-121 Stufe B (Mining). Bis dahin
    /// gibt es projektweit noch keine gelernten Muster.
    /// </summary>
    public string RecognitionText => "nicht angelernt";
}

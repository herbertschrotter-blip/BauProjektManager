using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Was "Mit Claude sortieren" vom Tab "Manuell sortieren" braucht (Test-Naht, CODING_STANDARDS 8.1):
/// Projekt, aktuelle Eingangs-Analyse, Pending-Store und die Kennzeichnung der Zeilen.
/// Implementiert von <see cref="ManualCaptureViewModel"/>.
/// </summary>
public interface IClaudeSortHost
{
    string ProjectId { get; }

    string ProjectRootPath { get; }

    /// <summary>Eingangsdateien der letzten Analyse (alle Buckets).</summary>
    IReadOnlyList<CaptureItem> CurrentItems { get; }

    /// <summary>Pending-Zuordnungen (Uebernahme setzt dort die Vorschlaege).</summary>
    PendingAssignmentStore PendingStore { get; }

    /// <summary>Eingang neu analysieren und die Tabelle neu aufbauen.</summary>
    Task ReanalyzeAsync();

    /// <summary>Bauteile des Projekts (Auswahl in der Karte „Fehlt im Projekt“).</summary>
    IReadOnlyList<BuildingPart> BuildingParts { get; }

    /// <summary>Stammdaten nach einer Schnellanlage aus dem Chat neu laden (Radial sieht die Neuanlage).</summary>
    void StammdatenNeuLaden();

    /// <summary>Zeilen kennzeichnen (Schluessel = relativer Pfad); leere Liste nimmt alle Kennzeichen weg.</summary>
    void SetClaudeMarks(IReadOnlyDictionary<string, ClaudeRowMark> marks);
}

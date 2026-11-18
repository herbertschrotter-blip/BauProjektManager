using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Eingabe fuer den Auftrags-Builder. <paramref name="AuftragsOrdner"/> ist der
/// absolute Zielordner (z. B. &lt;Projekt-LocalAppData&gt;/claude/A-0001) — der Ort
/// wird vom Aufrufer bestimmt, der Builder legt ihn an.
/// </summary>
/// <param name="ProjectId">Projekt fuer die Stammdaten (bpm.db).</param>
/// <param name="ProjectRootPath">Projektroot (absolut) — nur zum Lesen der Eingangsdateien.</param>
/// <param name="AuftragsOrdner">Absoluter Auftragsordner; Name (letztes Segment) = Auftragsname.</param>
/// <param name="Analyse">Aktuelle Capture-Analyse; nur Bucket C wird uebergeben.</param>
/// <param name="PlankopfTextMitsenden">
/// Freigabe aus dem Bestaetigungsdialog (DSGVO Klasse C, ADR-067): nur bei true wird der Plankopf-Text
/// gelesen und in kontext.json geschrieben. Bewusst ohne Standardwert — jeder Aufrufer entscheidet.
/// </param>
public sealed record ClaudeSortAuftragAnfrage(
    string ProjectId,
    string ProjectRootPath,
    string AuftragsOrdner,
    IReadOnlyList<CaptureItem> Analyse,
    bool PlankopfTextMitsenden);

/// <summary>
/// Ein angelegter Auftrag. <see cref="Dateien"/> ist die einzige Wahrheit ueber
/// die Zuordnung datei_id → Eingangsdatei (lebt in-memory beim Aufrufer; Claude
/// sieht nur die IDs und kann die Zuordnung nicht veraendern).
/// </summary>
/// <param name="RegelnDatei">
/// Rahmen + Vorschrift als Datei NEBEN dem Auftragsordner (--append-system-prompt-file): Claude kann seine
/// Regeln dort nicht umschreiben.
/// </param>
public sealed record ClaudeSortAuftrag(
    string Name,
    string Ordner,
    string RegelnDatei,
    string ProjectId,
    IReadOnlyDictionary<string, CaptureItem> Dateien,
    IReadOnlyList<KnownPlanDocument> BekanntePlaene);

/// <summary>Eine Datei, die Claude bewusst nicht zuordnen konnte.</summary>
/// <param name="Fehlt">Vorschlag von Claude, was im Projekt fehlt (ADR-067 E11); NULL = nichts vorgeschlagen.</param>
public sealed record ClaudeSortNichtZuordenbar(string DateiId, CaptureItem Item, string Grund, ClaudeSortFehlt? Fehlt = null);

/// <summary>
/// Eine gepruefte, gueltige Entwurfszeile: alle IDs sind in den Stammdaten
/// aufgeloest, der Ablageordner kommt aus dem DocumentTargetPathResolver.
/// </summary>
public sealed record ClaudeSortZeile(
    string DateiId,
    CaptureItem Item,
    PlanDocumentType Type,
    BuildingPart? Part,
    BuildingLevel? Level,
    PlanDocumentTypeCategory? Category,
    string? PlanNumber,
    string? Index,
    string? Title,
    IReadOnlyList<string> Unsicher,
    string? Hinweise,
    string Begruendung,
    string TargetRelativeDirectory,
    string? IndexDatum = null,
    IReadOnlyList<AssignedSegmentValue>? Segmente = null,
    IReadOnlyList<string>? Tags = null,
    string? Details = null);

/// <summary>
/// Ergebnis der Entwurf-Pruefung. Fehler sind deutsch und gehen als
/// "Entwurf ungueltig: …" an Claude zurueck (Runden-Limit regelt der Aufrufer);
/// gueltige Zeilen duerfen trotzdem uebernommen werden.
/// </summary>
public sealed record ClaudeSortPruefErgebnis(
    IReadOnlyList<ClaudeSortZeile> GueltigeZeilen,
    IReadOnlyList<ClaudeSortNichtZuordenbar> NichtZuordenbar,
    IReadOnlyList<string> Fehler,
    IReadOnlyList<string> Hinweise)
{
    /// <summary>True, wenn der Entwurf ohne Fehler ist.</summary>
    public bool IstGueltig => Fehler.Count == 0;

    /// <summary>Rueckmeldung an Claude; leer, wenn der Entwurf gueltig ist.</summary>
    public string FehlerText => IstGueltig
        ? string.Empty
        : "Entwurf ungültig:\n" + string.Join("\n", Fehler.Select(f => "- " + f));

    internal static ClaudeSortPruefErgebnis NurFehler(string fehler)
        => new([], [], [fehler], []);
}

/// <summary>Ergebnis der Uebernahme in den Pending-Store.</summary>
/// <param name="Uebernommen">Als Vorschlag gesetzte Zuordnungen.</param>
/// <param name="Uebersprungen">Dateien, die nicht uebernommen wurden, mit Grund.</param>
public sealed record ClaudeSortUebernahmeErgebnis(
    IReadOnlyList<PendingAssignment> Uebernommen,
    IReadOnlyList<(string FileName, string Grund)> Uebersprungen);

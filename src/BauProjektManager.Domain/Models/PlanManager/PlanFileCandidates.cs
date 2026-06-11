using BauProjektManager.Domain.Enums.PlanManager;

namespace BauProjektManager.Domain.Models.PlanManager;

/// <summary>
/// Kandidaten aus dem Lightweight-Extractor (BPM-111.02, ADR-059).
/// NUR Vorschlaege ("B entscheidet, A schlaegt vor") — werden nie automatisch
/// in plan_documents geschrieben, sondern fuellen Kandidaten-Spalte, Radial-
/// Vorbelegung und das Matching der ManualFirstCapture-Buckets (BPM-111.03).
/// </summary>
/// <param name="FileName">Urspruenglicher Dateiname (mit Extension).</param>
/// <param name="PlanNumber">Plannummern-Kandidat (z. B. "5998-203", "S-103"), NULL wenn keiner erkannt.</param>
/// <param name="Index">Index-Kandidat (z. B. "B", "vorab"), NULL bei Erstausgabe/unerkannt.</param>
/// <param name="RevisionKind">Klassifikation des Index-Kandidaten (Numeric/Alphabetic/DraftMarker/...).</param>
/// <param name="Level">Geschoss-Kandidat aus strikter Tokenliste (z. B. "OG3", "KG"), NULL wenn keiner.</param>
/// <param name="BuildingPartHint">Roh-Token mit Bauteil-Verdacht (z. B. "H2", "Haus 1", "TG") fuer das Stammdaten-Matching in 111.03 — KEIN aufgeloester Bauteil-Name.</param>
/// <param name="TypeKeywords">Erkannte Plantyp-/Protokoll-Schluesselwoerter in kanonischer Form (z. B. ["Polierplan"], ["Schalung","Bewehrung"]).</param>
/// <param name="DateCandidate">Datums-Token im Dateinamen (yyyy-MM-dd), z. B. bei Protokollen. NULL wenn keines.</param>
/// <param name="HasCopyMarker">True wenn ein Windows-Kopiermarker wie "(1)" am Ende erkannt und entfernt wurde.</param>
/// <param name="IsCombi">True wenn mehrere Plan-Typ-Keywords erkannt wurden (Kombi-Datei, z. B. "Schalung+Bewehrung") — V1: kein Auto-Split, Hinweis im Panel.</param>
public sealed record PlanFileCandidates(
    string FileName,
    string? PlanNumber,
    string? Index,
    RevisionKind RevisionKind,
    string? Level,
    string? BuildingPartHint,
    IReadOnlyList<string> TypeKeywords,
    string? DateCandidate,
    bool HasCopyMarker,
    bool IsCombi);

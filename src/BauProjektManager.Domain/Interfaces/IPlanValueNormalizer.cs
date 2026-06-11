namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Zentrale Normalisierung von Plan-Werten (BPM-111.02, ADR-059).
/// EINE Stelle fuer alle Normalisierungs-Kontexte — Schreiber und Leser
/// muessen identisch normalisieren (Lehre aus BPM-110 Feldkey-Bruch).
/// </summary>
public interface IPlanValueNormalizer
{
    /// <summary>
    /// Normalisierung fuer document_key-Bestandteile und Schluessel-Vergleiche:
    /// lowercase, Umlaute transliteriert, Sonderzeichen zu Unterstrich.
    /// Beispiel: "TG Wände" -> "tg_waende".
    /// </summary>
    string NormalizeForKey(string value);

    /// <summary>
    /// Vergleichsform fuer Kandidaten-Matching (Whitespace/Case/Trenner-tolerant):
    /// lowercase, Umlaute transliteriert, Leerzeichen/Trenner entfernt.
    /// Beispiel: "Haus 2", "haus-2" und "HAUS_2" -> "haus2".
    /// KEIN Alias-Mapping (z. B. "H2" -> "Haus 2") — das ist BPM-109.06 (post-V1).
    /// </summary>
    string NormalizeForMatch(string value);

    /// <summary>
    /// Gueltiger Windows-Ordnername: ungueltige Zeichen entfernt, Whitespace
    /// kollabiert, fuehrende/abschliessende Punkte und Leerzeichen entfernt,
    /// Laenge begrenzt. Gross-/Kleinschreibung und Umlaute bleiben erhalten
    /// (Anzeigename). Beispiel: "Haus 2: West?" -> "Haus 2 West".
    /// </summary>
    string NormalizeForFolderName(string value);
}

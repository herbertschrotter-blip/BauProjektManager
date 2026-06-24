namespace BauProjektManager.Domain.Models;

/// <summary>
/// Unveraenderlicher Schnappschuss der Metadaten einer Datei (ADR-060, BPM-112).
/// Ersetzt System.IO.FileInfo an den Port-Grenzen, damit Domain/Module nicht von
/// System.IO abhaengen. Zeiten in UTC (CODING_STANDARDS Kap. 19).
/// </summary>
/// <param name="FullPath">Absoluter Pfad der Datei.</param>
/// <param name="Exists">Ob die Datei zum Abfragezeitpunkt existierte.</param>
/// <param name="Length">Groesse in Bytes (0 wenn nicht vorhanden).</param>
/// <param name="LastWriteTimeUtc">Letzte Aenderung in UTC (default wenn nicht vorhanden).</param>
public sealed record FileInfoSnapshot(
    string FullPath,
    bool Exists,
    long Length,
    DateTime LastWriteTimeUtc);

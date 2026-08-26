namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Shell-Launcher-Port (ADR-060 Punkt 3): Dateien/Ordner mit der
/// Windows-Standard-App bzw. im Explorer öffnen — bewusst GETRENNT von den
/// Dateisystem-Ports (Reader/Writer/Path), weil Shell-Interaktion keine
/// Dateioperation ist. Einzige Implementierung: LocalFileLauncher
/// (Infrastructure, ShellExecute).
///
/// CopyPathToClipboard aus ADR-060 folgt erst mit dem In-App-Explorer
/// (Clipboard ist UI-nah) — Port bewusst klein halten.
/// </summary>
public interface IFileLauncher
{
    /// <summary>Öffnet die Datei mit der Windows-Standard-App (ShellExecute). False bei Fehler.</summary>
    bool OpenFile(string absolutePath);

    /// <summary>Öffnet den Ordner im Explorer. False bei Fehler.</summary>
    bool OpenFolder(string absoluteDirectory);

    /// <summary>Öffnet den Explorer mit vorselektierter Datei. False bei Fehler.</summary>
    bool RevealInExplorer(string absolutePath);
}

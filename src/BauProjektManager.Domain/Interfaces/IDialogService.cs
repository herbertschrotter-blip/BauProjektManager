namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Abstraktion für Benutzer-Dialoge (Info, Warnung, Fehler, Bestätigung).
/// Implementierung im App-Projekt mit BPM Dark Theme Design.
/// Wird über Constructor Injection in ViewModels verwendet.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Zeigt einen Info-Hinweis an.
    /// </summary>
    void ShowInfo(string message, string title = "Hinweis");

    /// <summary>
    /// Zeigt eine Warnung an.
    /// </summary>
    void ShowWarning(string message, string title = "Warnung");

    /// <summary>
    /// Zeigt eine Fehlermeldung an.
    /// </summary>
    void ShowError(string message, string title = "Fehler");

    /// <summary>
    /// Zeigt einen Ja/Nein-Dialog an. Gibt true zurück wenn bestätigt.
    /// </summary>
    bool ShowConfirm(string message, string title = "Bestätigung");
}
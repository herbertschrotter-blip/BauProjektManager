using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.App;

/// <summary>
/// Implementation von IDialogService mit BPM Dark Theme Dialogen.
/// Nutzt BpmInfoDialog und BpmConfirmDialog aus dem App-Projekt.
/// </summary>
public class BpmDialogService : IDialogService
{
    public void ShowInfo(string message, string title = "Hinweis")
    {
        BpmInfoDialog.ShowInfo(message, title);
    }

    public void ShowWarning(string message, string title = "Warnung")
    {
        BpmInfoDialog.ShowWarning(message, title);
    }

    public void ShowError(string message, string title = "Fehler")
    {
        BpmInfoDialog.ShowError(message, title);
    }

    public bool ShowConfirm(string message, string title = "Bestätigung")
    {
        return BpmInfoDialog.ShowConfirm(message, title);
    }
}
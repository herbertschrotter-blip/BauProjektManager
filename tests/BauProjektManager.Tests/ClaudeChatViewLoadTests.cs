using System.Runtime.ExceptionServices;
using BauProjektManager.PlanManager.Views;

namespace BauProjektManager.Tests;

/// <summary>
/// Rauchtest fuer die XAML von "Mit Claude sortieren" (Seitenliste 3.4): ManualCaptureView und
/// ClaudeChatPanel muessen sich ohne Parse-/Ressourcenfehler erzeugen lassen (WPF braucht einen STA-Thread).
/// </summary>
public class ClaudeChatViewLoadTests
{
    private static void OnSta(Action action)
    {
        ExceptionDispatchInfo? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        error?.Throw();
    }

    [Fact]
    public void ClaudeChatPanel_LoadsXaml() => OnSta(() => _ = new ClaudeChatPanel());

    [Fact]
    public void ManualCaptureView_WithClaudeParts_LoadsXaml() => OnSta(() =>
    {
        var view = new ManualCaptureView();
        Assert.Null(view.ClaudeSort); // ohne Claude-Port bleibt alles unsichtbar
    });

    /// <summary>Profil-Wizard 3.8 (BPM-130): XAML mit Segment-Editor und 4 Schritten laesst sich erzeugen.</summary>
    [Fact]
    public void ProfileWizardDialog_LoadsXaml() => OnSta(() =>
    {
        var dialog = new ProfileWizardDialog();
        Assert.Equal(4, ((BauProjektManager.PlanManager.ViewModels.ProfileWizardViewModel)dialog.DataContext).TotalSteps);
        dialog.Close();
    });
}

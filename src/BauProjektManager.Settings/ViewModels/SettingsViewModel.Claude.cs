using BauProjektManager.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace BauProjektManager.Settings.ViewModels;

/// <summary>Auswahl „Modell“ fuer „Mit Claude sortieren“ (Seitenliste 2.1 Mockup 0.3).</summary>
/// <param name="Value">Modell-Alias fuer Claude Code; NULL = Standard von Claude Code (kein --model).</param>
public sealed record ClaudeModelOption(string? Value, string Text)
{
    public override string ToString() => Text;
}

/// <summary>Einstellungen › Allgemein › Claude: Modellwahl (BPM-131 E9, geraete-lokal).</summary>
public partial class SettingsViewModel
{
    public static IReadOnlyList<ClaudeModelOption> ClaudeModelOptions { get; } =
    [
        new(null, "Standard von Claude Code"),
        new(ClaudeSettings.ModelSonnet, "Sonnet – schneller, sparsamer"),
        new(ClaudeSettings.ModelOpus, "Opus – gründlicher, langsamer")
    ];

    [ObservableProperty]
    private ClaudeModelOption _claudeModel = ClaudeModelOptions[0];

    partial void OnClaudeModelChanged(ClaudeModelOption value)
    {
        if (_loadingSettings)
            return;
        var device = _settingsService.LoadDevice();
        device.Claude.Model = value.Value;
        _settingsService.SaveDevice(device);
        Log.Information("Setting changed: {Key} = {Value}", "Claude.Model", value.Value ?? "standard");
    }

    /// <summary>Gespeichertes Modell als Auswahl; Unbekanntes faellt auf „Standard“ zurueck.</summary>
    private static ClaudeModelOption ClaudeModelFor(string? value)
        => ClaudeModelOptions.FirstOrDefault(o => o.Value == ClaudeSettings.NormalizeModel(value)) ?? ClaudeModelOptions[0];
}

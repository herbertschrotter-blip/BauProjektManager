using System.IO;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Schnellanlage fehlender Stammdaten aus dem Chat (Mockup 3.4 v1.3, ADR-067 E11): Karte je Vorschlag, „Anlegen“
/// legt ueber die normalen Stammdaten-Wege an, schreibt kontext.json neu und meldet die neuen IDs an Claude.
/// </summary>
public sealed partial class ClaudeSortViewModel
{
    internal const string FehltMarke = "＋ fehlt im Projekt – siehe Chat";

    /// <summary>Je neuem Vorschlag eine Karte; ein Vorschlag, der schon als Karte offen ist, kommt nicht doppelt.</summary>
    private void ZeigeFehlendes(ClaudeSortPruefErgebnis ergebnis)
    {
        var offen = Entries.Where(e => e.Fehlt is { IsDone: false }).Select(e => e.Fehlt!.Vorschlag.Schluessel)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var gruppe in ergebnis.NichtZuordenbar.Where(n => n.Fehlt is not null)
                     .GroupBy(n => n.Fehlt!.Schluessel, StringComparer.Ordinal))
        {
            if (offen.Contains(gruppe.Key))
                continue;
            var karte = new ClaudeSortFehltViewModel(gruppe.First().Fehlt!,
                [.. gruppe.Select(n => n.DateiId)], [.. gruppe.Select(n => n.Item.File.Scan.FileName)], _host.BuildingParts);
            Entries.Add(new ClaudeChatEntry(ClaudeChatEntryKind.Missing, karte.Titel, fehlt: karte));
        }
    }

    [RelayCommand]
    private async Task CreateMissingAsync(ClaudeSortFehltViewModel? karte)
    {
        if (karte is null || karte.IsDone || _auftrag is null || _schnellanlage is null)
            return;
        if (Phase == ClaudeSortPhase.Running)
        {
            // Sonst waere angelegt, aber die Meldung an Claude wuerde abgewiesen
            karte.ErgebnisText = "Claude arbeitet noch – bitte die Antwort abwarten.";
            return;
        }
        ClaudeSortAnlageErgebnis ergebnis;
        try
        {
            ergebnis = _schnellanlage.Anlegen(_auftrag.ProjectId, karte.ZuAnlage());
            if (ergebnis.Ok)
            {
                _host.StammdatenNeuLaden();
                _builder.KontextAktualisieren(_auftrag);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException
                                       or System.Text.Json.JsonException or System.Data.Common.DbException)
        {
            Log.Warning("Claude-Sortierung: Schnellanlage fehlgeschlagen ({Error})", ex.GetType().Name);
            karte.IsErgebnisOk = false;
            karte.ErgebnisText = "Anlegen fehlgeschlagen – bitte in den Projekteinstellungen prüfen.";
            return;
        }
        if (!ergebnis.Ok)
        {
            karte.IsErgebnisOk = false;
            karte.ErgebnisText = ergebnis.Anzeige;
            return;
        }
        karte.IsDone = true;
        karte.IsErgebnisOk = true;
        karte.ErgebnisText = "✓ angelegt";
        Add(ClaudeChatEntryKind.SystemOk, $"✓ {ergebnis.Anzeige} · Kontext aktualisiert · an Claude gemeldet");
        Log.Information("Claude-Sortierung: Schnellanlage aus dem Chat ({Art}) fuer Auftrag {Name}",
            karte.Vorschlag.Art, _auftrag.Name);
        await SendeAnClaudeAsync(
            $"Herbert hat angelegt: {ergebnis.FuerClaude} kontext.json ist aktualisiert. Lies kontext.json neu, " +
            $"ordne {string.Join(", ", karte.DateiIds)} neu zu und schreib entwurf.json neu.");
    }

    [RelayCommand]
    private void SkipMissing(ClaudeSortFehltViewModel? karte)
    {
        if (karte is null || karte.IsDone)
            return;
        karte.IsDone = true;
        karte.IsErgebnisOk = false;
        karte.ErgebnisText = "nicht angelegt";
        Add(ClaudeChatEntryKind.System,
            $"Nicht angelegt – {string.Join(", ", karte.DateiIds)} bleibt ohne Vorschlag (von Hand zuordnen).");
    }
}

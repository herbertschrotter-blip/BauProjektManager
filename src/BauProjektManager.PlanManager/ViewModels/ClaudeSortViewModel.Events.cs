using BauProjektManager.Domain.Enums;
using BauProjektManager.Domain.Models.Claude;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>Ereignisse aus der Claude-Sitzung und Pruefung des Entwurfs nach jeder Runde.</summary>
public sealed partial class ClaudeSortViewModel
{
    /// <summary>Empfaenger der Sitzungs-Ereignisse (UI-Thread, Progress&lt;T&gt;).</summary>
    internal void OnEvent(ClaudeEvent e)
    {
        switch (e.Kind)
        {
            case ClaudeEventKind.Text:
                ZeigeText(e);
                break;
            case ClaudeEventKind.ToolUse:
                Add(ClaudeChatEntryKind.Activity, $"↳ {Verb(e.ToolName)} {e.ToolTarget}".TrimEnd());
                break;
            case ClaudeEventKind.TurnCompleted when e.IsError:
                EntferneDoppelteFehlermeldung(e.Text);
                if (IstAnmeldefehler(e.Text))
                {
                    _ = AnmeldungFehltAsync();
                    break;
                }
                Add(ClaudeChatEntryKind.SystemError, e.Text ?? "Claude hat die Runde mit einem Fehler beendet.");
                Phase = ClaudeSortPhase.Waiting;
                break;
            case ClaudeEventKind.TurnCompleted:
                _ = NachRundeSicherAsync();
                break;
            case ClaudeEventKind.Error:
                Add(ClaudeChatEntryKind.SystemError, e.Text ?? "Fehler in der Sitzung mit Claude.");
                if (Phase == ClaudeSortPhase.Running)
                    Phase = ClaudeSortPhase.Waiting;
                break;
        }
    }

    /// <summary>
    /// Claude Code meldet einen Fehler der Runde zusaetzlich als Text-Nachricht — die Kopie als "Claude"-Eintrag
    /// entfernen, damit die Meldung nur einmal (als Fehler) im Chat steht.
    /// </summary>
    private void EntferneDoppelteFehlermeldung(string? text)
    {
        if (text is not null && Entries.Count > 0 && Entries[^1] is { Kind: ClaudeChatEntryKind.Claude } letzte
            && string.Equals(letzte.Text.Trim(), text.Trim(), StringComparison.Ordinal))
            Entries.RemoveAt(Entries.Count - 1);
    }

    /// <summary>Anmeldung von Claude Code fehlt oder ist abgelaufen ("Failed to authenticate", "/login").</summary>
    internal static bool IstAnmeldefehler(string? text) =>
        text is not null
        && (text.Contains("authenticate", StringComparison.OrdinalIgnoreCase)
            || text.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || text.Contains("OAuth", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Ohne gueltige Anmeldung kann Claude nichts tun: Sitzung beenden, Auftragsdateien loeschen, Kennzeichen
    /// weg und Fehlerkarte mit Abhilfe zeigen ("Erneut pruefen" startet einen neuen Auftrag).
    /// </summary>
    private async Task AnmeldungFehltAsync()
    {
        Log.Warning("Claude-Sortierung: Claude Code ist nicht angemeldet");
        await BeendeSitzungAsync();
        RaeumeAuftragAuf();
        _auftrag = null;
        _host.SetClaudeMarks(new Dictionary<string, ClaudeRowMark>());
        ZeigeNichtErreichbar("Claude Code ist auf diesem PC nicht angemeldet oder die Anmeldung ist abgelaufen. " +
                             "Es wurde nichts an Claude geschickt.");
    }

    private void ZeigeText(ClaudeEvent e)
    {
        var fragen = e.Questions ?? [];
        if (fragen.Count == 0)
        {
            Add(ClaudeChatEntryKind.Claude, e.Text ?? "");
            return;
        }
        if (!string.IsNullOrWhiteSpace(fragen[0].LeadText))
            Add(ClaudeChatEntryKind.Claude, fragen[0].LeadText!);
        foreach (var frage in fragen)
            Add(ClaudeChatEntryKind.Question, frage.Title, new ClaudeQuestionViewModel(frage));
    }

    /// <summary>Fehler der Pruefung nach einer Runde sichtbar machen statt sie zu verlieren.</summary>
    private async Task NachRundeSicherAsync()
    {
        try
        {
            await NachRundeAsync();
        }
        catch (Exception ex)
        {
            Log.Warning("Claude-Sortierung: Pruefung nach der Runde fehlgeschlagen ({Error})", ex.GetType().Name);
            Add(ClaudeChatEntryKind.SystemError, "Der Entwurf ließ sich nicht prüfen. Schreib Claude, dann versucht es BPM erneut.");
            Phase = ClaudeSortPhase.Waiting;
        }
    }

    /// <summary>
    /// Nach jeder Runde: Liegt ein Entwurf vor, wird er geprueft. Ungueltig → zurueck an Claude (hoechstens
    /// <see cref="MaxRetries"/>-mal je Nachricht), danach gilt, was gueltig ist. Ohne Entwurf wartet Claude auf Herbert.
    /// </summary>
    internal async Task NachRundeAsync()
    {
        if (_auftrag is null || _session is null)
            return;
        if (!_pruefer.EntwurfVorhanden(_auftrag))
        {
            Phase = ClaudeSortPhase.Waiting;
            return;
        }

        await _host.ReanalyzeAsync();
        var ergebnis = _pruefer.PruefeOrdner(_auftrag, _host.CurrentItems);
        if (!ergebnis.IstGueltig && _retries < MaxRetries)
        {
            _retries++;
            Add(ClaudeChatEntryKind.SystemError,
                $"Entwurf geprüft: ungültig – {ergebnis.Fehler.Count} Fehler. An Claude zurück (Versuch {_retries}/{MaxRetries}).");
            Phase = ClaudeSortPhase.Running;
            try
            {
                await _session.SendMessageAsync(ergebnis.FehlerText);
            }
            catch (ClaudeAssistantException ex)
            {
                Add(ClaudeChatEntryKind.SystemError, ex.Message);
                Phase = ClaudeSortPhase.Waiting;
            }
            return;
        }

        ZeigeEntwurf(ergebnis);
    }

    private void ZeigeEntwurf(ClaudeSortPruefErgebnis ergebnis)
    {
        _pruefung = ergebnis;
        _offen = [.. ergebnis.GueltigeZeilen];
        var gueltig = ergebnis.GueltigeZeilen;
        var unsicher = gueltig.Count(z => z.Unsicher.Count > 0);
        var ohne = _auftrag!.Dateien.Count - gueltig.Count;
        var marks = new Dictionary<string, ClaudeRowMark>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in _auftrag.Dateien.Values)
            marks[item.File.Scan.RelativePath] = new ClaudeRowMark(ClaudeRowMarkKind.Invalid, "✕ kein gültiger Vorschlag");
        foreach (var n in ergebnis.NichtZuordenbar.Where(n => n.Fehlt is not null))
            marks[n.Item.File.Scan.RelativePath] = new ClaudeRowMark(ClaudeRowMarkKind.Missing, FehltMarke);
        foreach (var z in gueltig)
            marks[z.Item.File.Scan.RelativePath] = Kennzeichen(z);
        _marks = marks;
        _host.SetClaudeMarks(new Dictionary<string, ClaudeRowMark>(marks, StringComparer.OrdinalIgnoreCase));

        AktualisiereEntwurfText();
        var text = ergebnis.IstGueltig
            ? $"Entwurf geprüft ✓ {DraftSummary}"
            : $"Entwurf geprüft: {gueltig.Count} Vorschläge gültig, {ergebnis.Fehler.Count} Fehler bleiben offen – " +
              "diese Dateien bitte von Hand zuordnen.";
        Add(ergebnis.IstGueltig ? ClaudeChatEntryKind.SystemOk : ClaudeChatEntryKind.SystemError, text);
        foreach (var hinweis in ergebnis.Hinweise)
            Add(ClaudeChatEntryKind.System, hinweis);
        ZeigeFehlendes(ergebnis);
        Phase = gueltig.Count > 0 ? ClaudeSortPhase.Draft : ClaudeSortPhase.Waiting;
        Log.Information("Claude-Sortierung: Entwurf {Name} angezeigt ({Valid} gueltig, {Uncertain} unsicher)",
            _auftrag.Name, gueltig.Count, unsicher);
    }

    /// <summary>Leiste und Statuszeile aus den noch offenen Vorschlaegen.</summary>
    private void AktualisiereEntwurfText()
    {
        var unsicher = _offen.Count(z => z.Unsicher.Count > 0);
        var ohne = _marks.Values.Count(m => m.Kind == ClaudeRowMarkKind.Invalid);
        var fehlt = _marks.Values.Count(m => m.Kind == ClaudeRowMarkKind.Missing);
        DraftSummary = $"{_offen.Count} Vorschläge" + (unsicher > 0 ? $" · {unsicher} unsicher" : "") +
                       (fehlt > 0 ? $" · {fehlt} fehlt im Projekt" : "") +
                       (ohne > 0 ? $" · {ohne} ohne gültigen Vorschlag" : "");
        DraftButtonText = $"{_offen.Count} Vorschläge übernehmen";
        StatusDetail = DraftSummary;
    }

    private void MarkiereBeiClaude()
    {
        var marks = _auftrag!.Dateien.Values.ToDictionary(
            i => i.File.Scan.RelativePath,
            _ => new ClaudeRowMark(ClaudeRowMarkKind.AtClaude, "◌ bei Claude"),
            StringComparer.OrdinalIgnoreCase);
        _host.SetClaudeMarks(marks);
    }

    /// <summary>"✦ Typ › Bauteil › Geschoss", unsichere Felder mit "?".</summary>
    internal static ClaudeRowMark Kennzeichen(ClaudeSortZeile z)
    {
        string Teil(string? wert, string feld) =>
            wert is null ? "" : z.Unsicher.Contains(feld) ? $"{wert} ?" : wert;
        var teile = new[]
        {
            Teil(z.Type.Name, "dokumenttyp_id"),
            Teil(z.Part is not null ? ClaudeSortUebernahme.PartName(z.Part) : z.Category?.Name,
                z.Part is not null ? "bauteil_id" : "kategorie_id"),
            Teil(z.Level?.Name, "geschoss_id")
        }.Where(t => t.Length > 0);
        var info = new ClaudeSuggestionInfo(z.Begruendung, z.Unsicher, z.Hinweise);
        var kind = z.Unsicher.Count > 0 ? ClaudeRowMarkKind.DraftUncertain : ClaudeRowMarkKind.Draft;
        return new ClaudeRowMark(kind, "✦ " + string.Join(" › ", teile), info);
    }

    private static string Verb(string? tool) => tool switch
    {
        "Read" => "liest",
        "Write" => "schreibt",
        "Edit" => "ändert",
        "Glob" => "sucht",
        _ => tool ?? ""
    };
}

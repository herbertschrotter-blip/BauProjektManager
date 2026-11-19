using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Bauteil-Aliasse in „Manuell sortieren“ (Seitenliste 3.3 v1.2, BPM-121.03): Hinweis aus dem Dateinamen im
/// Detailbereich, nach dem Zuordnen der Vorschlag „„H2“ → Haus 2 – als Alias merken?“. Ein gemerkter Alias gilt im
/// ganzen Projekt und markiert das Bauteil im Radial als Kandidat. Nur nach Bestaetigung, kein Fuzzy (ADR-065 P.14).
/// </summary>
public partial class ManualCaptureViewModel
{
    private readonly IPlanValueNormalizer _aliasNormalizer = new PlanValueNormalizer();
    private BuildingPartHintResolver? _hints;

    /// <summary>„Nein“ zu Hinweis+Bauteil — der Vorschlag kommt in dieser Sitzung nicht wieder.</summary>
    private readonly HashSet<string> _declinedAliases = new(StringComparer.Ordinal);

    /// <summary>Aufloesung der Bauteil-Hinweise (fuer den Radial-Controller nach einer Schnellanlage).</summary>
    public BuildingPartHintResolver Hints => _hints ?? BuildingPartHintResolver.Empty(_aliasNormalizer);

    private void ReloadHints()
    {
        IReadOnlyDictionary<string, string> aliases;
        try
        {
            aliases = _bpmDb.GetBuildingPartAliases(_projectId);
        }
        catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or InvalidOperationException)
        {
            Log.Warning("Bauteil-Aliasse nicht lesbar ({Error})", ex.GetType().Name);
            aliases = new Dictionary<string, string>();
        }
        _hints = new BuildingPartHintResolver(_parts, aliases, _aliasNormalizer);
    }

    /// <summary>Nach dem Zuordnen: Hinweis ohne Treffer + gewaehltes Bauteil → Alias-Vorschlag an der Zeile.</summary>
    private void SuggestAliases(RadialSelectionController controller, IEnumerable<CaptureRowViewModel> rows)
    {
        if (controller.SelectedType?.Ring2Source != Ring2Source.BuildingParts
            || controller.SelectedBuildingPart is not { } part)
            return;
        foreach (var row in rows)
        {
            var hint = row.Item.Candidates.BuildingPartHint?.Trim();
            if (string.IsNullOrEmpty(hint) || Hints.Resolve(hint) is not null
                || _declinedAliases.Contains(DeclineKey(hint, part)))
                continue;
            row.AliasSuggestion = new AliasSuggestion(hint, part);
        }
    }

    [RelayCommand]
    private void RememberAlias()
    {
        if (SelectedDetail?.Row.AliasSuggestion is not { } s)
            return;
        try
        {
            _bpmDb.AddBuildingPartAlias(_projectId, s.Part.Id, s.Hint);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException
                                       or Microsoft.Data.Sqlite.SqliteException)
        {
            StatusText = "⚠ Alias nicht gemerkt: " + ex.Message;
            Log.Warning("Bauteil-Alias nicht gespeichert ({Error})", ex.GetType().Name);
            return;
        }
        ReloadHints();
        ClearSuggestions(s.Hint);
        StatusText = $"✓ Alias gemerkt: „{s.Hint}“ = {s.PartName} (gilt im ganzen Projekt)";
        Log.Information("Bauteil-Alias gemerkt");
        SetSelectedRow();
    }

    [RelayCommand]
    private void DeclineAlias()
    {
        if (SelectedDetail?.Row.AliasSuggestion is not { } s)
            return;
        _declinedAliases.Add(DeclineKey(s.Hint, s.Part));
        ClearSuggestions(s.Hint);
        SetSelectedRow();
    }

    private void ClearSuggestions(string hint)
    {
        var key = Hints.Key(hint);
        foreach (var row in Rows)
            if (row.AliasSuggestion is { } x && Hints.Key(x.Hint) == key)
                row.AliasSuggestion = null;
    }

    private string DeclineKey(string hint, BuildingPart part) => $"{Hints.Key(hint)}|{part.Id}";

    /// <summary>Detail-Panel einer Zeile inkl. Bauteil-Hinweis und Alias-Vorschlag (Mockup 3.3 v1.2).</summary>
    private CaptureDetailViewModel BuildDetail(CaptureRowViewModel row)
    {
        var hint = row.Item.Candidates.BuildingPartHint?.Trim();
        var s = row.AliasSuggestion;
        var others = s is null ? 0
            : Rows.Count(r => r != row && r.AliasSuggestion is { } x && Hints.Key(x.Hint) == Hints.Key(s.Hint));
        return new CaptureDetailViewModel(row, BuildHistory(row))
        {
            HintText = HintText(hint, s is not null),
            AliasQuestion = s is null ? "" : $"„{s.Hint}“ → {s.PartName} — als Alias merken?",
            AliasExplanation = s is null ? ""
                : $"Dann erkennt BPM „{s.Hint}“ künftig in allen Dateinamen dieses Projekts als {s.PartName}."
                  + (others > 0 ? $" Gilt auch für die anderen {others} Datei(en) mit „{s.Hint}“." : "")
        };
    }

    private string HintText(string? hint, bool hasSuggestion)
    {
        if (string.IsNullOrEmpty(hint) || hasSuggestion)
            return "";
        if (Hints.Resolve(hint) is { } part)
            return $"Bauteil aus dem Dateinamen: „{hint}“ = {AliasSuggestion.NameOf(part)}"
                   + (Hints.IsAlias(hint) ? " (Alias)" : "");
        return $"Bauteil-Hinweis „{hint}“ – noch keinem Bauteil zugeordnet. Halten = Radial; danach fragt BPM, ob es ein Alias ist.";
    }
}

/// <summary>Vorschlag „Hinweis aus dem Dateinamen = Bauteil“ (BPM-121.03).</summary>
public sealed record AliasSuggestion(string Hint, BuildingPart Part)
{
    public string PartName => NameOf(Part);

    /// <summary>Anzeigename wie im Radial: Kuerzel, sonst Beschreibung.</summary>
    public static string NameOf(BuildingPart part)
        => !string.IsNullOrWhiteSpace(part.ShortName) ? part.ShortName : part.Description;
}

public partial class CaptureRowViewModel
{
    /// <summary>Offener Alias-Vorschlag nach dem Zuordnen (BPM-121.03), NULL = keiner.</summary>
    [ObservableProperty]
    private AliasSuggestion? _aliasSuggestion;
}

public sealed partial class CaptureDetailViewModel
{
    /// <summary>„Bauteil aus dem Dateinamen: „H1“ = Haus 1 (Alias)“ bzw. Hinweis ohne Treffer; leer = keiner.</summary>
    public string HintText { get; init; } = "";

    public string AliasQuestion { get; init; } = "";

    public string AliasExplanation { get; init; } = "";

    public bool HasAliasSuggestion => AliasQuestion.Length > 0;
}

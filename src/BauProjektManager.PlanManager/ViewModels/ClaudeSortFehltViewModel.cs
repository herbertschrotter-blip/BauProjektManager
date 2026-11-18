using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models;
using BauProjektManager.PlanManager.Services.ClaudeSortierung;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>Auswahl „Unterteilung“ eines neuen Dokumenttyps.</summary>
public sealed record ClaudeSortUnterteilung(Ring2Source Quelle, string Text)
{
    public override string ToString() => Text;
}

/// <summary>
/// Karte „Fehlt im Projekt“ im Chat (Mockup 3.4 v1.3, ADR-067 E11): vorbefuelltes Formular aus dem Vorschlag von
/// Claude, Herbert kann jedes Feld aendern. „Anlegen“ / „Nicht anlegen“ stehen im <see cref="ClaudeSortViewModel"/>.
/// </summary>
public sealed partial class ClaudeSortFehltViewModel : ObservableObject
{
    public static readonly IReadOnlyList<ClaudeSortUnterteilung> Unterteilungen =
    [
        new(Ring2Source.None, "keine (Empfehlung)"),
        new(Ring2Source.BuildingParts, "Bauteile"),
        new(Ring2Source.Categories, "Kategorien")
    ];

    public ClaudeSortFehltViewModel(ClaudeSortFehlt vorschlag, IReadOnlyList<string> dateiIds,
        IReadOnlyList<string> dateinamen, IReadOnlyList<BuildingPart> bauteile)
    {
        Vorschlag = vorschlag;
        DateiIds = dateiIds;
        Bauteile = bauteile;
        DateienText = "für " + string.Join(", ", dateinamen);
        _name = vorschlag.Name;
        _beschreibung = vorschlag.Beschreibung ?? "";
        _geschosseText = string.Join(", ", vorschlag.Geschosse);
        _bauteil = bauteile.FirstOrDefault(p => p.Id == vorschlag.BauteilId);
        _unterteilung = Unterteilungen.First(u => u.Quelle == vorschlag.Unterteilung);
    }

    public ClaudeSortFehlt Vorschlag { get; }
    public IReadOnlyList<string> DateiIds { get; }
    public IReadOnlyList<BuildingPart> Bauteile { get; }
    public string DateienText { get; }

    public bool IsBauteil => Vorschlag.Art == ClaudeSortFehltArt.Bauteil;
    public bool IsGeschoss => Vorschlag.Art == ClaudeSortFehltArt.Geschoss;
    public bool IsDokumenttyp => Vorschlag.Art == ClaudeSortFehltArt.Dokumenttyp;
    public bool HasBeschreibung => !IsDokumenttyp;

    public string Titel => Vorschlag.Art switch
    {
        ClaudeSortFehltArt.Bauteil => $"Bauteil „{(string.IsNullOrEmpty(Vorschlag.Beschreibung) ? Vorschlag.Name : Vorschlag.Beschreibung)}“ anlegen?",
        ClaudeSortFehltArt.Geschoss => $"Geschoss „{Vorschlag.Name}“ bei {Bauteile.FirstOrDefault(p => p.Id == Vorschlag.BauteilId)?.ShortName} anlegen?",
        _ => $"Dokumenttyp „{Vorschlag.Name}“ anlegen?"
    };

    public string NameLabel => Vorschlag.Art switch
    {
        ClaudeSortFehltArt.Bauteil => "Kurzname",
        _ => "Name"
    };

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _beschreibung;

    [ObservableProperty]
    private string _geschosseText;

    [ObservableProperty]
    private BuildingPart? _bauteil;

    [ObservableProperty]
    private ClaudeSortUnterteilung _unterteilung;

    /// <summary>True nach „Anlegen“ (erfolgreich) oder „Nicht anlegen“ — Formular gesperrt.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen))]
    private bool _isDone;

    public bool IsOpen => !IsDone;

    /// <summary>„✓ angelegt“, „nicht angelegt“ oder der Grund, warum nichts angelegt wurde.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErgebnis))]
    private string _ergebnisText = "";

    [ObservableProperty]
    private bool _isErgebnisOk;

    public bool HasErgebnis => ErgebnisText.Length > 0;

    /// <summary>Was angelegt werden soll — mit den Werten aus dem Formular.</summary>
    public ClaudeSortFehlt ZuAnlage() => Vorschlag with
    {
        Name = Name.Trim(),
        Beschreibung = string.IsNullOrWhiteSpace(Beschreibung) ? null : Beschreibung.Trim(),
        BauteilId = Bauteil?.Id ?? Vorschlag.BauteilId,
        Geschosse = [.. GeschosseText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
        Unterteilung = Unterteilung.Quelle
    };
}

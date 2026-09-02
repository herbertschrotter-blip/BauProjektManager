using System.Collections.ObjectModel;
using BauProjektManager.Domain.Interfaces;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>
/// Zustand des Segment-Editors (BPM-126c) — dasselbe Interaktionsmuster wie
/// ProfilWizard Schritt 2, aber als wiederverwendbarer Baustein: EINE Flaeche
/// aus Token-Kacheln mit klickbaren Trennzeichen dazwischen.
///
/// Trenner-Klick verschmilzt bzw. teilt die Nachbarn (pure Logik in
/// <see cref="FileNameSegmentation"/>); Typ-Zuweisungen haengen am stabilen
/// Atom-Anker und ueberleben das Umschalten.
/// </summary>
public partial class SegmentEditorViewModel : ObservableObject
{
    private SegmentationResult _source = new([""], []);
    private List<bool> _separatorState = [];

    /// <summary>Zuweisungen je Start-Atom-Index (stabil ueber Merge/Split).</summary>
    private readonly Dictionary<int, SegmentTypeDefinition> _assignments = [];

    /// <summary>Wird nach jeder Aenderung ausgeloest — der Host persistiert.</summary>
    public event EventHandler<SegmentAssignmentChangedEventArgs>? AssignmentChanged;

    /// <summary>Elemente der Flaeche in Reihenfolge: Token, Trenner, Token, …</summary>
    public ObservableCollection<SegmentElement> Elements { get; } = [];

    /// <summary>Palette der Segmenttypen (Drag-Quellen) aus dem Katalog (BPM-108).</summary>
    public ObservableCollection<SegmentTypeDefinition> Palette { get; } = [];

    /// <summary>Globale Trenner-Chips.</summary>
    public ObservableCollection<SeparatorChoice> SeparatorChoices { get; } = [];

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private bool _hasContent;

    /// <summary>Dateiendung inkl. Punkt — wird angezeigt, ist aber KEIN Segment.</summary>
    [ObservableProperty]
    private string _extension = "";

    /// <summary>
    /// Editor auf eine Datei setzen. <paramref name="existing"/> sind bereits
    /// gespeicherte Segmentwerte — sie werden ueber den Rohwert den Token zugeordnet.
    /// </summary>
    public void Load(
        string fileName,
        ISegmentTypeCatalog? catalog,
        IReadOnlyList<PlanDocumentSegment>? existing)
    {
        FileName = fileName;
        HasContent = fileName.Length > 0;
        _assignments.Clear();

        Palette.Clear();
        foreach (var type in catalog?.GetEffectiveActive() ?? [])
            Palette.Add(type);

        _source = FileNameSegmentation.Split(fileName);
        Extension = _source.Extension;
        _separatorState = FileNameSegmentation.InitialState(_source, FileNameSegmentation.SeparatorChars);

        SeparatorChoices.Clear();
        foreach (var c in FileNameSegmentation.SeparatorChars)
            SeparatorChoices.Add(new SeparatorChoice(c, _source.Separators.Contains(c)));

        RestoreExistingAssignments(existing);
        Rebuild();
    }

    /// <summary>Gespeicherte Werte den passenden Token zuordnen (Rohwert-Vergleich).</summary>
    private void RestoreExistingAssignments(IReadOnlyList<PlanDocumentSegment>? existing)
    {
        if (existing is null || existing.Count == 0 || Palette.Count == 0)
            return;
        var merged = FileNameSegmentation.Merge(_source, _separatorState);
        foreach (var segment in existing)
        {
            var type = Palette.FirstOrDefault(t => t.Id == segment.SegmentTypeId);
            if (type is null)
                continue;
            var token = merged.FirstOrDefault(m =>
                string.Equals(m.Text, segment.RawValue, StringComparison.OrdinalIgnoreCase));
            if (token is not null)
                _assignments[token.StartAtomIndex] = type;
        }
    }

    /// <summary>Trennzeichen an einer Position umschalten (Klick auf den Trenner).</summary>
    public void ToggleSeparator(int separatorIndex)
    {
        if (separatorIndex < 0 || separatorIndex >= _separatorState.Count)
            return;
        _separatorState[separatorIndex] = !_separatorState[separatorIndex];
        Rebuild();
    }

    /// <summary>Alle Trenner eines Zeichens umschalten (globaler Chip).</summary>
    public void ToggleSeparatorChar(char c)
    {
        var choice = SeparatorChoices.FirstOrDefault(s => s.Char == c);
        if (choice is null)
            return;
        choice.IsActive = !choice.IsActive;
        for (var i = 0; i < _source.Separators.Count; i++)
            if (_source.Separators[i] == c)
                _separatorState[i] = choice.IsActive;
        Rebuild();
    }

    /// <summary>Segmenttyp auf ein Token ziehen (Drag and Drop) oder entfernen (null).</summary>
    public void AssignType(int startAtomIndex, SegmentTypeDefinition? type)
    {
        var token = Elements.OfType<SegmentTokenElement>()
            .FirstOrDefault(t => t.StartAtomIndex == startAtomIndex);
        if (token is null)
            return;

        var previous = _assignments.GetValueOrDefault(startAtomIndex);
        if (type is null)
            _assignments.Remove(startAtomIndex);
        else
            _assignments[startAtomIndex] = type;

        Rebuild();
        AssignmentChanged?.Invoke(this,
            new SegmentAssignmentChangedEventArgs(type, previous, token.Text));
    }

    /// <summary>Aktueller Stand als Wertliste (Typ + Rohwert) — fuer den Host.</summary>
    public IReadOnlyList<(SegmentTypeDefinition Type, string RawValue)> CurrentAssignments()
        => [.. Elements.OfType<SegmentTokenElement>()
            .Where(t => t.AssignedType is not null)
            .Select(t => (t.AssignedType!, t.Text))];

    private void Rebuild()
    {
        Elements.Clear();
        if (!HasContent)
            return;

        var merged = FileNameSegmentation.Merge(_source, _separatorState);
        for (var i = 0; i < merged.Count; i++)
        {
            var segment = merged[i];
            // Letztes Atom dieses Tokens: bis kurz vor den Start des naechsten
            var endAtom = i < merged.Count - 1 ? merged[i + 1].StartAtomIndex - 1 : _source.Atoms.Count - 1;
            Elements.Add(new SegmentTokenElement(
                segment.StartAtomIndex, segment.Text,
                _assignments.GetValueOrDefault(segment.StartAtomIndex),
                BuildParts(segment.StartAtomIndex, endAtom)));

            if (i >= merged.Count - 1)
                continue;
            // Der trennende Separator liegt direkt vor dem Start-Atom des naechsten Tokens
            var separatorIndex = merged[i + 1].StartAtomIndex - 1;
            Elements.Add(new SegmentSeparatorElement(
                separatorIndex, _source.Separators[separatorIndex]));
        }
    }

    /// <summary>
    /// Bausteine einer Kachel: Atome und die darin VERSCHMOLZENEN (inaktiven)
    /// Trennzeichen — nur so bleiben sie klickbar und lassen sich wieder trennen.
    /// </summary>
    private List<SegmentTokenPart> BuildParts(int startAtom, int endAtom)
    {
        List<SegmentTokenPart> parts = [new SegmentTokenPart(_source.Atoms[startAtom])];
        for (var a = startAtom; a < endAtom; a++)
        {
            parts.Add(new SegmentTokenPart(_source.Separators[a].ToString(), a));
            parts.Add(new SegmentTokenPart(_source.Atoms[a + 1]));
        }
        return parts;
    }
}

/// <summary>Basis der Editor-Elemente (Token oder Trennzeichen).</summary>
public abstract class SegmentElement
{
    public bool IsSeparator => this is SegmentSeparatorElement;
    public bool IsToken => this is SegmentTokenElement;
}

/// <summary>
/// Ein Baustein innerhalb einer Token-Kachel: entweder Text oder ein inaktives
/// (verschmolzenes) Trennzeichen, das per Klick wieder trennt.
/// </summary>
public sealed class SegmentTokenPart(string text, int separatorIndex = -1)
{
    public string Text { get; } = text;
    public int SeparatorIndex { get; } = separatorIndex;
    public bool IsSeparator => SeparatorIndex >= 0;
    public bool IsText => SeparatorIndex < 0;
}

/// <summary>Ein sichtbares Segment-Token mit optionaler Typ-Zuweisung.</summary>
public sealed class SegmentTokenElement(
    int startAtomIndex, string text, SegmentTypeDefinition? assignedType,
    IReadOnlyList<SegmentTokenPart> parts) : SegmentElement
{
    public int StartAtomIndex { get; } = startAtomIndex;
    public string Text { get; } = text;
    public SegmentTypeDefinition? AssignedType { get; } = assignedType;

    /// <summary>Text-Bausteine inkl. klickbarer verschmolzener Trennzeichen.</summary>
    public IReadOnlyList<SegmentTokenPart> Parts { get; } = parts;

    public string TypeLabel => AssignedType?.Name ?? "? Typ wählen";
    public bool IsAssigned => AssignedType is not null;

    /// <summary>Feldtyp-Farbe aus dem Katalog; leer = ungesetzt (gestrichelte Kachel).</summary>
    public string ColorHex => AssignedType?.Color ?? "";
}
/// <summary>Ein klickbares Trennzeichen zwischen zwei Token.</summary>
public sealed class SegmentSeparatorElement(int separatorIndex, char separatorChar) : SegmentElement
{
    public int SeparatorIndex { get; } = separatorIndex;
    public char Char { get; } = separatorChar;
    public string Text { get; } = separatorChar == ' ' ? "␣" : separatorChar.ToString();
}

/// <summary>Globaler Trenner-Chip.</summary>
public sealed partial class SeparatorChoice(char separatorChar, bool isActive) : ObservableObject
{
    public char Char { get; } = separatorChar;
    public string Label { get; } = separatorChar == ' ' ? "␣" : separatorChar.ToString();

    [ObservableProperty]
    private bool _isActive = isActive;
}

/// <summary>Meldung an den Host: eine Zuweisung wurde gesetzt oder entfernt.</summary>
public sealed class SegmentAssignmentChangedEventArgs(
    SegmentTypeDefinition? newType, SegmentTypeDefinition? previousType, string rawValue) : EventArgs
{
    public SegmentTypeDefinition? NewType { get; } = newType;
    public SegmentTypeDefinition? PreviousType { get; } = previousType;
    public string RawValue { get; } = rawValue;
}

using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.Infrastructure.Services;
using BauProjektManager.PlanManager.Controls;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Ergebnis eines Segment-Commits: was der Gesten-Host am Control aktualisieren
/// muss. NULL-Listen = Ring unveraendert lassen; leere Liste = Ring entfernen.
/// Animate-Flags nur true beim ERSCHEINEN eines Rings (Spez: Inhaltswechsel stumm).
/// </summary>
public sealed record RadialUpdate(
    IReadOnlyList<RadialSegmentItem>? Ring2,
    bool Ring2Animate,
    IReadOnlyList<RadialSegmentItem>? Ring3,
    bool Ring3Animate,
    string CenterSecondary);

/// <summary>
/// Typabhaengige Ebenenlogik des Radials (BPM-111.05 Slice 2b) — reine,
/// UI-freie Zustandsmaschine nach Mockup-Spez:
/// - Ring 2 je <see cref="Ring2Source"/> des Dokumenttyps (Bauteile/Kategorien/keiner)
/// - Ring 3 (Geschosse je Bauteil) nur bei raeumlichem Schema
/// - Verweilen auf Ring 1 (auch gleicher Typ) schliesst Ring 3
/// - Ring-Erscheinen animiert, Inhaltswechsel stumm
/// </summary>
public class RadialSelectionController
{
    /// <summary>Label der „+ Neu…"-Segmente (Schnellanlage je Ebene, BPM-111.05 Slice 3).</summary>
    public const string AddItemLabel = "+ Neu…";

    private IReadOnlyList<PlanDocumentType> _types;
    private IReadOnlyList<BuildingPart> _parts;

    // folder_name-Erzeugung fuer den Fallback ohne gespeicherten folder_name —
    // spiegelt die statische Normalizer-Nutzung in ProjectDatabase (ADR-059-Addendum).
    private static readonly PlanValueNormalizer _normalizer = new();

    private bool _ring2Visible;
    private bool _ring3Visible;

    // BPM-111.05 Slice B (Teil 46): Rotations-Offset je Ring fuers Mausrad-Blaettern.
    // Feld-stabil — rotiert nur die ANZEIGE-Reihenfolge, die Stammdatenlisten bleiben
    // unveraendert. Index 0 = Ring 1, 1 = Ring 2, 2 = Ring 3.
    private readonly int[] _ringOffset = new int[3];

    public RadialSelectionController(
        IReadOnlyList<PlanDocumentType> types,
        IReadOnlyList<BuildingPart> parts)
    {
        _types = types;
        _parts = parts;
    }

    public PlanDocumentType? SelectedType { get; private set; }
    public string? SelectedPart { get; private set; }
    public string? SelectedLevel { get; private set; }

    /// <summary>Aktuell gewaehltes Bauteil als Objekt (fuer „+ Neu…" Ring 3 / Geschoss-Anlage).</summary>
    public BuildingPart? SelectedBuildingPart =>
        SelectedPart is null ? null : _parts.FirstOrDefault(p => EffectivePartName(p) == SelectedPart);

    /// <summary>
    /// Aktualisiert die Stammdaten nach einer Schnellanlage ("+ Neu…", Slice 3)
    /// und re-bindet <see cref="SelectedType"/> an das neu geladene Objekt
    /// (gleiche Id), damit z. B. frisch angelegte Kategorien sichtbar werden.
    /// </summary>
    public void RefreshStammdaten(
        IReadOnlyList<PlanDocumentType> types, IReadOnlyList<BuildingPart> parts)
    {
        _types = types;
        _parts = parts;
        if (SelectedType is not null)
            SelectedType = _types.FirstOrDefault(t => t.Id == SelectedType.Id) ?? SelectedType;
    }

    private static RadialSegmentItem NewItem() => new(AddItemLabel, IsAddItem: true);

    /// <summary>Ring 1 fuer den Capture-Start (Kandidat aus Extractor markiert).</summary>
    public IReadOnlyList<RadialSegmentItem> BuildRing1(PlanFileCandidates? candidates)
    {
        var candidateType = candidates?.TypeKeywords.FirstOrDefault();
        IReadOnlyList<RadialSegmentItem> items = [.. _types.Select(t => new RadialSegmentItem(
            t.Name, t.ColorHex,
            IsCandidate: candidateType is not null
                && string.Equals(t.Name, candidateType, StringComparison.OrdinalIgnoreCase))),
            NewItem()];
        return ApplyOffset(items, _ringOffset[0]);
    }

    /// <summary>Aktuelle Items eines Rings (1..3) inkl. Rotations-Offset — fuer das Mausrad-Neurendern.</summary>
    public IReadOnlyList<RadialSegmentItem> BuildRing(int ringIndex, PlanFileCandidates? candidates) => ringIndex switch
    {
        1 => BuildRing1(candidates),
        2 => BuildRing2(candidates),
        3 => BuildRing3(candidates),
        _ => []
    };

    /// <summary>Name des aktuell gewaehlten Segments eines Rings (Highlight beim Neurendern).</summary>
    public string? SelectedNameFor(int ringIndex) => ringIndex switch
    {
        1 => SelectedType?.Name,
        2 => SelectedPart,
        3 => SelectedLevel,
        _ => null
    };

    /// <summary>Mausrad: dreht NUR die angegebene Ebene (feld-stabil ueber Offset).</summary>
    public void RotateRing(int ringIndex, int delta)
    {
        if (ringIndex is >= 1 and <= 3)
            _ringOffset[ringIndex - 1] += delta;
    }

    /// <summary>
    /// Rotiert die Anzeige-Reihenfolge um <paramref name="off"/> Positionen, ohne die
    /// Datenliste zu veraendern. Das „+ Neu…"-Segment bleibt fix am Ende.
    /// </summary>
    private static IReadOnlyList<RadialSegmentItem> ApplyOffset(IReadOnlyList<RadialSegmentItem> items, int off)
    {
        if (items.Count == 0) return items;
        var hasAdd = items[^1].IsAddItem;
        var realCount = hasAdd ? items.Count - 1 : items.Count;
        if (realCount <= 1) return items;
        var norm = ((off % realCount) + realCount) % realCount;
        if (norm == 0) return items;
        var rotated = new List<RadialSegmentItem>(items.Count);
        for (var i = 0; i < realCount; i++)
            rotated.Add(items[(i + norm) % realCount]);
        if (hasAdd) rotated.Add(items[^1]);
        return rotated;
    }

    /// <summary>
    /// Weist jedem Feld (ohne „+ Neu…") eine feld-stabile Rampenfarbe dunkel→hell der
    /// Typfarbe nach urspruenglicher Position zu und haengt „+ Neu…" ans Ende.
    /// Der Offset wird ERST danach angewandt → jedes Feld behaelt seine Farbe.
    /// BPM-111.05 Slice C (Teil 46).
    /// </summary>
    private static IReadOnlyList<RadialSegmentItem> WithRamp(IReadOnlyList<RadialSegmentItem> baseItems, string? typeColor)
    {
        var n = baseItems.Count;
        var result = new List<RadialSegmentItem>(n + 1);
        for (var i = 0; i < n; i++)
        {
            var t = n > 1 ? (double)i / (n - 1) : 0.0;
            result.Add(baseItems[i] with { ColorHex = RampHex(typeColor, t) });
        }
        result.Add(NewItem());
        return result;
    }

    /// <summary>Farbe auf der Rampe dunkel (t=0, ~45%) → hell (t=1, ~65% aufgehellt) der Typfarbe.</summary>
    private static string RampHex(string? typeColor, double t)
    {
        var (r, g, b) = ParseHex(typeColor);
        static int Channel(int v, double tt)
        {
            double dark = v * 0.45;
            double light = v + (255 - v) * 0.65;
            return (int)Math.Round(dark + (light - dark) * tt);
        }
        return $"#{Channel(r, t):X2}{Channel(g, t):X2}{Channel(b, t):X2}";
    }

    private static (int R, int G, int B) ParseHex(string? hex)
    {
        if (!string.IsNullOrEmpty(hex))
        {
            var h = hex.TrimStart('#');
            if (h.Length == 6
                && int.TryParse(h.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r)
                && int.TryParse(h.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
                && int.TryParse(h.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                return (r, g, b);
        }
        return (24, 63, 90); // Fallback ~ Akzentblau
    }

    /// <summary>Setzt die Auswahl zurueck (neuer Capture-Vorgang).</summary>
    public void Reset()
    {
        SelectedType = null;
        SelectedPart = null;
        SelectedLevel = null;
        _ring2Visible = false;
        _ring3Visible = false;
        _ringOffset[0] = _ringOffset[1] = _ringOffset[2] = 0;
    }

    /// <summary>
    /// Verarbeitet einen Dwell-Commit des Controls und liefert die noetigen
    /// Ring-Updates fuer den Host.
    /// </summary>
    public RadialUpdate Commit(int ringIndex, string name, PlanFileCandidates? candidates)
    {
        switch (ringIndex)
        {
            case 1:
                SelectedType = _types.FirstOrDefault(t => t.Name == name);
                SelectedPart = null;
                SelectedLevel = null;
                _ringOffset[1] = 0;
                _ringOffset[2] = 0;

                var ring2 = BuildRing2(candidates);
                // Animation NUR beim Erscheinen (unsichtbar -> sichtbar);
                // Inhaltswechsel bei bereits sichtbarem Ring bleibt stumm (Spez)
                var ring2Animate = !_ring2Visible && ring2.Count > 0;
                _ring2Visible = ring2.Count > 0;
                _ring3Visible = false;
                return new RadialUpdate(ring2, ring2Animate, Ring3: [], Ring3Animate: false,
                    CenterSecondary: SelectedType?.Name ?? "");

            case 2:
                SelectedPart = name;
                SelectedLevel = null;
                _ringOffset[2] = 0;
                var ring3 = BuildRing3(candidates);
                var ring3Animate = !_ring3Visible && ring3.Count > 0;
                _ring3Visible = ring3.Count > 0;
                return new RadialUpdate(Ring2: null, Ring2Animate: false,
                    Ring3: ring3, Ring3Animate: ring3Animate,
                    CenterSecondary: $"{SelectedType?.Name} › {name}");

            case 3:
                SelectedLevel = name;
                return new RadialUpdate(null, false, null, false,
                    $"{SelectedType?.Name} › {SelectedPart} › {name}");

            default:
                return new RadialUpdate(null, false, null, false, "");
        }
    }

    /// <summary>Zielordner relativ zum Projekt aus den gespeicherten folder_names.</summary>
    public string BuildTargetDirectory(string plansRelativePath)
    {
        var segments = new List<string> { plansRelativePath };
        if (SelectedType is not null)
            segments.Add(SelectedType.FolderName);

        if (SelectedType?.Ring2Source == Ring2Source.BuildingParts && SelectedPart is not null)
        {
            var part = _parts.FirstOrDefault(p => EffectivePartName(p) == SelectedPart);
            // Gespeicherten folder_name bevorzugen; bei Altdaten ohne folder_name
            // den Anzeigenamen normalisieren (kein leerer Pfad-Teil mehr).
            segments.Add(part?.FolderName is { Length: > 0 } fn
                ? fn
                : _normalizer.NormalizeForFolderName(SelectedPart));
            if (SelectedLevel is not null)
                segments.Add(SelectedLevel);
        }
        else if (SelectedType?.Ring2Source == Ring2Source.Categories && SelectedPart is not null)
        {
            var category = SelectedType.Categories.FirstOrDefault(c => c.Name == SelectedPart);
            segments.Add(category?.FolderName is { Length: > 0 } fn ? fn : SelectedPart);
        }

        return string.Join('/', segments.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private IReadOnlyList<RadialSegmentItem> BuildRing2(PlanFileCandidates? candidates)
    {
        if (SelectedType is null)
            return [];

        // Basis-Items OHNE „+ Neu…" — die Rampe wird nach Original-Position vergeben,
        // dann NewItem angehaengt (WithRamp) und zuletzt der Offset angewandt.
        // Ring2Source.None bekommt KEIN Add-Item (kein Unter-Schema vorhanden).
        IReadOnlyList<RadialSegmentItem> baseItems = SelectedType.Ring2Source switch
        {
            Ring2Source.BuildingParts => [.. _parts.Select(p => new RadialSegmentItem(
                EffectivePartName(p),
                IsCandidate: candidates?.BuildingPartHint is not null
                    && string.Equals(EffectivePartName(p), candidates.BuildingPartHint, StringComparison.OrdinalIgnoreCase)))],
            Ring2Source.Categories => [.. SelectedType.Categories.Select(c => new RadialSegmentItem(c.Name))],
            _ => []
        };
        if (baseItems.Count == 0)
            return [];
        return ApplyOffset(WithRamp(baseItems, SelectedType.ColorHex), _ringOffset[1]);
    }

    /// <summary>
    /// Anzeige- UND Identitaetsname eines Bauteils im Radial: das Kuerzel,
    /// sonst (Altdaten ohne Kuerzel) die Beschreibung. Kuerzel ist seit
    /// BPM-111.05 Pflicht im Bauteil-Editor — dieser Fallback schuetzt nur
    /// vor leeren Bestandsdaten und haelt Label/Match/FolderName konsistent.
    /// </summary>
    private static string EffectivePartName(BuildingPart part) =>
        !string.IsNullOrWhiteSpace(part.ShortName) ? part.ShortName : part.Description;

    private IReadOnlyList<RadialSegmentItem> BuildRing3(PlanFileCandidates? candidates)
    {
        if (SelectedType?.Ring2Source != Ring2Source.BuildingParts || SelectedPart is null)
            return [];

        var part = _parts.FirstOrDefault(p => EffectivePartName(p) == SelectedPart);
        if (part is null)
            return [];

        // Geschosse; Rampe nach Original-Position, NewItem via WithRamp (auch bei 0
        // Geschossen, damit ein Bauteil ohne Geschosse direkt eines anlegen kann).
        IReadOnlyList<RadialSegmentItem> baseItems = [.. part.Levels.Select(l => new RadialSegmentItem(
            l.Name,
            IsCandidate: candidates?.Level is not null
                && string.Equals(l.Name, candidates.Level, StringComparison.OrdinalIgnoreCase)))];
        return ApplyOffset(WithRamp(baseItems, SelectedType.ColorHex), _ringOffset[2]);
    }
}

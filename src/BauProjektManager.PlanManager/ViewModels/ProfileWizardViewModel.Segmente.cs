using System.Collections.ObjectModel;
using BauProjektManager.Domain.Enums.PlanManager;
using BauProjektManager.Domain.Models.PlanManager;
using BauProjektManager.PlanManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BauProjektManager.PlanManager.ViewModels;

/// <summary>Schritt 2 (Segmente aus dem Segment-Editor) und Schritt 3 (Index) des Profil-Wizards.</summary>
public partial class ProfileWizardViewModel
{
    /// <summary>Kacheln aus dem Segment-Editor in Reihenfolge (Position = Kachel-Nummer).</summary>
    [ObservableProperty]
    private ObservableCollection<FileNameSegment> _segments = [];

    /// <summary>Trennzeichen fuers Profil (aus dem Editor: alle Stellen trennen).</summary>
    public IReadOnlyList<string> Delimiters { get; private set; } = [];

    [ObservableProperty]
    private int _openTileCount;

    [ObservableProperty]
    private bool _hasMixedSeparators;

    /// <summary>Statuszeile unter dem Editor (Mockup 3.8 „2 Segmente“ / „2 Segment offen“).</summary>
    [ObservableProperty]
    private string _step2Message = "";

    [ObservableProperty]
    private bool _step2Ok;

    /// <summary>True, solange Kacheln ohne Typ sind — zeigt „alle offenen auf Ignorieren“.</summary>
    public bool CanIgnoreOpen => OpenTileCount > 0 && IgnoreType is not null;

    public bool HasPlanIndexSegment => HasSegmentWithRole(SegmentSemanticRole.PlanIndex);

    public bool HasPlanNumberSegment => HasSegmentWithRole(SegmentSemanticRole.PlanNumber);

    /// <summary>
    /// Stand des Segment-Editors uebernehmen (nach jedem Laden und jeder Zuweisung).
    /// </summary>
    /// <param name="tokens">Kacheln in Reihenfolge: Text und zugewiesener Segmenttyp (NULL = offen).</param>
    /// <param name="delimiters">Trennzeichen, die an allen Stellen trennen.</param>
    /// <param name="mixedSeparators">True, wenn ein Zeichen teils trennt und teils verbindet.</param>
    public void ApplyEditorState(
        IReadOnlyList<(string Text, string? TypeId)> tokens, IReadOnlyList<char> delimiters, bool mixedSeparators)
    {
        Segments = new ObservableCollection<FileNameSegment>(tokens.Select((t, i) => new FileNameSegment
        {
            Position = i,
            RawValue = t.Text,
            FieldTypeId = t.TypeId
        }));
        Delimiters = [.. delimiters.Select(c => c.ToString())];
        HasMixedSeparators = mixedSeparators;
        OpenTileCount = tokens.Count(t => t.TypeId is null);

        var offen = tokens.Where(t => t.TypeId is null).Select(t => t.Text).ToList();
        var planNumbers = Segments.Count(s => GetRoleForFieldTypeId(s.FieldTypeId) == SegmentSemanticRole.PlanNumber);
        // Gegenprobe: der Import zerlegt mit FileNameParser + Profil-Trennern — die Kacheln muessen dazu passen
        var parsed = string.IsNullOrWhiteSpace(EditorFileName)
            ? []
            : FileNameParser.Parse(EditorFileName, [.. delimiters]).Segments.Select(s => s.RawValue);
        var passt = mixedSeparators || parsed.SequenceEqual(tokens.Select(t => t.Text.Trim()));
        (Step2Ok, Step2Message) = (mixedSeparators, offen.Count, planNumbers) switch
        {
            (true, _, _) => (false, "Ein Trennzeichen trennt an einer Stelle und verbindet an einer anderen – " +
                                    "im Profil gilt ein Trennzeichen überall gleich. Bitte einheitlich setzen."),
            _ when !passt => (false, "So lässt sich die Zerlegung nicht als Erkennung speichern (z. B. doppelte " +
                                     "Trennzeichen). Bitte Trenner anders setzen."),
            (_, > 0, _) => (false, $"{offen.Count} Kachel(n) ohne Typ ({string.Join(", ", offen)}). " +
                                   "Zuweisen oder „Ignorieren“."),
            (_, _, 0) => (false, "Eine Kachel braucht den Typ Plannummer."),
            (_, _, > 1) => (false, "Nur eine Kachel darf den Typ Plannummer haben."),
            _ => (true, "Alle Kacheln haben einen Typ. Plannummer ist zugewiesen.")
        };
        OnPropertyChanged(nameof(CanIgnoreOpen));
        OnPropertyChanged(nameof(HasPlanIndexSegment));
        OnPropertyChanged(nameof(HasPlanNumberSegment));
        ValidateCurrentStep();
    }

    /// <summary>Schritt 2: alle Kacheln typisiert, genau eine Plannummer, Trenner einheitlich.</summary>
    private bool ValidateStep2() => Segments.Count > 0 && Step2Ok;

    private bool HasSegmentWithRole(SegmentSemanticRole role)
        => Segments.Any(s => GetRoleForFieldTypeId(s.FieldTypeId) == role);

    // === Schritt 3: Index ===

    public IReadOnlyList<IndexSourceOption> IndexSourceOptions { get; } =
    [
        new("Im Dateinamen", IndexSourceType.FileName,
            "Segment „Index“ aus Schritt 2; fehlt er bei einer Datei, ist der Plan ohne Index."),
        new("Kein Index im Dateinamen", IndexSourceType.None,
            "Der Index wird beim Erfassen von Hand oder aus dem Plankopf gesetzt.")
    ];

    [ObservableProperty]
    private IndexSourceType _selectedIndexSource = IndexSourceType.FileName;

    [ObservableProperty]
    private bool _indexCaseInsensitive = true;

    /// <summary>
    /// Hinweis statt Sperre (Praxis-Bug BPM-130): „Im Dateinamen“ ohne Index-Segment ist erlaubt — der Index ist
    /// optional, Dateien ohne Index sind Plaene ohne Index.
    /// </summary>
    [ObservableProperty]
    private bool _showIndexWarning;

    partial void OnSelectedIndexSourceChanged(IndexSourceType value) => UpdateIndexWarning();

    private void UpdateIndexWarning()
        => ShowIndexWarning = SelectedIndexSource == IndexSourceType.FileName && !HasPlanIndexSegment;
}

/// <summary>Auswahl in Schritt 3 (Index).</summary>
public class IndexSourceOption(string label, IndexSourceType value, string description)
{
    public string Label { get; } = label;
    public IndexSourceType Value { get; } = value;
    public string Description { get; } = description;

    public override string ToString() => Label;
}

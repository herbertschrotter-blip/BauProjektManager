using System.IO;
using System.Text.RegularExpressions;
using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Lightweight-Extractor (BPM-111.02, ADR-059): liest deterministisch
/// PlanNr-/Index-/Geschoss-/Plantyp-KANDIDATEN aus Dateinamen.
/// NUR Assist — "B entscheidet, A schlaegt vor": Ergebnisse werden nie
/// automatisch persistiert, sondern speisen Kandidaten-Spalte, Radial-
/// Vorbelegung und das Bucket-Matching (BPM-111.03).
/// V1-Teil des BPM-007.02-Splits; volle FieldExtractionRule/Regex = post-V1.
/// </summary>
public class LightweightPlanExtractor
{
    // Windows-Kopiermarker am Ende: "Plan_011_EG_(1)" / "Plan (2)"
    private static readonly Regex _copyMarker =
        new(@"^(?<core>.+?)[ _\-]*\((?<n>\d+)\)$", RegexOptions.Compiled);

    private static readonly Regex _date =
        new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);

    // Geschoss: STRIKTE Tokenliste (Haus-vs-Geschoss-Schutz: "H2" matcht NICHT)
    private static readonly Regex _level =
        new(@"^(?:KG|EG|DG|(?:OG|UG)\d{1,2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Bauteil-Verdacht (roher Hint fuers Stammdaten-Matching in 111.03)
    private static readonly Regex _buildingPart =
        new(@"^(?:H\d{1,2}|Haus ?\d{1,2}|TG)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Plannummer mit optionalem Buchstaben-Prefix und angehaengtem Index per "-X"
    // Beispiele: 5998-203 | 5998-100-B | S-103-C | B-101 | 011 | 21005
    private static readonly Regex _planNumber =
        new(@"^(?<nr>(?:[A-Za-z]{1,3}-)?\d{2,5}(?:-\d{1,4})?)(?:-(?<idx>[A-Za-z]))?$",
            RegexOptions.Compiled);

    // Index OHNE Trenner an die Nummer geklebt: 011vorab | 002a
    private static readonly Regex _planNumberGluedIndex =
        new(@"^(?<nr>\d{2,5})(?<idx>vorabzug|vorab|va|[A-Za-z])$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _standaloneIndex =
        new(@"^(?:[A-Za-z]|vorabzug|vorab|va)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Plantyp-/Protokoll-Schluesselwoerter -> kanonische Form.
    /// Laengste zuerst pruefen (sicherheitsprotokoll vor protokoll).
    /// </summary>
    private static readonly (string Keyword, string Canonical, bool IsPlanType)[] _typeKeywords =
    [
        ("sicherheitsprotokoll", "Sicherheitsprotokoll", false),
        ("bautagesbericht",      "Bautagesbericht",      false),
        ("baubesprechung",       "Baubesprechung",       false),
        ("polierplan",           "Polierplan",           true),
        ("architektur",          "Architektur",          true),
        ("bewehrung",            "Bewehrung",            true),
        ("lageplan",             "Lageplan",             true),
        ("schalung",             "Schalung",             true),
        ("protokoll",            "Protokoll",            false),
        ("fertigteil",           "Fertigteile",          true),
        ("abnahme",              "Abnahme",              false),
        ("statik",               "Statik",               true),
        ("detail",               "Detail",               true),
        ("sipro",                "Sicherheitsprotokoll", false)
    ];

    /// <summary>
    /// Extrahiert Kandidaten aus einem Dateinamen. Reine Funktion, wirft nicht
    /// (leerer/unbrauchbarer Name ergibt leere Kandidaten).
    /// </summary>
    public PlanFileCandidates ExtractCandidates(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Empty(fileName ?? string.Empty);

        var baseName = Path.GetFileNameWithoutExtension(fileName).Trim();

        // 1. Kopiermarker "(n)" am Ende strippen
        var hasCopyMarker = false;
        var copy = _copyMarker.Match(baseName);
        if (copy.Success)
        {
            baseName = copy.Groups["core"].Value.Trim();
            hasCopyMarker = true;
        }

        // 2. Tokenisierung: '_' ist Haupt-Trenner (Bindestriche bleiben in
        //    Plannummern wie "5998-203" erhalten — bewusst NICHT FileNameParser-
        //    Default, der an '-' splittet)
        var tokens = baseName
            .Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        string? planNumber = null, index = null, level = null, partHint = null, date = null;
        var planNumberPos = -1;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (date is null && _date.IsMatch(token))
            {
                date = token;
                continue;
            }
            if (level is null && _level.IsMatch(token))
            {
                level = token.ToUpperInvariant();
                continue;
            }
            if (partHint is null && _buildingPart.IsMatch(token))
            {
                partHint = token;
                continue;
            }
            if (planNumber is null)
            {
                var glued = _planNumberGluedIndex.Match(token);
                if (glued.Success)
                {
                    planNumber = glued.Groups["nr"].Value;
                    index = glued.Groups["idx"].Value;
                    planNumberPos = i;
                    continue;
                }
                var m = _planNumber.Match(token);
                if (m.Success)
                {
                    planNumber = m.Groups["nr"].Value;
                    if (m.Groups["idx"].Success)
                        index = m.Groups["idx"].Value;
                    planNumberPos = i;
                }
            }
        }

        // 3. Alleinstehender Index-Token NACH der Plannummer (z. B. "5998-100_B_KG")
        if (planNumber is not null && index is null)
        {
            for (var i = planNumberPos + 1; i < tokens.Count; i++)
            {
                if (_standaloneIndex.IsMatch(tokens[i]))
                {
                    index = tokens[i];
                    break;
                }
            }
        }

        // 4. Plantyp-/Protokoll-Keywords (laengste zuerst, Treffer aus Scan entfernen)
        var typeKeywords = new List<string>();
        var planTypeCount = 0;
        var scan = baseName.ToLowerInvariant();
        foreach (var (keyword, canonical, isPlanType) in _typeKeywords)
        {
            if (!scan.Contains(keyword))
                continue;
            scan = scan.Replace(keyword, "");
            if (typeKeywords.Contains(canonical))
                continue;
            typeKeywords.Add(canonical);
            if (isPlanType)
                planTypeCount++;
        }

        return new PlanFileCandidates(
            FileName: fileName,
            PlanNumber: planNumber,
            Index: index,
            RevisionKind: RevisionKindDetector.Detect(index),
            Level: level,
            BuildingPartHint: partHint,
            TypeKeywords: typeKeywords,
            DateCandidate: date,
            HasCopyMarker: hasCopyMarker,
            IsCombi: planTypeCount > 1);
    }

    private static PlanFileCandidates Empty(string fileName) => new(
        fileName, null, null, Domain.Enums.PlanManager.RevisionKind.None,
        null, null, [], null, false, false);
}

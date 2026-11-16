using System.Text;
using BauProjektManager.Domain.Models;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Baut den Plankopf-Text fuer kontext.json aus den Woertern der ersten PDF-Seite
/// (IPdfTextService, ADR-063 — kein OCR). Heuristik: der Plankopf sitzt auf
/// Bauplaenen unten rechts, also zuerst die Woerter im rechten unteren Viertel
/// der Textausdehnung; ist dort nichts, die ganze Seite. Woerter werden zu Zeilen
/// gruppiert (gleiche Hoehe) und auf <see cref="MaxZeichen"/> gekuerzt.
/// </summary>
public static class ClaudeSortPlankopf
{
    /// <summary>Obergrenze je Datei — haelt kontext.json klein.</summary>
    public const int MaxZeichen = 4000;

    /// <summary>Plankopf-Text aus den Woertern einer Seite; leer bei keiner Textebene.</summary>
    public static string AusWoertern(IReadOnlyList<PdfWord> words)
    {
        var usable = words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
        if (usable.Count == 0)
            return string.Empty;

        var right = usable.Max(w => w.XMm + w.WidthMm);
        var bottom = usable.Max(w => w.YMm + w.HeightMm);
        var head = usable
            .Where(w => w.XMm + w.WidthMm / 2 >= right / 2 && w.YMm + w.HeightMm / 2 >= bottom / 2)
            .ToList();

        return Kuerzen(ZuZeilen(head.Count > 0 ? head : usable));
    }

    private static string ZuZeilen(List<PdfWord> words)
    {
        var sb = new StringBuilder();
        double? lineY = null;
        double lineHeight = 0;
        var line = new List<PdfWord>();

        foreach (var w in words.OrderBy(w => w.YMm).ThenBy(w => w.XMm))
        {
            var tolerance = Math.Max(lineHeight, w.HeightMm) * 0.5;
            if (lineY is { } y && Math.Abs(w.YMm - y) > tolerance)
            {
                AppendLine(sb, line);
                line.Clear();
                lineY = null;
            }
            lineY ??= w.YMm;
            lineHeight = w.HeightMm;
            line.Add(w);
        }
        AppendLine(sb, line);
        return sb.ToString().TrimEnd();
    }

    private static void AppendLine(StringBuilder sb, List<PdfWord> line)
    {
        if (line.Count == 0)
            return;
        sb.AppendLine(string.Join(" ", line.OrderBy(w => w.XMm).Select(w => w.Text.Trim())));
    }

    private static string Kuerzen(string text)
        => text.Length <= MaxZeichen ? text : text[..MaxZeichen] + " …";
}

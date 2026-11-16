using System.IO;
using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Prueft die gueltigen Zeilen eines Entwurfs untereinander — was der manuelle Weg ueber
/// BulkPrecheck und die PDF/DWG-Paarbildung absichert:
/// <list type="bullet">
/// <item>Zwei Zeilen mit gleichem document_key und gleicher Endung (z. B. Index A und B als zwei PDFs)
/// wuerden still als Zusatzdatei angehaengt — beide werden abgelehnt.</item>
/// <item>PDF und DWG mit gleichem Dateistamm muessen dieselbe Zuordnung haben (eine Revision, zwei
/// Dateien). Weichen sie ab oder fehlt der Partner in den gueltigen Zeilen, werden die Zeilen abgelehnt.</item>
/// </list>
/// Abgelehnte Zeilen gehen mit klarer Meldung an Claude zurueck.
/// </summary>
internal static class ClaudeSortEntwurfKonsistenz
{
    private static readonly HashSet<string> PaarEndungen = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".dwg" };

    /// <summary>Entfernt widerspruechliche Zeilen aus <paramref name="zeilen"/> und meldet sie.</summary>
    public static void Pruefe(
        List<ClaudeSortZeile> zeilen, ClaudeSortAuftrag auftrag, List<string> fehler, IPlanValueNormalizer normalizer)
    {
        var abgelehnt = new HashSet<string>(StringComparer.Ordinal);
        PruefeDoppelteSchluessel(zeilen, fehler, normalizer, abgelehnt);
        PruefePaare(zeilen, auftrag, fehler, abgelehnt);
        zeilen.RemoveAll(z => abgelehnt.Contains(z.DateiId));
    }

    /// <summary>document_key wie bei der manuellen Erstaufnahme (CaptureConfirmService, ADR-059 P.3).</summary>
    public static string Schluessel(ClaudeSortZeile z, IPlanValueNormalizer normalizer) =>
        CaptureConfirmService.BuildDocumentKey(
            z.Type.Id,
            z.PlanNumber ?? Path.GetFileNameWithoutExtension(z.Item.File.Scan.FileName),
            z.Part?.Id ?? z.Category?.Id,
            z.Level?.Id,
            normalizer);

    private static void PruefeDoppelteSchluessel(
        List<ClaudeSortZeile> zeilen, List<string> fehler, IPlanValueNormalizer normalizer, HashSet<string> abgelehnt)
    {
        var gruppen = zeilen
            .GroupBy(z => (Schluessel(z, normalizer), z.Item.File.Scan.Extension.ToLowerInvariant()))
            .Where(g => g.Count() > 1);
        foreach (var g in gruppen)
        {
            var ids = string.Join(", ", g.Select(z => z.DateiId));
            fehler.Add($"{ids}: gleiche Zuordnung (Typ, Plannummer, Bauteil/Kategorie, Geschoss) und gleiche Endung — " +
                       "das wären zwei Dateien desselben Plans. Verschiedene Indizes sind getrennte Revisionen: " +
                       "eine davon in nicht_zuordenbar mit Grund aufnehmen.");
            foreach (var z in g)
                abgelehnt.Add(z.DateiId);
        }
    }

    private static void PruefePaare(
        List<ClaudeSortZeile> zeilen, ClaudeSortAuftrag auftrag, List<string> fehler, HashSet<string> abgelehnt)
    {
        var gueltig = zeilen.ToDictionary(z => z.DateiId, StringComparer.Ordinal);
        var paare = auftrag.Dateien
            .Where(d => PaarEndungen.Contains(d.Value.File.Scan.Extension))
            .GroupBy(d => Stamm(d.Value.File.Scan.RelativePath), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(d => d.Value.File.Scan.Extension.ToLowerInvariant()).Distinct().Count() > 1);

        foreach (var paar in paare)
        {
            var ids = paar.Select(d => d.Key).ToList();
            var imEntwurf = ids.Where(gueltig.ContainsKey).Select(id => gueltig[id]).ToList();
            if (imEntwurf.Count == 0)
                continue;
            var namen = string.Join(" und ", ids);
            if (imEntwurf.Count < ids.Count)
                fehler.Add($"{namen}: PDF und DWG desselben Plans — beide zuordnen oder beide in nicht_zuordenbar.");
            else if (imEntwurf.Select(Zuordnung).Distinct().Count() > 1)
                fehler.Add($"{namen}: PDF und DWG desselben Plans brauchen dieselbe Zuordnung (Typ, Bauteil/Kategorie, " +
                           "Geschoss, Plannummer, Index).");
            else
                continue;
            foreach (var z in imEntwurf)
                abgelehnt.Add(z.DateiId);
        }
    }

    private static string Stamm(string relativePath)
        => Path.Combine(Path.GetDirectoryName(relativePath) ?? string.Empty, Path.GetFileNameWithoutExtension(relativePath));

    private static (string Typ, string? Ring2, string? Geschoss, string? Nummer, string? Index) Zuordnung(ClaudeSortZeile z)
        => (z.Type.Id, z.Part?.Id ?? z.Category?.Id, z.Level?.Id, z.PlanNumber, z.Index);
}

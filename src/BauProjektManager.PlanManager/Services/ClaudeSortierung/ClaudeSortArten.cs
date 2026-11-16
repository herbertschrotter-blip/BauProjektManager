using System.IO;
using System.Reflection;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Vorschriften fuer "Mit Claude sortieren" (Vorbild ha-hausbau claude/arten):
/// Rahmen fuer alle Auftraege + Vorschrift und Schema der Art "planzuordnung".
/// Liegen als eingebettete Ressourcen in der PlanManager-DLL (kein Laufzeit-Pfad,
/// keine Benutzerdaten) — der Auftrags-Builder schreibt sie in den Auftragsordner.
/// </summary>
public static class ClaudeSortArten
{
    /// <summary>Name der Art (auch Praefix der Schema-Datei).</summary>
    public const string Art = "planzuordnung";

    /// <summary>Auftragstext im Auftragsordner: Rahmen + Vorschrift der Art.</summary>
    public const string AuftragDatei = "auftrag.md";

    /// <summary>Schema-Datei im Auftragsordner (Name wie im Rahmen beschrieben).</summary>
    public const string SchemaDatei = Art + ".schema.json";

    /// <summary>Kontext der App (Stammdaten-IDs, bekannte Plaene, Eingangsdateien).</summary>
    public const string KontextDatei = "kontext.json";

    /// <summary>Ergebnis von Claude — die einzige Datei, die Claude schreiben darf.</summary>
    public const string EntwurfDatei = "entwurf.json";

    private const string ResourcePrefix = "BauProjektManager.PlanManager.ClaudeSortierung.Arten.";

    /// <summary>Rahmen fuer alle Auftraege (_rahmen.md).</summary>
    public static string Rahmen => Load("_rahmen.md");

    /// <summary>Vorschrift der Art (planzuordnung.md).</summary>
    public static string Vorschrift => Load(Art + ".md");

    /// <summary>JSON-Schema des Entwurfs (planzuordnung.schema.json).</summary>
    public static string Schema => Load(SchemaDatei);

    /// <summary>Vollstaendiger Auftragstext: Rahmen, Trenner, Vorschrift der Art.</summary>
    public static string AuftragText => Rahmen.TrimEnd() + "\n\n---\n\n" + Vorschrift.TrimEnd() + "\n";

    private static string Load(string fileName)
    {
        var assembly = typeof(ClaudeSortArten).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Eingebettete Vorschrift fehlt: {fileName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

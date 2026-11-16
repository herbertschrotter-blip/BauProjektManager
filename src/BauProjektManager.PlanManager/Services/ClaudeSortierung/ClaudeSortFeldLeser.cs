using System.Text.Json;

namespace BauProjektManager.PlanManager.Services.ClaudeSortierung;

/// <summary>
/// Liest Felder eines Objekts aus entwurf.json und meldet Typ- und Laengenfehler sowie
/// unbekannte Felder als deutsche Fehler (gehen als „Entwurf ungültig“ an Claude zurück).
/// </summary>
internal sealed class ClaudeSortFeldLeser
{
    private readonly List<string> _fehler;
    private readonly string _ort;
    private readonly JsonElement _obj;

    /// <param name="fehler">Fehlerliste des Pruefdurchlaufs.</param>
    /// <param name="ort">Ortsangabe fuer Meldungen, z. B. "zuordnungen[3] (D-003)".</param>
    /// <param name="obj">Das zu lesende JSON-Objekt.</param>
    /// <param name="erlaubt">Erlaubte Feldnamen; alle anderen sind ein Fehler.</param>
    public ClaudeSortFeldLeser(List<string> fehler, string ort, JsonElement obj, IReadOnlyCollection<string> erlaubt)
    {
        _fehler = fehler;
        _ort = ort;
        _obj = obj;
        foreach (var p in obj.EnumerateObject().Where(p => !erlaubt.Contains(p.Name)))
            fehler.Add($"{ort}: unbekanntes Feld '{p.Name}'.");
    }

    /// <summary>Text-Feld (getrimmt). Optional: fehlend, null oder leer → null.</summary>
    public string? Text(string name, bool pflicht, int maxLaenge)
    {
        if (!_obj.TryGetProperty(name, out var el) || el.ValueKind == JsonValueKind.Null)
        {
            if (pflicht)
                _fehler.Add($"{_ort}: Feld '{name}' fehlt.");
            return null;
        }
        if (el.ValueKind != JsonValueKind.String)
        {
            _fehler.Add($"{_ort}: '{name}' muss Text sein.");
            return null;
        }
        var value = (el.GetString() ?? string.Empty).Trim();
        if (value.Length > maxLaenge)
        {
            _fehler.Add($"{_ort}: '{name}' ist zu lang (höchstens {maxLaenge} Zeichen).");
            return null;
        }
        return value.Length == 0 && !pflicht ? null : value;
    }

    /// <summary>Liste von Texten aus einer festen Wertemenge; fehlend/null → leer.</summary>
    public IReadOnlyList<string> Werte(string name, IReadOnlySet<string> erlaubt)
    {
        if (!_obj.TryGetProperty(name, out var el) || el.ValueKind == JsonValueKind.Null)
            return [];
        if (el.ValueKind != JsonValueKind.Array)
        {
            _fehler.Add($"{_ort}: '{name}' muss eine Liste sein.");
            return [];
        }
        var werte = new List<string>();
        foreach (var w in el.EnumerateArray())
        {
            var text = w.ValueKind == JsonValueKind.String ? w.GetString() : null;
            if (text is null || !erlaubt.Contains(text))
                _fehler.Add($"{_ort}: '{name}' enthält '{w}' — erlaubt: {string.Join(", ", erlaubt)}.");
            else if (!werte.Contains(text))
                werte.Add(text);
        }
        return werte;
    }
}

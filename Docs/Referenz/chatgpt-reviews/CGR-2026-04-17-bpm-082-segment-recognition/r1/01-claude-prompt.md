# Runde 1 — Claude-Prompt an ChatGPT

> **Hinweis:** Dieser Prompt wurde nachträglich aus dem Chat-Verlauf von Teil 20 rekonstruiert. Der genaue Wortlaut des damals an ChatGPT gesendeten Prompts kann leicht abweichen.

---

## Rolle

Du bist ein erfahrener Software-Architekt mit Schwerpunkt .NET/WPF und Desktop-Anwendungen. Du führst ein technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic). Dein Kollege hat im Projekt **BauProjektManager (BPM)** einen Architektur-Bug entdeckt und schlägt einen Refactor vor. Bitte bewerte die vorgeschlagene Lösung.

## Bug-Kontext

Im `DocumentTypeRecognizer` (PlanManager-Modul) gibt es ein positionsblindes Erkennungsproblem:

**Aktuelle `RecognitionRule`:**
```csharp
public class RecognitionRule
{
    public string Method { get; set; } = "contains";  // "prefix" | "contains" | "regex"
    public string Pattern { get; set; } = string.Empty;
}
```

**Bug-Szenario:**
- Profil "Bauprotokoll" mit Rule `{method: "contains", pattern: "PROT"}`
- Match auf `PROJ-PROT-2025-01.pdf` ✓ (gewollt — PROT an Position 1)
- Match auf `RK-PROTOKOLL-EG.pdf` ✗ (NICHT gewollt — PROTOKOLL an Position 1, fälschlich erkannt als Bauprotokoll)

**Wurzel des Problems:** Der Wizard suggeriert dem User positionsgenaue Erkennung ("Klick auf Segment 1 → Plantyp = Bauprotokoll"). Im Code wird aber nur `fileName.Contains("PROT")` aufgerufen — die Position-Information geht beim Speichern verloren.

## Vorschlag

Eine neue Methode `"segment"` einführen, die Position + Wert prüft:

```csharp
public class RecognitionRule
{
    public string Method { get; set; } = "segment";       // Default ändert sich
    public string Pattern { get; set; } = string.Empty;
    public int? SegmentPosition { get; set; }             // NEU, nullable
}
```

**Recognizer-Logik (Pseudocode):**
```
1. Dateinamen-Stem bilden (ohne Extension)
2. An Profil-Delimitern splitten → tokens
3. Wenn rule.SegmentPosition nicht gesetzt → false + Warning
4. Wenn SegmentPosition außerhalb der Token-Anzahl → false
5. Return: tokens[SegmentPosition].Equals(rule.Pattern, IgnoreCase)
```

**AND-Semantik bei Multi-Rules:** Alle Rules eines Profils müssen matchen.

**`regex` bleibt** als Fallback für Spezialfälle (Statik-Nummernkreise, Dateien ohne saubere Delimiter).

**`prefix` und `contains`** werden komplett entfernt (keine Legacy-Toleranz, da das Profil-System noch jung und überschaubar ist).

## Fragen an dich

1. **Datenmodell:** Ist `SegmentPosition: int?` als nullable Feld die richtige Wahl, oder würdest du es anders modellieren (z.B. als Sub-Type, Discriminated Union, separates `SegmentRule`)?

2. **Default-Methode:** Soll `"segment"` der Default sein, auch wenn das bedeutet dass alte Profile beim Laden auf das neue Schema gemappt werden müssen?

3. **Migration:** Wir denken über zwei Wege nach:
   - **Hart:** prefix/contains rauswerfen, alte Profile abweisen
   - **Weich:** Best-Effort-Konvertierung beim Laden (`prefix` mit Pattern X → `segment` mit Position 0 + Pattern X)

   Was ist dein Bauchgefühl?

4. **AND-Semantik:** Reicht es, dass alle Rules matchen müssen, oder brauchen wir explizite Verknüpfungs-Operatoren (AND/OR)?

5. **Test-Strategie:** Welche Test-Klassen würdest du als Pflicht ansehen?

6. **ADR-Struktur:** ADR-010 dokumentiert das Recognition-System aktuell. Würdest du ADR-010 erweitern, oder einen neuen ADR (z.B. ADR-050) für die `segment`-Methode anlegen?

Bitte gib eine ausführliche Bewertung. Markiere am Ende:
- ✅ Worin du klar zustimmst
- ⚠️ Worin du Bedenken hast
- ❓ Was du noch geklärt haben willst

Canvas-Titel: **"Review Runde 1 — Segment-basierte Plantyp-Erkennung"**

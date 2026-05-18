# Claude-Analyse — Review Runde 3

**Datum:** 2026-05-18

---

## Gesamtbewertung

**Sign-off erreicht.** ChatGPT gibt grünes Licht für Commit 1.

Zusätzlich liefert Runde 3 noch wertvolle Detail-Verfeinerungen:

### Wertvolle Ergänzungen

**4 zusätzliche Akzeptanzkriterien (14–17):** Decken Lücken in den 13 Punkten:
- **#14 Immutable Keys** — `id` UND `token_key` nie änderbar (explizit als Invariante)
- **#15 Built-in-Rollen unveränderlich** — `semantic_role` seed-definiert, im Manager read-only; Custom = NULL
- **#16 Strict Reset für PatternTemplates** — Same Pattern wie Profile
- **#17 Health-Gating vor Auto-Import** — Recognizer darf matchen, Auto-Import blockiert bei Missing-ID in Identity/Hierarchy/Rename/IndexExtraction

Alle 4 sind klar formuliert und gehören in die BPM-108-Beschreibung.

**Präzisierte Mockup-Texte für Manager-Dialog Rollenanzeige:**
- Spatial: „Wird automatisch Teil der Dokument-Identität, **wenn dieser Segmenttyp einem Profilsegment zugewiesen ist**." — wichtige Klarstellung dass Built-in allein noch keine Identity-Wirkung hat
- PlanNumber: „Genau ein Segment mit dieser Rolle ist pro Profil erforderlich."
- PlanIndex: „Erforderlich, wenn IndexSource = Aus Dateiname gewählt ist."

**3 zusätzliche Popover-Zustände für „+ Eigenes":**
1. Validierungsfehler bei leerem Name („Name ist erforderlich.")
2. Token-Vorschau (read-only) — erklärt früh warum spätere Renames Token nicht ändern
3. Direkt-zuweisen-Hinweis wenn Segment aktiv

### Eine offene optionale Frage

ChatGPT empfiehlt: Token-Key als read-only Vorschau im Inline-Popover anzeigen.

Vorteile:
- User versteht sofort warum Rename später den Token nicht ändert
- Transparenz: User weiß welchen Token er in Templates verwenden kann
- Kein zusätzlicher Klick, weil Token live mit Nameingabe generiert wird

Nachteile:
- Popover wird etwas voller (eine weitere Zeile)
- Power-User-Konzept: viele User werden Token gar nicht verwenden

**Meine klare Empfehlung:** Ja, anzeigen. Pflanzt Verständnis früh und verhindert Confusion bei späterer Template-Verwendung.

## Status nach Runde 3

Review-Serie kann abgeschlossen werden. Nächste Schritte:

1. **CGR-Serie auf „Abgeschlossen" setzen** (README + INDEX.md)
2. **BPM-108 ClickUp-Description updaten** mit:
   - 3-Phasen-Plan A/B/C
   - 17 Akzeptanzkriterien
   - 3 erste Commit-Messages
3. **Mockup-Anpassungen:**
   - `04_FeldtypManager/01_Uebersicht.html`: Rollenanzeige bei Built-ins ergänzen (3 Varianten Spatial/PlanNumber/PlanIndex)
   - Neues Mockup `03_ProfilWizard/02_Segmente_NeuerCustom.html`: Inline-Popover mit Token-Vorschau + 3 Zuständen
4. **Pause / nächste Session:** Commit 1 (Phase A Domain/Persistence)

# User-Entscheidungen — Review Runde 2

**Datum:** 2026-05-18
**User:** Herbert

---

## Antworten auf ChatGPTs 4 Rückfragen

Alle 4 Antworten folgen Claudes Empfehlung (Konsens aus Analyse + ChatGPT-Vorschlag).

### 1. Spatial-Rollen für `objekt` und `achse`?

**Entscheidung: Beide als `Spatial`.**

Konservativer Default. Achse ist in den meisten Fällen räumlich, Objekt mehrdeutig — wenn User es nicht identitätsbildend will, verwendet er es einfach nicht im Profil.

### 2. `token_key` unveränderlich nach Anlage?

**Entscheidung: JA — auch bei Custom-Rename.**

Templates wie `{plan_number}-{akustik_klasse}` würden sonst brechen. Name darf frei umbenannt werden, Token bleibt stabil. Token-Wechsel ist bewusst nicht in BPM-108.

### 3. DevTool: archivieren statt löschen?

**Entscheidung: Archivieren.**

`<project>/.bpm/profiles/_archiv/schema-reset-YYYYMMDD-HHMMSS/`. Manuelles Löschen durch User später möglich, Rollback bei Fehler. Frühphase ≠ Datenverlust riskieren.

### 4. PatternTemplateService weiterverwenden oder deaktivieren?

**Entscheidung: Deaktivieren bis v4-Anpassung.**

- `pattern-templates.json` wird beim BPM-108-Release nach `_archiv/` verschoben (genau wie alte Profile)
- Service bleibt im Code, schreibt/liest aber nur v4-konforme Templates
- Keine v4-Templates initial: UI zeigt „keine Vorschläge"
- User erstellt 1-2 Pattern-Templates neu im Wizard → automatisch v4-konform

---

## Status

- **Runde 2: abgeschlossen** mit klaren Antworten zu allen 4 Detailfragen
- **Runde 3 startet:** Folgeprompt mit Antworten + Bitte um finale Bestätigung der Architektur

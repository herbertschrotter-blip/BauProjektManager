# Review Runde 3 — Abschluss-Bestätigung BPM-108

## Rolle
Senior Software Architect, Review-Gespräch mit Claude/Anthropic über BPM-108 Segmenttyp-Architektur.

## Gesprächsformat
- Direkt zu deinem Kollegen, NICHT zum User
- **CANVAS-TITEL: "Review Runde 3"**
- Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff
- **Repo:** `herbertschrotter-blip/BauProjektManager` (per deiner Korrektur aus Runde 2)
- **Branch:** `feature/planmanager-v1` — IMMER diesen Branch verwenden, NICHT `main`!

## Gesprächsregeln
- Ehrlich und kritisch
- Kurz halten — diese Runde ist Abschluss-Bestätigung, kein erneutes Auseinandernehmen
- Wenn alles passt: kurz „grünes Licht" geben

---

## Stand nach Runde 2

Die Architektur ist scharfgestellt. Wir übernehmen alle deine Korrekturen:

| Punkt | Übernommen |
|---|---|
| `user_modified_group`-Flag zusätzlich | ✓ |
| `token_key` bei Built-ins unveränderlich | ✓ |
| `semantic_role` read-only Anzeige im Manager mit Warntext bei Built-ins | ✓ |
| `PlanNumber == 1`-Validierung in Schritt 2 | ✓ |
| `identityFields` nur tatsächlich verwendete Profilsegmente | ✓ |
| `is_required` nur aus Rolle abgeleitet, nicht user-toggle | ✓ |
| `token_key` Custom stabil nach Anlage | ✓ |
| 13 Refactor-Stellen als BPM-108-Akzeptanzkriterien | ✓ |
| 3-Phasen-Plan A/B/C + 3 erste Commits | ✓ |
| Kein `lastKnownLabel`, keine Massenreparatur in Schritt 5 | ✓ |
| Custom-Chip Inline-Schnellanlage, kein Manager-Wechsel | ✓ |
| Deaktivierte Typen verborgen + Kontextbanner | ✓ |

---

## Antworten auf deine 4 Rückfragen aus Runde 2

### 1. Spatial für `objekt` und `achse`?

**Ja, beide als `Spatial`.** Wir folgen deinem konservativen Vorschlag. Achse ist überwiegend räumlich; Objekt mehrdeutig, aber wenn User nicht identitätsbildend wünscht, verwendet er es einfach nicht im Profil. Built-in-Rolle kann später bei Bedarf entfernt werden, das ist günstiger als zu wenig Identity zu haben.

Finale Spatial-Liste:
```text
geschoss, haus, bauteil, bauabschnitt, stiege, zone, block, achse, objekt
```

`planart` bleibt `None`.

### 2. `token_key` nach Anlage unveränderlich, auch bei Custom-Rename?

**Ja, unveränderlich.** Name kann frei umbenannt werden, Token bleibt stabil. Templates wie `{plan_number}-{akustik_klasse}` würden sonst brechen. Token-Umbenennung ist bewusst nicht Teil von BPM-108 — wenn jemand das wirklich braucht, kommt es später als expliziter „Token umbenennen mit Profil-Update"-Flow.

### 3. DevTool: archivieren statt löschen?

**Ja, archivieren.** Frühphase ≠ Datenverlust riskieren. Profile mit `schemaVersion != 4` werden beim `ArchiveOutdatedProfiles`-DevTool-Befehl nach `<project>/.bpm/profiles/_archiv/schema-reset-YYYYMMDD-HHMMSS/` verschoben. User kann später manuell löschen, Rollback bei Fehler möglich. Normaler `ProfileManager.Load` bleibt strikt: `schemaVersion != 4` → verwerfen + Fehlermeldung.

### 4. PatternTemplateService weiterverwenden oder deaktivieren?

**Deaktivieren bis v4-Anpassung.**

Konkret:
- `pattern-templates.json` wird beim BPM-108-Release nach `_archiv/` verschoben (analog zu alten Profilen)
- Service bleibt im Code, schreibt/liest nur v4-konforme Templates
- Keine v4-Templates initial: UI zeigt „keine Vorschläge"
- User erstellt 1-2 Pattern-Templates neu im Wizard → automatisch v4-konform

Pseudocode-Check beim Load:

```csharp
if (template.SchemaVersion != 4) {
    Log.Warning("PatternTemplate verworfen: {File} — SchemaVersion {Version}, erwartet 4.", file, template.SchemaVersion);
    continue;
}
```

Das entspricht dem gleichen Strict-Loader-Pattern wie bei Profilen.

---

## Geplante Mockup-Ergänzungen vor Implementierung

Bevor Commit 1 startet, ergänzen wir noch zwei Mockup-Aspekte. Bitte gib kurzes Feedback ob du Lücken siehst:

### A. Manager-Dialog: read-only Rollenanzeige bei Built-ins

Im aktuellen Mockup (`Docs/Mockups/PlanManager/04_FeldtypManager/01_Uebersicht.html`) gibt es keinen Hinweis auf die `semantic_role`. Wir ergänzen im Edit-Panel bei Built-ins:

```text
Fachrolle: Räumlich (read-only)
ℹ Dieser Segmenttyp wird automatisch in die Dokument-Identität aufgenommen.
```

Bei Custom-Typen: gar keine Rollen-Anzeige (weil `semantic_role = NULL`).

### B. Neues Mockup: Inline-Popover „+ Eigenes" im Wizard Schritt 2

Aktuell ist der „+ Eigenes"-Chip funktionslos. Wir bauen ein kleines Popover (kein Modal-Dialog), das beim Klick erscheint:

```text
┌───────────────────────────────┐
│ Neuer Segmenttyp              │
│ Name: [_______________]       │
│ Farbe: ●●●●●●●●●●●●           │
│ [Abbrechen] [Anlegen]         │
└───────────────────────────────┘

Hinweis: Gruppe und Sortierung
im Segmenttypen-Manager.
```

Ergebnis: Neuer Custom-Typ landet in Default-Gruppe „Eigene" + Chip erscheint sofort im Wizard.

---

## Was ich von dir brauche

**Kurz und knapp:**

1. **Grünes Licht** für die 4 Antworten, oder hast du noch Einwände?
2. **Mockup-Ergänzungen A + B** sinnvoll oder fehlt was Wichtiges?
3. **Finale Sicht:** Soll ich noch etwas in BPM-108 ClickUp-Description aufnehmen, das du als Akzeptanzkriterium siehst, was bisher noch nicht in den 13 Punkten steht?

Wenn nichts mehr offen ist: ein klares „grünes Licht, starte Commit 1" reicht. Kein neuer Code, keine neuen Konzepte. Diese Runde ist Sign-off.

# Review Runde 3 — ChatGPT-Antwort

**Modell:** GPT-5.4
**Datum:** 2026-05-18
**Canvas-Titel:** Review Runde 3

---

Kollege, grünes Licht. Die vier Antworten sind konsistent mit dem bisher geschärften Modell und schließen die offenen Architekturfragen sauber ab.

## 1. Sign-off zu den 4 Antworten

### 1.1 `objekt` und `achse` als `Spatial`

Einverstanden. Das ist die konservativ richtige Entscheidung. Mehr Identity-Differenzierung ist im PlanManager weniger riskant als zu wenig Differenzierung. Wenn ein Profil `objekt` oder `achse` nicht braucht, weist der User diese Typen einfach nicht zu. Damit landen sie auch nicht in `identityFields`, weil nur tatsächlich verwendete Profilsegmente berücksichtigt werden.

### 1.2 `token_key` unveränderlich

Einverstanden. Das muss als harte Invariante in BPM-108 stehen:

```text
token_key ist nach Anlage immutable — auch bei Custom-Rename.
```

Name ist Anzeige. Token ist Template-Vertrag.

### 1.3 DevTool archiviert alte Profile

Einverstanden. Archivieren statt Löschen ist die richtige Frühphasen-Reset-Variante. Wichtig bleibt: Das ist ein expliziter DevTool-/Setup-Befehl, kein automatischer Loader-Side-Effect.

Normaler Loader:
```text
schemaVersion != 4 → verwerfen + klare Fehlermeldung
```

DevTool:
```text
schemaVersion != 4 → nach _archiv/schema-reset-YYYYMMDD-HHMMSS/ verschieben
```

### 1.4 `PatternTemplateService` bis v4-Anpassung deaktivieren

Einverstanden. Das ist sauberer als temporäre Kompatibilität. Gleiches Strict-Pattern wie bei Profilen ist richtig:

```csharp
if (template.SchemaVersion != 4)
{
    Log.Warning("PatternTemplate verworfen: {File} — SchemaVersion {Version}, erwartet 4.", file, template.SchemaVersion);
    continue;
}
```

Zusätzlich in BPM-108 aufnehmen: `pattern-templates.json` wird über DevTool/Setup archiviert, nicht still migriert.

## 2. Mockup-Ergänzungen A + B

### A. Manager-Dialog: read-only Rollenanzeige bei Built-ins

Sinnvoll und ausreichend. Ich würde den Text minimal präzisieren, damit klar ist, dass nicht jeder Built-in automatisch in die Identity geht, sondern nur wenn er im Profil verwendet wird:

```text
Fachrolle: Räumlich (read-only)
ℹ Wird automatisch Teil der Dokument-Identität, wenn dieser Segmenttyp einem Profilsegment zugewiesen ist.
```

Bei `PlanNumber`:
```text
Fachrolle: Plannummer (read-only)
ℹ Genau ein Segment mit dieser Rolle ist pro Profil erforderlich.
```

Bei `PlanIndex`:
```text
Fachrolle: Index (read-only)
ℹ Erforderlich, wenn IndexSource = Aus Dateiname gewählt ist.
```

Bei Custom: keine Rollenanzeige. Das passt.

### B. Inline-Popover „+ Eigenes" in Wizard Schritt 2

Sinnvoll. Das ist der richtige UX-Schnitt: schnell im Wizard anlegen, Details später im Manager.

Ich würde im Mockup noch drei kleine Zustände ergänzen:

1. **Validierungsfehler leerer Name**
```text
Name ist erforderlich.
```

2. **Token-Hinweis nach Nameingabe**
```text
Token: akustik_klasse
```
Nicht editierbar, nur Vorschau. Das erklärt früh, warum Rename später den Token nicht ändert.

3. **Direkt-zuweisen-Verhalten**

Wenn gerade ein Dateisegment aktiv/selektiert ist:
```text
Nach dem Anlegen wird der neue Segmenttyp dem ausgewählten Segment zugewiesen.
```

Wenn kein Segment aktiv ist: nur Chip hinzufügen.

Diese drei Ergänzungen verhindern spätere UX-Unklarheiten ohne das Popover aufzublähen.

## 3. Letzte Akzeptanzkriterien für BPM-108

Die 13 Punkte aus Runde 2 reichen fast. Ich würde noch diese expliziten Akzeptanzkriterien aufnehmen, damit bei der Umsetzung nichts zwischen Phase A/B/C verloren geht:

**Ergänzung 14 — Immutable Keys**
```text
segment_types.id und segment_types.token_key sind nach Anlage unveränderlich.
Rename ändert nur name, nie id/token_key.
```

**Ergänzung 15 — Built-in-Rollen unveränderlich**
```text
semantic_role ist bei Built-ins seed-definiert und im Manager read-only.
Custom-Segmenttypen haben immer semantic_role = NULL.
```

**Ergänzung 16 — Strict Reset für PatternTemplates**
```text
pattern-templates.json wird nicht migriert. Nur schemaVersion 4 wird geladen; alte Templates werden per explizitem DevTool/Setup-Befehl archiviert.
```

**Ergänzung 17 — Health-Gating vor Auto-Import**
```text
Ein Profil mit MissingSegmentTypes darf zwar angezeigt und vom Recognizer grundsätzlich gematcht werden, blockiert aber Auto-Import, sobald die fehlende ID in identityFields, folderHierarchy, renameSchema oder indexExtraction verwendet wird.
```

Das ist wichtig genug, um als eigenes Kriterium sichtbar zu sein.

## 4. Finale Sicht

Die Architektur ist jetzt sign-off-fähig.

Die Implementierungsreihenfolge bleibt:
1. Commit 1: Segment type catalog persistence.
2. Commit 2: Profile schema v4 with `fieldTypeId`.
3. Commit 3: Wizard uses segment type catalog.

Danach Manager-Dialog und DevTool-Archivierung als eigene Commits. Nicht alles in Commit 1 packen.

**Grünes Licht, starte Commit 1.**

## ✅ Einigkeit

* `objekt` und `achse` werden `Spatial`.
* `token_key` ist immutable, auch bei Custom-Rename.
* Alte Profile/Templates werden explizit archiviert, nicht migriert.
* `PatternTemplateService` akzeptiert nur v4-konforme Templates.
* Manager zeigt Built-in-Rollen read-only mit Erklärung.
* Custom-Anlage erfolgt inline im Wizard über kleines Popover.

## ⚠️ Widerspruch

* Kein automatisches Archivieren alter Profile/Templates beim normalen App-Start.
* Keine editierbaren `token_key`s in BPM-108.
* Keine Rollenanzeige oder Rollenauswahl für Custom-Typen.

## ❓ Rückfragen

Keine blockernden Rückfragen mehr. Optional für das Mockup: Soll das Inline-Popover den generierten `token_key` als read-only Vorschau anzeigen? Meine Empfehlung: ja.

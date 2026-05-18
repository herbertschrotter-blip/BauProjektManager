# Review Runde 2 — BPM-108 Folge-Prompt

## Rolle
Du bist ein erfahrener Senior Software Architect und führst ein technisches Review-Gespräch mit einem Kollegen (Claude/Anthropic) über die geplante Segmenttyp-Architektur in BPM-108.

## Gesprächsformat
- Sprich direkt zu deinem Kollegen, NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- **CANVAS-TITEL: "Review Runde 2"**
- Fasse am Ende JEDER Antwort zusammen:
  ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** SchrotterHerbert/BauProjektManager
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren und Originaldateien zu lesen.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!

## Gesprächsregeln
- Ehrlich und kritisch
- Probleme konkret benennen
- Verbesserungen mit Code/Pseudocode zeigen
- Rückfragen bei fehlendem Kontext
- Fokus halten, keine allgemeinen Exkurse
- Kompakt, Code nur wenn nötig

---

## Was bisher geschah (Runde 1)

### Deine wichtigsten Korrekturen (vollständig akzeptiert)

1. **`fieldTypeId` gehört NICHT in `RecognitionRule`.** BPM-082-Recognizer arbeitet ausschließlich über `method` / `pattern` / `segmentPosition`. Recognition bleibt unverändert.
2. **`FieldType` ist mehr als Display-Katalog.** Wizard-Validierung (PlanNumber-Pflicht), Identity-Trigger, Hierarchie-Auswahl, Variable-Segment-Heuristik hängen am Enum.
3. **Zwei-Schichten-Modell:** `fieldTypeId` (persistente Referenz) + `SemanticRole` (kleine Enum für fachliche Sonderfälle: PlanNumber, PlanIndex, Date, Spatial, Description, Ignore). Custom-Typen haben `SemanticRole = null`.
4. **Schema v4 + Frühphasen-Reset** statt Migration. `ProfileManager.Load` strikt auf `schemaVersion == 4`.
5. **`token_key` für Templates** (snake_case, stabil) zusätzlich zur `id` (ULID/snake_case). Custom-Typen bekommen aus Name generierten `token_key` mit Suffix-Logik.
6. **ProfileHealth-Marker** für Missing-IDs statt Hardfail. Auto-Import-Blockade wenn Missing in `identityFields` / `folderHierarchy` / `renameSchema`.
7. **Sync-Reihenfolge topologisch:** Groups → Types → Profiles. Soft-Delete bleibt referenzierbar.
8. **Custom-Duplikate nicht auto-mergen.** ULID-Identität schlägt Name-Identität.
9. **DSGVO Klasse A** für `SegmentTypeDto`. Whitelist-Eintrag in ADR-053 Whitelist.
10. **Schichten-Schnitt A/B/C** (Domain/Persistence → Profilformat v4 → Wizard/UI) übernommen.

---

## User-Entscheidungen zu deinen 5 Rückfragen

### Frage 1: Built-in umbenennbar/farbänderbar?
**Antwort: JA — voll editierbar wie Custom.**

Built-ins sind nicht read-only. User darf z.B. „Plannummer" in „Plan-Nr." umbenennen und Farbe anpassen.

**Konsequenz für DB-Schema:** Wir nehmen deine vorgeschlagene Variante mit User-Ownership-Flags:

```sql
ALTER TABLE segment_types ADD COLUMN builtin_version INTEGER NOT NULL DEFAULT 1;
ALTER TABLE segment_types ADD COLUMN user_modified_name INTEGER NOT NULL DEFAULT 0;
ALTER TABLE segment_types ADD COLUMN user_modified_color INTEGER NOT NULL DEFAULT 0;
ALTER TABLE segment_types ADD COLUMN user_modified_sort INTEGER NOT NULL DEFAULT 0;
ALTER TABLE segment_types ADD COLUMN user_modified_active INTEGER NOT NULL DEFAULT 0;
```

Seed-on-start aktualisiert nur Felder mit `user_modified_<feld> = 0`.

### Frage 2: Custom SemanticRole wählbar?
**Antwort: REIN DEKORATIV — keine SemanticRole für Custom.**

Custom-Typen haben **immer** `SemanticRole = null`. Wenn ein User räumliche Klassifikation braucht, muss er einen Built-in nutzen (Geschoss/Haus/Bauteil/Bauabschnitt/Stiege/Zone/Block) oder Built-in umbenennen.

**Konsequenz:** Manager-Dialog zeigt **kein** SemanticRole-Dropdown für Custom-Einträge. Bei Built-ins ist die Rolle als read-only-Info sichtbar.

### Frage 3: identityFields UI-geführt?
**Vorschlag:** Implizit aus SemanticRole — kein UI-Override in Frühphase.

`identityFields` wird beim Profil-Speichern automatisch berechnet:
```text
identityFields = [
  "documentType",                              // System-Key, immer dabei
  ...segments mit SemanticRole == PlanNumber,
  ...segments mit SemanticRole == Spatial
].map(s => s.fieldTypeId)
```

**Frage an dich:** Reicht das, oder gibt es Edge-Cases (z.B. zwei Spatial-Segmente in einem Profil — Geschoss + Bauteil), die zu false-positive-Identity-Kollisionen führen? Brauchen wir doch ein UI-Häkchen?

### Frage 4: `recognition_profiles`-Tabelle für ADR-053?
**Vorschlag:** Nicht in BPM-108. Separates Task in der ADR-053-Sync-Implementierung.

BPM-108 fokussiert auf `segment_types` + `segment_type_groups`. JSON-Profile bleiben als Dateien unter `<project>/.bpm/profiles/`.

**Frage an dich:** Schaffen wir uns damit ein Sync-Problem auf der ADR-053-Seite? Müssen wir Profile-DB-Migration vorab denken (Vorbereitung in BPM-108) oder ist „später" sauber?

### Frage 5: bpm.db Komplett-Reset?
**Vorschlag:** Ja — Frühphase, keine Produktivdaten.

Beim BPM-108-Release wird die App-DB (`bpm.db`) komplett gelöscht und neu aufgebaut, inklusive Settings. User-Hinweis im Release-Note. Profile-JSONs in den Projektordnern werden vom User manuell gelöscht (oder via Setup-Skript).

**Frage an dich:** Sollten wir trotzdem einen kurzen Migrations-Code-Block für die Profile haben — zum Beispiel ein Lösch-Skript, das alle `<project>/.bpm/profiles/*.json` mit `schemaVersion != 4` automatisch in einen `_archiv/`-Unterordner verschiebt? Oder strikt „User muss selbst aufräumen"?

---

## Offene Details aus meiner Analyse

Diese Punkte hast du in Runde 1 nicht oder nur kurz angerissen. Ich brauche deine Einschätzung.

### A. `lastKnownLabel` im Profil-JSON — ja oder nein?

Du hast es als optionalen Komfort vorgeschlagen für Missing-ID-Rendering. Ich tendiere zu **NEIN** in Frühphase:

**Gegen lastKnownLabel:**
- Doppelte Source of Truth (DB + JSON-Snapshot)
- Profile-JSON wird länger / unübersichtlicher
- „Unbekannt (`<id>`)" reicht als UI-Signal — User sieht klar: hier fehlt was, geh in den Manager
- Wenn Sync den fehlenden Segmenttyp nachträglich liefert, ist Rendering wieder korrekt — Snapshot wäre unnötig

**Für lastKnownLabel:**
- User-Hilfe bei „warum heißt das jetzt Unbekannt"
- Funktioniert auch ohne Sync

**Frage an dich:** Welche Variante in Frühphase? Falls JA: speichern wir nur bei explizitem User-Edit im Wizard, oder bei jedem Save?

### B. Custom-Chip „+ Eigenes" UI-Flow im Wizard Schritt 2

Im aktuellen Mockup ([Docs/Mockups/PlanManager/03_ProfilWizard/02_Segmente.html](Docs/Mockups/PlanManager/03_ProfilWizard/02_Segmente.html)) existiert ein Custom-Chip mit Stricheltlinie und Plus-Symbol. Aktueller Stand: funktionslos.

Mit BPM-108 sind zwei Varianten denkbar:

**Variante 1: Inline-Anlage**
- Klick auf „+ Eigenes" → InputBox „Name?" + Farbpalette inline im Wizard → Speichern → neuer Chip erscheint sofort
- Schnell, ohne Dialog-Wechsel
- Custom landet automatisch in Default-Gruppe „Sonstiges"

**Variante 2: Modal Manager-Dialog**
- Klick auf „+ Eigenes" → öffnet den Manager-Dialog mit vorausgewähltem „Neuer Custom-Typ"-Modus
- Voller Funktionsumfang (Gruppe wählen, Sortierung)
- Aber: Unterbricht den Wizard-Flow

**Frage an dich:** Welche Variante? Hast du eine dritte? Mein Bauchgefühl: Variante 1 für Schnellanlage + Hinweis „Im Manager kannst du Gruppe ändern".

### C. Wizard Schritt 5 (Indizes verwalten) — was muss BPM-108 dort tun?

Schritt 5 ist noch nicht implementiert. Aktueller Plan: Anzeige der erkannten Indizes mit Reopen-Möglichkeit.

Mit BPM-108: Wenn beim Reopen ein Segment auf einen deaktivierten/gelöschten Typ verweist, muss das Chip mit Badge gerendert werden (siehe deine `SegmentTypeDisplay`). Save-Block bei Missing-ID nur, wenn fachlich relevant.

**Frage an dich:** Übersehen wir etwas? Sollte Schritt 5 z.B. eine „Profilen reparieren"-Aktion bekommen für Massenoperationen (alle Profile auf einmal anschauen, die Missing-IDs haben)?

### D. Migration der bestehenden Domain-Konstanten (Phase B Pflicht-Bestandteil)

Aktuell hartcodiert in der Code-Basis:

1. **`BuildFieldTypeOptions()`** in `ProfileWizardViewModel.cs` — erzeugt fixe Liste aus `FieldType`-Enum
2. **`BuildFromWizard()`** mit `Required = PlanNumber` / `IncludeInIdentity = PlanNumber|Haus|Bauteil|Bauabschnitt`
3. **`ValidateStep3()`** mit Variable-Segment-Heuristik (`PlanNumber`/`PlanIndex`/`Date`)
4. **`ValidateStep4()`** mit Hierarchie-Whitelist (`Geschoss`/`Haus`/`Bauteil`/`Bauabschnitt`/`Stiege`/`Zone`/`Block`)
5. **`Colors.xaml`** — 8 hartcodierte Field-Type-Farb-Tokens (BpmFieldPlanNumber etc.)

Alle 5 Stellen werden auf `ISegmentTypeCatalog` + `SemanticRole` umgestellt. `Colors.xaml`-Tokens entfallen, Farben kommen direkt aus DB.

**Frage an dich:** Übersiehst du noch eine Stelle, die ich aus Runde 1 mitnehmen sollte? Sollte ich ein konkretes Refactor-Schema in den BPM-108-Task setzen oder bleibt das offen?

### E. Wizard Schritt 2 — Chip-Rendering bei deaktivierten Typen

Du hast `SegmentTypeDisplay` mit `IsActive` / `IsDeleted` / `Exists` vorgeschlagen.

Konkretes Szenario: User öffnet bestehendes Profil, dessen Segment `Position 4` auf den Custom-Typ „Akustik-Klasse" verweist. Dieser ist zwischenzeitlich deaktiviert worden.

**Mein Vorschlag:**
- Im Beispiel-Block oben (das Datei-Token-Visual) wird das Segment normal farbig + Badge „deaktiviert" gerendert
- In der unteren Chip-Liste (Drag-Quellen) erscheint „Akustik-Klasse" NICHT (gefiltert über `GetEffectiveActive`)
- User kann das Segment per Rechtsklick → Reset entfernen oder einem aktiven Typ neu zuweisen

**Frage an dich:** Sollte die deaktivierte Quelle trotzdem mit grauem Stil in der Liste sichtbar bleiben, damit der User versteht „diesen Typ gibt es noch, ist nur deaktiviert"? Oder ist „verborgen" besser?

---

## Aufgabe

Validiere die User-Entscheidungen und meine Vorschläge zu den offenen Details. Konkret:

1. **Sind die User-Entscheidungen zu Frage 1 + 2 (Built-in editierbar, Custom rein dekorativ) konsistent mit dem Gesamtkonzept?** Siehst du Folgeprobleme?
2. **Beantworte meine Rückfragen zu Frage 3 / 4 / 5** (identityFields-Edge-Cases, recognition_profiles-Vorbereitung, Profile-Lösch-Skript).
3. **Beantworte die 5 offenen Detail-Punkte A–E** (lastKnownLabel, Custom-Chip UI, Schritt 5, Domain-Konstanten-Migration, Chip-Rendering bei deaktivierten Typen).
4. **Wenn du zufrieden bist:** gib einen finalen, schnittfesten BPM-108-Implementierungsleitfaden für Phase A / B / C (kurz, kein Roman) und nenne klar die ersten 3 Commits, mit denen ich starten sollte.
5. **Wenn nicht zufrieden:** sag mir wo es noch hakt.

Schreibe alles ins Canvas „Review Runde 2".

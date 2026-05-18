# Claude-Analyse — Review Runde 1

**Datum:** 2026-05-18
**Branch:** feature/planmanager-v1

---

## Gesamtbewertung

ChatGPT hat eine **kritische Lücke** in unserem Konzept aufgedeckt, die im Initial-Prompt nicht klar war: `FieldType` wird im aktuellen Code für **mehr** als nur UI-Anzeige verwendet — er ist Pflicht-Validator, Identity-Trigger, Hierarchie-Trigger, Variable-Segment-Heuristik und Template-Token gleichzeitig. Unser BPM-108-Vorschlag „ersetze FieldType durch frei definierbare DB-Einträge" hätte diese Logik in String-Vergleiche zerlegt oder ganz verloren.

**Lösung von ChatGPT ist überzeugend:** Zwei-Schichten-Modell

1. **`fieldTypeId`** (persistente Referenz, ULID/snake_case)
2. **`SemanticRole`** (kleine Enum für fachliche Sonderfälle: PlanNumber, PlanIndex, Date, Spatial, Description, Ignore)

Das löst gleichzeitig zwei Probleme:
- Custom-Typen sind frei wählbar (keine Enum-Erweiterung nötig)
- Built-in-Spezialsemantik (Pflicht, Identity, Hierarchie) bleibt deterministisch ableitbar

## Bewertung pro Punkt

### ✅ Volle Zustimmung

| Punkt | Bewertung |
|---|---|
| **SemanticRole-Trennung** | Knackpunkt. Ohne sie wird System fragil. |
| **Schema v4 + Reset** | Konform zur Frühphasen-Regel. Keine Migration bauen. |
| **RecognitionRule unverändert** | Mein Initial-Vorschlag mit `fieldTypeId` in `recognition[]` war falsch. BPM-082 braucht das nicht. |
| **token_key für Templates** | Custom-ULIDs (z.B. `01H...`) sind nicht template-tauglich. Built-ins haben fixe IDs (`plan_number`), Custom bekommen aus Name generierten `token_key` mit Suffix-Logik. |
| **ProfileHealth + Save-Block** | Soft-Skip mit unhealthy-Marker + Auto-Import-Blockade ist robust gegen Sync-Lag. |
| **Sync-Reihenfolge topologisch** | Groups → Types → Profiles. Selbstverständlich, aber explizit dokumentieren. |
| **DSGVO Klasse A** | Korrekt, da reine UI-Konfiguration. |
| **Custom-Duplikate nicht auto-mergen** | ULID-Identität schlägt Name-Identität. Merge ist späterer expliziter UI-Flow. |
| **Schichten-Schnitt A/B/C** | Domain → Profilformat → UI-Wizard. Klare BPM-108-Implementierungsreihenfolge. |

### ⚖️ Mittelweg / Diskussion

**Built-in-Update-Policy (Punkt 6)**

ChatGPT schlägt zwei Varianten vor:
- **Aufwendig:** 4 `user_modified_*`-Flags pro Built-in
- **Hart-Frühphase:** Default-Update überschreibt alles außer `is_active`

Mein Eindruck: Die harte Variante ist Frühphasen-konform und reicht für V1. Die 4 Flags machen das DB-Schema komplexer als nötig. Wenn der User später Built-ins umbenennen will, kann man die Flags nachträglich einführen — heute ist „Built-in-Name/Farbe/Sortierung kommt vom Code" der saubere Schnitt.

Empfehlung: **Harte Variante** — Built-ins sind read-only für Name/Farbe/Sortierung, nur `is_active` ist user-toggle. Custom-Typen sind voll editierbar.

Das beantwortet auch indirekt **Rückfrage 1**: Built-ins nur deaktivierbar/sortierbar (eigentlich nur deaktivierbar, da Sortierung über Built-in-Default kommt).

**`lastKnownLabel` im Profil-JSON (Punkt 11)**

Schneller Komfort für Missing-ID-Rendering — kostet aber ein Feld pro Segment und doppelt die Source of Truth (DB + JSON-Fallback). In Frühphase könnte man auch `Unbekannt (<id>)` als Fallback rendern und damit fertig sein. User sieht klar: hier fehlt was, geh in den Manager.

Empfehlung: **Erstmal weglassen.** `Unbekannt (<id>)` ist kommunikativ klar genug. `lastKnownLabel` kann nachträglich kommen, wenn User-Feedback es einfordert.

### 🆕 Was ChatGPT nicht voll abgedeckt hat

**1. Wizard Schritt 2 UX bei deaktivierten Typen**

Die `EffectiveActive`-Liste filtert deaktivierte Typen für die Chip-Auswahl raus. Aber wenn der User ein **bestehendes** Profil öffnet und ein Segment ist mit einem inzwischen deaktivierten Typ zugewiesen, muss der Chip noch farbig dargestellt werden — er darf nur nicht als **neuer** Drag-Quell-Chip verfügbar sein. Das ist konsistent mit ChatGPTs `SegmentTypeDisplay`-Record (exists+inactive: rendern + Badge), nur explizit auf den Wizard angewendet.

**2. Custom-Chip „+ Eigenes" UI-Flow**

ChatGPT geht nicht darauf ein. Aktueller Stand: Das Chip ist sichtbar (Mockup) aber funktionslos. Mit BPM-108 wird es zu:
- Klick → InputBox „Name?" → Farbpalette → optional SemanticRole → in DB persistieren → neuer Chip erscheint sofort
- Custom-Chip bleibt am Ende der Liste (oder in einer „Custom"-Gruppe)

**3. ProfileWizard Schritt 5 (Indizes verwalten)**

Schritt 5 ist noch nicht implementiert und benutzt aktuell nur `PlanNumber`/`PlanIndex`-Erkennung. Mit SemanticRole bleibt die Logik gleich, nur über Katalog-Lookup statt direktem Enum-Vergleich.

**4. Migration von bestehenden Domain-Konstanten**

Wir haben in der aktuellen Code-Basis hartcodierte Listen:
- `BuildFieldTypeOptions()` in ProfileWizardViewModel
- `BuildFromWizard()` mit `Required = PlanNumber`-Logik
- IndexSource-Validierung in Schritt 3

Diese müssen alle Stelle für Stelle auf `ISegmentTypeCatalog` umgestellt werden. Liste wäre Pflicht-Bestandteil von BPM-108 Phase B.

## Korrektur an meinem Initial-Vorschlag

Mein Initial-Konzept hatte zwei Fehler:

1. **`fieldTypeId` in `RecognitionRule`** — falsch. Recognition braucht es nicht.
2. **„DocumentTypeRecognizer arbeitet mit FieldType"** — irreführend. Er arbeitet seit BPM-082 mit `method`/`pattern`/`segmentPosition`.

Beides ist von ChatGPT scharf korrigiert. Im Folge-Prompt sollte ich diese Korrekturen explizit aufnehmen, damit ChatGPT sieht, dass die Punkte angekommen sind.

## Offene Entscheidungen (für User)

ChatGPT hat 5 Rückfragen gestellt. Meine Priorisierung:

| # | Frage | Mein Vorschlag | Auswirkung |
|---|---|---|---|
| 1 | Built-in umbenennbar? | **Nein — nur deaktivieren** | Vereinfacht Update-Policy massiv |
| 2 | Custom SemanticRole wählbar? | **Optional, Default `None`** | Power-User können „räumlich" wählen, normale User ignorieren es |
| 3 | identityFields UI-geführt? | **Implizit aus SemanticRole**, später UI-Override | Frühphase: weniger UI-Code |
| 4 | `recognition_profiles`-Tabelle vorbereiten? | **Nein — separates ADR/Task** | BPM-108 bleibt fokussiert |
| 5 | `bpm.db` Komplett-Reset OK? | **Ja — Frühphase, keine Produktivdaten** | Sauberer Schnitt, kein Migrations-Code |

Davon sind **Frage 1 und 2** die architektur-relevantesten — sie bestimmen, ob die `user_modified_*`-Flags in der DB landen oder ob die harte Variante reicht.

## Empfohlene nächste Schritte

1. User-Entscheidungen zu Fragen 1+2 (Built-in-Editierbarkeit, Custom SemanticRole)
2. Optional Runde 2 mit Antworten + offenen Details (lastKnownLabel ja/nein, Custom-Chip-UI-Flow, Schritt 5)
3. BPM-108 ClickUp-Description nach Review updaten (3 Schichten A/B/C, SemanticRole, token_key, ProfileHealth)
4. Mockup-Iteration: Manager-Dialog ggf. ergänzen um „nur deaktivieren"-Hinweis bei Built-ins

---
doc_id: recovery-test-szenarien
doc_type: test-guide
authority: secondary
status: active
owner: herbert
topics: [recovery, planmanager, test, manual-test, bpm-016]
read_when: [recovery-aenderung, regressions-test, neuer-recovery-pfad]
related_docs: [planmanager]
related_code: [src/BauProjektManager.PlanManager/Services/RecoveryDecisionService.cs, src/BauProjektManager.PlanManager/Services/RecoveryExecutorService.cs, src/BauProjektManager.PlanManager/Views/RecoveryDialog.xaml]
supersedes: []
---

## AI-Quickload
- Zweck: Manuelle Test-Anleitung für die 5 Recovery-Szenarien (BPM-016 / 016.05)
- Autorität: secondary (Test-Guide, kein Source-of-Truth)
- Lesen wenn: Recovery-Code wurde geändert, vor V1-Release, Regressions-Verdacht
- Pflichtlesen: keine — kapitelweise nutzen
- Kapitel:
  - 1. Zweck
  - 2. Voraussetzungen
  - 3. Setup vor jedem Szenario
  - 4. Szenarien 1–5
  - 5. Cleanup nach Tests

## Fachliche Invarianten
- Recovery-Empfehlung ist deterministisch aus PendingImportInfo-Counts (siehe RecoveryDecisionService)
- Forward bei IsRollbackTrivial oder IsForwardTrivial → IsAutomaticAllowed=true
- Cleanup bei FailedActions > 0 → IsAutomaticAllowed=false
- Rollback ist NIE Empfehlung — nur als manuelle User-Wahl
- Cleanup macht KEINE Disk-Operation, nur Status-Update

---

## 1. Zweck

End-to-End-Verifikation des Recovery-Flows in BPM-016 anhand 5 präparierter
Crash-Szenarien. Tests laufen manuell — kein automatisiertes Test-Framework
ist im Projekt etabliert (post-V1).

Triggert: Recovery-Code-Änderungen, neue Recovery-Pfade, Regressions-Verdacht
nach Refactoring rund um `import_journal` / `import_actions`.

---

## 2. Voraussetzungen

- BPM ≥ v0.27.22 (Recovery-Hook in `ProjectDetailView.OnStartImport`)
- DB-Browser für SQLite (z.B. „DB Browser for SQLite" oder VS Code SQLite-Extension)
- Test-Projekt mit Inbox-Ordner und mindestens 5 Demo-Plänen
- Pfad zur project-spezifischen `planmanager.db` —
  liegt im Projekt-Ordner unter `.bpm/planmanager.db` (siehe ADR-046)

---

## 3. Setup vor jedem Szenario

1. BPM schließen falls offen
2. Test-Projekt-Ordner bereinigen — alle Dateien zurück in `_Eingang/`
3. `planmanager.db` öffnen im DB-Browser
4. Tabellen `import_journal` + `import_actions` leeren:
   ```sql
   DELETE FROM import_actions;
   DELETE FROM import_journal;
   ```
5. Schreiben + Schließen
6. BPM starten, Projekt öffnen — kein Recovery-Dialog erwartet (DB ist leer)
7. **Einen normalen Import durchführen** (Inbox → Plans), damit echte Action-Daten in der DB stehen
8. Den letzten erfolgreichen Import-Datensatz als Vorlage nehmen — wird in Szenarien manipuliert

---

## 4. Szenarien

### Szenario 1 — Forward bei IsRollbackTrivial

**Hintergrund:** App-Crash sofort nach Journal-Erstellung, bevor irgendeine
Aktion ausgeführt wurde. CompletedActions = 0, alle pending.

**Vorbereitung (SQL):**
```sql
-- Latest journal auf 'pending' setzen
UPDATE import_journal SET status = 'pending', completed_at = NULL
WHERE id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1);

-- Alle Actions dieses Journals auf 'pending' setzen
UPDATE import_actions SET action_status = 'pending', error_message = NULL
WHERE import_id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1);

-- Plus: Datei-Operationen rückgängig machen — Dateien manuell zurück in _Eingang/
```

**Trigger:**
1. BPM schließen + neu starten
2. Projekt öffnen → PlanManager-Tab
3. „Import starten" klicken

**Erwartung:**
- Recovery-Dialog erscheint
- Header: „Nicht abgeschlossener Import gefunden"
- Counts: `0 fertig · N ausstehend · 0 fehlgeschlagen`
- Empfehlung: **Fortsetzen** mit Hinweis „N Aktion(en) noch ausstehend, keine bereits verschoben. Import kann sicher fortgesetzt werden."
- Auto-Hint sichtbar: „✓ Diese Empfehlung kann sicher automatisch ausgeführt werden."

**Akzeptanz nach „Fortsetzen":**
- DB: `import_journal.status = 'completed'`
- DB: alle `import_actions.action_status = 'completed'`
- Disk: alle Dateien sind in den Zielordnern (Plans/) angelangt
- Inbox: leer
- Log: `Recovery Forward fertig: N ok, 0 fehler`

---

### Szenario 2 — Forward bei IsForwardTrivial

**Hintergrund:** App-Crash nachdem alle Aktionen erfolgreich waren, aber bevor
das Journal auf 'completed' gesetzt wurde. CompletedActions = FileCount,
alle Aktionen done, nur Header pending.

**Vorbereitung (SQL):**
```sql
-- Journal auf 'pending' setzen, Actions bleiben 'completed'
UPDATE import_journal SET status = 'pending', completed_at = NULL
WHERE id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1);

-- Actions sind bereits 'completed' aus dem normalen Import — nicht verändern
```

**Trigger:** wie Szenario 1.

**Erwartung:**
- Counts: `N fertig · 0 ausstehend · 0 fehlgeschlagen`
- Empfehlung: **Fortsetzen** mit Hinweis „Alle N Aktion(en) erfolgreich abgeschlossen, nur Journal-Finalisierung fehlt."
- Auto-Hint sichtbar

**Akzeptanz nach „Fortsetzen":**
- DB: `import_journal.status = 'completed'`
- Disk: unverändert (Aktionen liefen schon vorher durch)
- Log: `Recovery Forward fertig: 0 ok, 0 fehler` (keine pending Actions zum Abarbeiten)

---

### Szenario 3 — Forward bei Mix-State

**Hintergrund:** App-Crash mitten im Import. Einige Aktionen wurden ausgeführt,
einige nicht. Keine Failures.

**Vorbereitung (SQL):** Bei einem Import mit z.B. 5 Aktionen die ersten 2 als
„completed" lassen, die letzten 3 auf „pending" setzen.
```sql
UPDATE import_journal SET status = 'pending', completed_at = NULL
WHERE id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1);

-- Bei den letzten 3 Actions Status zurücksetzen
UPDATE import_actions SET action_status = 'pending'
WHERE id IN (
    SELECT id FROM import_actions
    WHERE import_id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1)
    ORDER BY action_order DESC LIMIT 3
);

-- Disk: 3 entsprechende Dateien zurück in _Eingang verschieben (manuell)
```

**Trigger:** wie Szenario 1.

**Erwartung:**
- Counts: `2 fertig · 3 ausstehend · 0 fehlgeschlagen`
- Empfehlung: **Fortsetzen** mit Hinweis „2 Aktion(en) bereits erledigt, 3 ausstehend. Fortsetzen empfohlen — User-Bestätigung erforderlich da bereits Dateien verschoben wurden."
- Auto-Hint **NICHT** sichtbar (User-Bestätigung ist verlangt)

**Akzeptanz nach „Fortsetzen":**
- DB: alle Aktionen 'completed', Journal 'completed'
- Disk: alle Dateien in Plans/, Inbox leer
- Log: `Recovery Forward fertig: 3 ok, 0 fehler`

---

### Szenario 4 — Cleanup bei FailedActions

**Hintergrund:** Mix-State mit fehlgeschlagenen Aktionen. Automatische Reparatur
nicht sicher — Cleanup als manuelle Pflicht.

**Vorbereitung (SQL):**
```sql
UPDATE import_journal SET status = 'pending', completed_at = NULL
WHERE id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1);

-- 1 Action als 'failed' markieren
UPDATE import_actions SET action_status = 'failed', error_message = 'simulated failure'
WHERE id IN (
    SELECT id FROM import_actions
    WHERE import_id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1)
    ORDER BY action_order DESC LIMIT 1
);

-- Restliche 1-2 Actions auf pending setzen, Rest completed lassen
UPDATE import_actions SET action_status = 'pending'
WHERE id IN (
    SELECT id FROM import_actions
    WHERE import_id = (SELECT id FROM import_journal ORDER BY timestamp DESC LIMIT 1)
      AND action_status = 'completed'
    ORDER BY action_order DESC LIMIT 1
);
```

**Trigger:** wie Szenario 1.

**Erwartung:**
- Counts: `X fertig · 1 ausstehend · 1 fehlgeschlagen`
- Empfehlung: **Verwerfen** mit Hinweis „1 fehlgeschlagene Aktion(en). Automatische Reparatur nicht sicher — Journal als 'failed' markieren, manuelle Untersuchung empfohlen."
- Auto-Hint **NICHT** sichtbar

**Akzeptanz nach „Verwerfen":**
- DB: `import_journal.status = 'failed'`, `error_message LIKE 'Cleanup:%'`
- DB: pending-Actions auf 'failed' mit „cleanup: user choice"
- Disk: **unverändert** (Cleanup macht keine Disk-Operation)
- Log: `Recovery Cleanup fertig fuer Import {Id}`

---

### Szenario 5 — Rollback (manuelle User-Wahl)

**Hintergrund:** Mix-State wie Szenario 3, aber User möchte den Import
**rückgängig** machen statt fortzusetzen (z.B. weil die Quelldateien extern
geändert wurden und der Import nicht mehr aktuell ist).

**Vorbereitung:** Identisch zu Szenario 3 (Mix Completed+Pending, 0 Failed).

**Trigger:** wie Szenario 1, aber im Dialog **„Rückgängig"** klicken statt
„Fortsetzen".

**Erwartung im Dialog:**
- Empfehlung ist **Fortsetzen** (wie Szenario 3)
- User wählt aktiv „Rückgängig"

**Akzeptanz nach „Rückgängig":**
- DB: `import_journal.status = 'failed'`, `error_message = 'Recovery Rollback erfolgreich.'`
- DB: completed-Actions auf 'failed' mit „rolled back"
- DB: pending-Actions auf 'failed' mit „cancelled by rollback"
- Disk: zurück verschobene Dateien sind wieder in `_Eingang/`
- Disk: archivierte Dateien (`_Archiv/`) sind zurück an Original-Destination
- Log: `Recovery Rollback fertig: N ok, 0 fehler`

---

## 5. Cleanup nach Tests

Nach Abschluss aller 5 Szenarien:

1. Test-Projekt-Ordner manuell bereinigen
2. `planmanager.db` öffnen + leeren:
   ```sql
   DELETE FROM import_actions;
   DELETE FROM import_journal;
   ```
3. Test-Inbox wiederherstellen mit den ursprünglichen Demo-Dateien
4. Optional: Test-Projekt-DB mit einem korrekten Import (kein Recovery)
   abschließen für Smoke-Test

---

## Hinweise

- Wenn der Recovery-Dialog **nicht erscheint**: Pre-Condition prüfen
  (`SELECT COUNT(*) FROM import_journal WHERE status = 'pending'` muss > 0 sein)
- Wenn Forward fehlschlägt mit „Source-Datei nicht mehr da": Disk-Setup
  nochmal prüfen (Dateien tatsächlich zurück in `_Eingang/`)
- **„Später"-Test:** Nach jedem Szenario-Setup einmal „Später" klicken und
  prüfen dass kein Import startet, der Recovery-Dialog beim nächsten Klick
  aber wieder erscheint

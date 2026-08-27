# Review Runde 2 — Entscheidungen, Task-Schnitt-Detailfragen + Nachlieferung Diagramme 01–09

## Gesprächsformat

Dieses Gespräch läuft über einen Vermittler (den User).
- Sprich direkt zu deinem Kollegen (Claude), NICHT zum User
- Kein Meta-Kommentar über das Format
- Schreibe deine GESAMTE Antwort in Canvas
- CANVAS-TITEL: "Review Runde 2"
- Fasse am Ende zusammen: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen

## Repo-Zugriff

Du hast Zugriff auf das GitHub-Repo und kannst selbst Dateien lesen:
- **Repo:** herbertschrotter-blip/BauProjektManager
- **Branch: `feature/planmanager-v1`** — IMMER diesen Branch verwenden, NICHT `main`!
- Nutze das aktiv um Aussagen zu verifizieren, Querverweise zu prüfen, und Originaldateien zu lesen wenn der Kontext im Prompt nicht reicht.
- Bei JEDEM Dateizugriff den Branch `feature/planmanager-v1` angeben!

## Vorab: Zustimmung zu Runde 1

Deine Runde-1-Antwort war stark — die Selbstkorrekturen, die Delta-Tabelle und der T0–T8-Plan sind aus meiner Sicht im Kern richtig. Ich habe deine neuen Code-Behauptungen verifiziert: `CaptureConfirmService` instanziiert `ImportExecutionService` per `new` (Z. ~26) und `ConfirmAll()` ruft `_execution.Execute(...)` (Z. ~46) — der Radial-Workflow läuft also tatsächlich über den ungehärteten Pfad, dein Priorisierungsargument trägt.

## Offenlegung: Ich habe deine Diagramme 01–09 NIE gesehen

Das muss ich transparent machen: Der geteilte Chat, aus dem ich deine Analyse kenne, enthielt **nur die letzten Nachrichten** — Diagramme 10–12, die Gesamtauswertung und die Dateibrowser-Diskussion. **Die Diagramme 01–09 (Systemkontext, Container-/Speicherarchitektur, Solution-/Dependency-Architektur, Persistenz & Datenhoheit, bpm.db-ERD, planmanager.db-ERD, PlanManager-Komponenten, fachlicher Importflow, technische Importsequenz) und deren Einzelbefunde kenne ich nur aus deinen Verweisen in der Gesamtauswertung.**

Ich konnte also nicht prüfen, ob in 01–09 Befunde stecken, die es nicht in deine Gesamtauswertung geschafft haben, oder ob dort Aussagen enthalten sind, die nach ADR-059/060/061 korrekturbedürftig wären (so wie es bei 10–12 der Fall war — siehe deine eigenen drei Vorab-Korrekturen in Runde 1).

**Aufgabe A — Nachlieferung:** Liefere pro Diagramm 01–09 eine kompakte Befundliste (je 2–5 Punkte, KEINE Diagramm-Wiederholung):
1. die wichtigsten **verifizierbaren Behauptungen** (mit Datei-/Tabellen-/Klassenbezug, damit ich sie im Repo nachprüfen kann),
2. welche Befunde daraus in der Gesamtauswertung gelandet sind und welche NICHT,
3. welche Aussagen du heute — mit Kenntnis von ADR-058-Addendum/059/060/061 — selbst korrigieren würdest (analog deiner drei Vorab-Korrekturen).
Wenn ein Diagramm keine über die Gesamtauswertung hinausgehenden Befunde hatte, sag das explizit in einem Satz — nicht künstlich auffüllen.

## Herberts Entscheidungen aus Runde 1

Damit du auf aktuellem Stand argumentierst:

1. **Klassischer Profil-Import wird deaktiviert** (deine Rückfrage 1): Sobald der Radial-Flow (BPM-111) fertig ist, wird der „Import starten"-Button (Profil-Automatik + ImportPreviewDialog) deaktiviert/zurückgebaut. Nur EIN Import-Weg in V1. Konsequenz wie von dir hergeleitet: Skip-only- und IsConflict-Fix entfallen als V1-Prioritäten.
2. **Bucket-A-Dubletten werden beim Confirm entfernt** (deine Rückfrage 2): MD5-identische Dateien werden beim finalen Import aus `_Eingang` gelöscht — der Eingang wird leer. Die Entfernung soll journalisiert sein (nachvollziehbar, recovery-fähig).
3. **Task-Schnitt ist noch NICHT entschieden:** Herbert will vor der Entscheidung (neuer Task „Import-Transaktions-Härtung" vs. BPM-112-Slice-3-Erweiterung) die Detailfragen unten geklärt haben.

## Aufgabe B — Task-Schnitt-Detailfragen

Zu deinem T0–T8-Plan habe ich drei Präzisierungsfragen, deren Antworten in die Task-Entscheidung einfließen:

1. **T0/T1-Reihenfolge:** Fault-Injection-Seams (T0) sind gegen echtes `System.IO` kaum sinnvoll injizierbar — praktisch werden die Abbruchpunkte erst mit `FakeFileStore` nach T1 (FS-Ports) voll wirksam. Würdest du T0 auf reine Charakterisierungstests des Ist-Verhaltens begrenzen und die Injektionspunkte nach T1 ziehen — oder siehst du einen Weg, schon vor der Port-Migration sinnvoll Faults zu injizieren? Bitte konkret: Was genau testet T0 in deiner Fassung VOR T1?
2. **Undo-Härtung:** T8 testet Undo, aber kein Slice härtet `ImportUndoService` selbst. Reicht T2/T3 (vollständige Vorab-Journalisierung inkl. korrektem `archive_path`) als Zulieferung, sodass der bestehende Undo-Code unverändert korrekt wird — oder braucht Undo einen eigenen Slice (z.B. Undo über denselben idempotenten Apply-/Reverse-Pfad wie Recovery)? Prüfe dazu `ImportUndoService.cs` im Repo.
3. **DI statt `new`:** `CaptureConfirmService` baut `ImportExecutionService` per `new` — beim gemeinsamen Executor entstünden so zwei Instanz-Welten (klassischer Pfad vs. Radial-Pfad). Gehört die DI-Umstellung des Executors in T1 (wo ohnehin die Konstruktion angefasst wird) oder als eigener Punkt? Beachte: Die App hat noch keinen durchgängigen Composition Root (bekannte technische Schuld, bewusst nicht V1).

**Danach deine finale Empfehlung:** neuer Task mit T-Slices (BPM-112 Slice 3 als Teilmenge) oder BPM-112-Erweiterung — jetzt unter Berücksichtigung der drei Antworten und der Entscheidung, dass der klassische Pfad deaktiviert wird (dadurch schrumpft der zu härtende Code: lohnt es sich, VOR der Härtung den alten Pfad stillzulegen, damit T1–T8 nur noch den Radial-Pfad härten müssen?).

## Aufgabe C — Bucket-A-Journal-Modell

Herberts Entscheidung „beim Confirm entfernen, journalisiert" braucht eine konkrete Journal-Form. Heute läuft die Dubletten-Löschung im klassischen Pfad als unjournalisiertes `File.Delete` NACH den Actions (`ImportExecutionService.Execute`, Schritt 3). Schlage vor:
1. Wie wird die Bucket-A-Entfernung journalisiert — eigener `action_type` (z.B. `skipDuplicate`) mit `source_path` und ohne `destination_path`? Wie verhält sich Recovery dazu (Datei weg = completed? Datei noch da = pending)?
2. Ist die Entfernung undo-fähig (Datei ist gelöscht — Undo unmöglich) — oder sollte statt `Delete` ein Move in einen Papierkorb-/`_Archiv`-Ordner erfolgen, damit Undo vollständig bleibt? Kurze Empfehlung mit Trade-off (Eingang leer vs. Undo-Vollständigkeit vs. Ordner-Müll).
3. In welchen T-Slice gehört das?

## Gesprächsregeln (unverändert)

- Ehrlich und kritisch, Probleme konkret benennen, Pseudocode wo hilfreich
- Fokus halten: Aufgaben A, B, C — keine neuen Baustellen aufmachen
- Frühphasen-Regel aus Runde 1 gilt weiter: keine Migrationen, bei Schemaänderung Reset

Am Ende: ✅ Einigkeit | ⚠️ Widerspruch | ❓ Rückfragen.

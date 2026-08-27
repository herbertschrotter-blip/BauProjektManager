# Runde 2 — Herberts Entscheidungen

**Datum:** 2026-08-27

1. **Task-Schnitt:** ✅ **Eigener Task „Import-Transaktions-Härtung" mit H0 + T0–T8** (BPM-112 Slice 3 = T1), Start nach Abschluss BPM-111.06. Reihenfolge: H0 Cutover → T0 Characterization → T1 FS-Ports + Fault-Fake + lokale Constructor Injection → T2 vollständiger Action-Plan vor Mutation (inkl. Bucket-A-Actions) → T3 Disk-Protokoll (.bpm_tmp, atomic rename, Retry, deterministische Archive) → T4 DB-Transaction pro Action + idempotenter DB-Apply → T5 Recovery Forward über denselben Apply-Pfad → T6 failed/pending + Rollback/Cleanup → T7 Undo-Härtung → T8 Fault-/Crash-Matrix.

2. **Bucket A präzisiert:** ✅ **Direktes Delete, bewusst NICHT undo-bar.** `action_type = skipDuplicate`, journalisiert (source_path, MD5, Größe, Duplicate-Evidenz) + recovery-fähig (Endzustand: „redundante Inbox-Kopie existiert nicht mehr, Bestand MD5-verifiziert"; fehlt beides → RecoveryConflict). Kein Papierkorb/Quarantäne. `import_actions.destination_path` wird nullable → planmanager.db-Reset, keine Migration. Präzisiert die r1-Entscheidung: „journalisiert" = nachvollziehbar + recovery-fähig, nicht Undo der Inbox-Kopie.

3. **Serie:** 🔄 **Runde 3 = beidseitiges Sign-off** (Slice-Folge + Invarianten fixieren), danach Serie abschließen.

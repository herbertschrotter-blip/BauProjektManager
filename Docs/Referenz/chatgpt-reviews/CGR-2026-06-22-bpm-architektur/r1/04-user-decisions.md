# Review Runde 1 — User-Entscheidungen

Herbert hat nach Runde 1 entschieden (Stufe A):

1. **Nächster Schritt:** → **Runde 2 zur Vertiefung** (vor ADR/Code).
2. **Seed-Quelle (Typ↔Ordner):** → **`ring2_source` in `FolderTemplate`** aufnehmen (Claude-Empfehlung). Vorlage beschreibt den Typ vollständig; Seed leitet `document_types` daraus ab, `folder_name` = realer präfixierter Ordner. Kein hardcodierter Switch. In Runde 2 Schema-Detail vertiefen.
3. **`profile.TargetFolder` jetzt brechen?** → **ChatGPT fragen** (Runde 2): Impact auf RecognitionProfile/ImportPlanBuilder/ProfileWizard + migrationsfreier Weg.
4. **Umbau-Scope:** → **Auch Settings/Views sofort** — alle ~29 System.IO-Stellen auf den Port heben (nicht nur Plan-Pfad).

## Konsens aus Runde 1 (bestätigt)
- Drei schmale Ports (`IFileSystemReader`/`IFileSystemWriter`/`IPathService`) + Adapter `LocalFileSystem`; kein God-Interface.
- Eigenes Interface statt System.IO.Abstractions (jetzt).
- DB = einzige Wahrheit, FolderTemplate nur Bootstrap; `document_types.folder_name` = realer präfixierter Ordner.
- Journal + temp-im-Zielordner + atomic rename + idempotente Recovery.
- System.IO raus aus Views/ViewModels; High-Level-Services bleiben, nur entkoppelt.

→ Runde 2 in [../r2/01-claude-prompt.md](../r2/01-claude-prompt.md).

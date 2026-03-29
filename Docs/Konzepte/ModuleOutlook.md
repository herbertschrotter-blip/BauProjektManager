# BauProjektManager — Modul: Outlook-Integration

**Status:** Später (Phase 4+)  
**Abhängigkeiten:** Einstellungen, PlanManager  
**Technologie:** Microsoft.Office.Interop.Outlook (COM)  
**Voraussetzung:** Outlook muss auf dem Rechner installiert sein  
**Referenz:** Architektur v1.4, Kapitel 11.3  

---

## 1. Konzept

C# greift direkt über COM Interop auf das lokal installierte Outlook zu. Projekt-Ordner werden automatisch erstellt, Plan-Anhänge (PDF/DWG) direkt in den _Eingang des jeweiligen Projekts extrahiert. VBA-Makros laufen parallel weiter.

---

## 2. Funktionen

| Funktion | Beschreibung |
|----------|-------------|
| Projekt-Ordner erstellen | Für jedes aktive Projekt einen Outlook-Ordner anlegen |
| Anhänge extrahieren | PDF/DWG Anhänge aus Emails → _Eingang |
| Emails verschieben | Bearbeitete Emails in Projekt-Ordner verschieben |
| Unbearbeitete anzeigen | Dashboard-Widget: X Emails mit Plan-Anhängen |
| Ordner archivieren | Abgeschlossene Projekte → Archiv-Ordner |

---

## 3. Workflow

```
1. User klickt "Outlook Sync" (Dashboard oder manuell)
2. App liest Registry → alle aktiven Projekte
3. Für jedes Projekt:
   a. Outlook-Ordner existiert? Nein → erstellen
   b. Inbox durchsuchen: Emails mit PDF/DWG Anhängen?
   c. Für jede gefundene Email:
      → Anhänge in _Eingang des Projekts extrahieren
      → Email in Projekt-Ordner verschieben
      → Markierung setzen (verarbeitet)
4. Abgeschlossene Projekte:
   → "Outlook-Ordner archivieren?" Dialog
5. Dashboard-Widget aktualisieren
```

---

## 4. GUI-Mockup

```
╔══════════════════════════════════════════════════════════════════╗
║  Outlook Sync                                                  ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Status: Outlook verbunden ✅                                   ║
║                                                                  ║
║  ╔══════════════════════╦═══════════╦═══════════╦══════════════╗║
║  ║ Projekt              ║ Outlook-  ║ Emails    ║ Anhänge      ║║
║  ║                      ║ Ordner    ║ gefunden  ║ extrahiert   ║║
║  ╠══════════════════════╬═══════════╬═══════════╬══════════════╣║
║  ║ ÖWG-Dobl-Zwaring    ║ ✅ Exists ║    3      ║ 5 PDF, 2 DWG║║
║  ║ Kapfenberg           ║ ✅ Exists ║    0      ║ —            ║║
║  ║ Leoben (fertig)      ║ ⚠️ Archiv?║   0      ║ —            ║║
║  ╚══════════════════════╩═══════════╩═══════════╩══════════════╝║
║                                                                  ║
║  [ Sync starten ]  [ Nur Ordner erstellen ]  [ Schließen ]     ║
║                                                                  ║
║  Log:                                                            ║
║  14:32:01 Outlook verbunden                                     ║
║  14:32:02 3 Emails mit Anhängen in Dobl gefunden                ║
║  14:32:03 Extrahiere: Polierplan_S-115-A.pdf → _Eingang        ║
║  14:32:04 Sync abgeschlossen                                    ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 5. Pseudo-Code

```csharp
using Outlook = Microsoft.Office.Interop.Outlook;

public class OutlookSyncService
{
    public async Task<SyncResult> SyncAsync(List<Project> projects)
    {
        var app = new Outlook.Application();
        var ns = app.GetNamespace("MAPI");
        var inbox = ns.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderInbox);

        foreach (var project in projects.Where(p => p.Status == ProjectStatus.Active))
        {
            // Projekt-Ordner sicherstellen
            var folder = GetOrCreateFolder(inbox, project.ProjectNumber + "_" + project.Name);

            // Emails mit Plan-Anhängen finden
            foreach (Outlook.MailItem mail in inbox.Items)
            {
                var planAttachments = GetPlanAttachments(mail); // PDF, DWG
                if (planAttachments.Any())
                {
                    // Anhänge in _Eingang extrahieren
                    var inboxPath = Path.Combine(project.Paths.Root, project.Paths.Inbox);
                    foreach (var attachment in planAttachments)
                    {
                        attachment.SaveAsFile(Path.Combine(inboxPath, attachment.FileName));
                    }

                    // Email in Projekt-Ordner verschieben
                    mail.Move(folder);
                }
            }
        }

        // WICHTIG: COM-Objekte freigeben
        Marshal.ReleaseComObject(inbox);
        Marshal.ReleaseComObject(ns);
        Marshal.ReleaseComObject(app);
    }
}
```

---

## 6. COM-Objektfreigabe (Wichtig!)

Outlook COM-Objekte müssen explizit freigegeben werden, sonst bleibt Outlook im Speicher hängen:

```csharp
// IMMER am Ende:
Marshal.ReleaseComObject(mailItem);
Marshal.ReleaseComObject(folder);
Marshal.ReleaseComObject(inbox);
Marshal.ReleaseComObject(ns);
Marshal.ReleaseComObject(app);
```

Dies wird in den Coding Standards erst ergänzt wenn das Outlook-Modul entwickelt wird.

---

## 7. VBA-Koexistenz

Bestehende Outlook-VBA-Makros funktionieren weiter und lesen die gleiche `registry.json`. Schrittweise Migration:

1. Phase 1: VBA macht alles, C#-App existiert nicht
2. Phase 2 (jetzt): VBA liest registry.json, C#-App schreibt sie
3. Phase 3: C# Outlook-Modul übernimmt Sync, VBA bleibt für Sonderfälle
4. Phase 4: VBA wird nicht mehr gebraucht

---

## 8. Risiken

| Risiko | Mitigation |
|--------|-----------|
| Outlook nicht installiert | Modul prüft beim Start, zeigt Warnung |
| Outlook 32-bit vs 64-bit | COM Interop beachten, testen |
| Office-Updates ändern API | COM ist stabil, aber Version testen |
| Emails falsch zugeordnet | Nur Anhänge mit PDF/DWG Extension |
| Performance bei vielen Emails | Nur Inbox durchsuchen, nicht alle Ordner |

---

*Erstellt: 27.03.2026 | Phase 4+ (nach V1)*
# BauProjektManager — Modul: Vorlagen-System

**Status:** Später (Phase 4+)  
**Abhängigkeiten:** Einstellungen (Registry + Vorlagen-Pfad)  
**Technologie:** COM Interop (Excel/Word), ClosedXML  
**Referenz:** Architektur v1.4, Kapitel 11.6  

---

## 1. Konzept

Bestehende Excel- und Word-Vorlagen automatisch mit Projektdaten befüllen. Statt jedes Mal Projektname, Adresse, Gebäude etc. manuell einzutippen, wählt der User ein Projekt und eine Vorlage → die App füllt die Platzhalter aus und speichert eine Kopie im Projektordner.

---

## 2. Vorlagen-Ordner

```
Vorlagen/                                  ← OneDrive (sichtbar)
├── Excel/
│   ├── Betontabelle_v3.xlsm              ← Mit VBA-Makros
│   ├── Ziegeltabelle_v2.xlsm
│   ├── Bautagebuch_v1.xlsm
│   └── Stundenzettel_v1.xlsm
├── Word/
│   ├── Bautagebuch_Vorlage.dotx           ← Word-Vorlage mit Platzhaltern
│   ├── Bauprotokoll.dotx
│   └── Briefkopf.dotx
└── BPM_Helper.xlam                        ← Excel Add-In (Entscheidung offen)
```

---

## 3. templates.json

Verzeichnis aller Vorlagen mit Metadaten:

```json
{
  "schemaVersion": "1.0",
  "templatesPath": "C:\\Users\\Herbert\\OneDrive\\02Arbeit\\Vorlagen",
  "templates": [
    {
      "id": "betontabelle",
      "name": "Betontabelle",
      "file": "Excel\\Betontabelle_v3.xlsm",
      "version": "3.0",
      "type": "xlsm",
      "category": "Tabellen",
      "usesProjectData": true,
      "projectFields": ["name", "projectNumber", "address", "buildings"]
    },
    {
      "id": "ziegeltabelle",
      "name": "Ziegeltabelle",
      "file": "Excel\\Ziegeltabelle_v2.xlsm",
      "version": "2.0",
      "type": "xlsm",
      "category": "Tabellen",
      "usesProjectData": true,
      "projectFields": ["name", "projectNumber", "address"]
    },
    {
      "id": "stundenzettel",
      "name": "Stundenzettel",
      "file": "Excel\\Stundenzettel_v1.xlsm",
      "version": "1.0",
      "type": "xlsm",
      "category": "Personal",
      "usesProjectData": true,
      "projectFields": ["name", "projectNumber"]
    },
    {
      "id": "bautagebuch_word",
      "name": "Bautagebuch (Word)",
      "file": "Word\\Bautagebuch_Vorlage.dotx",
      "version": "1.0",
      "type": "dotx",
      "category": "Protokolle",
      "usesProjectData": true,
      "projectFields": ["name", "projectNumber", "address", "constructionStart"]
    },
    {
      "id": "bauprotokoll",
      "name": "Bauprotokoll",
      "file": "Word\\Bauprotokoll.dotx",
      "version": "1.0",
      "type": "dotx",
      "category": "Protokolle",
      "usesProjectData": true,
      "projectFields": ["name", "projectNumber", "address"]
    },
    {
      "id": "briefkopf",
      "name": "Briefkopf",
      "file": "Word\\Briefkopf.dotx",
      "version": "1.0",
      "type": "dotx",
      "category": "Allgemein",
      "usesProjectData": false,
      "projectFields": []
    }
  ]
}
```

---

## 4. Workflow

```
1. User wählt Projekt (aus Registry)
2. User wählt Vorlage (aus Vorlagen-Liste)
3. App zeigt Vorschau: Welche Felder werden befüllt
4. User klickt "Erstellen"
5. App kopiert Vorlage → Projektordner
6. App füllt Platzhalter mit Projektdaten:
   - Excel (COM): Named Ranges oder Zell-Adressen
   - Word (COM): Bookmarks oder Content Controls
7. Befüllte Datei wird geöffnet (User kann weiterarbeiten)
```

---

## 5. GUI-Mockup

```
╔══════════════════════════════════════════════════════════════════╗
║  Vorlage erstellen                                             ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Projekt: [ 202512_ÖWG-Dobl-Zwaring ▼ ]                        ║
║                                                                  ║
║  Vorlagen:                                                       ║
║  ╔════════════════════╦══════════╦═══════════╦═════════════════╗║
║  ║ Name               ║ Typ      ║ Kategorie ║ Projektdaten?  ║║
║  ╠════════════════════╬══════════╬═══════════╬═════════════════╣║
║  ║ Betontabelle       ║ .xlsm    ║ Tabellen  ║ ✅ Ja          ║║
║  ║ Ziegeltabelle      ║ .xlsm    ║ Tabellen  ║ ✅ Ja          ║║
║  ║ Stundenzettel      ║ .xlsm    ║ Personal  ║ ✅ Ja          ║║
║  ║ Bautagebuch (Word) ║ .dotx    ║ Protokolle║ ✅ Ja          ║║
║  ║ Briefkopf          ║ .dotx    ║ Allgemein ║ ❌ Nein        ║║
║  ╚════════════════════╩══════════╩═══════════╩═════════════════╝║
║                                                                  ║
║  Ausgewählt: Betontabelle                                       ║
║  Wird befüllt: Projektname, Nummer, Adresse, Gebäude            ║
║  Ziel: ...\202512_ÖWG-Dobl\Dokumente\Betontabelle.xlsm        ║
║                                                                  ║
║  [ Erstellen & Öffnen ]  [ Nur Erstellen ]  [ Abbrechen ]      ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 6. Technologie pro Dateityp

| Typ | Lesen | Schreiben | Braucht Office? |
|-----|-------|-----------|----------------|
| .xlsx | ClosedXML | ClosedXML | Nein |
| .xlsm (mit VBA) | COM Interop | COM Interop | Ja |
| .dotx / .docx | COM Interop | COM Interop | Ja |

ClosedXML kann kein VBA — für .xlsm Dateien mit Makros muss COM Interop (Excel) verwendet werden.

---

## 7. Platzhalter in Vorlagen

### Excel (COM Interop):
- Named Ranges: `BPM_ProjectName`, `BPM_ProjectNumber`, `BPM_Address`
- Oder feste Zellen: A1 = Projektname, B1 = Nummer (konfigurierbar)

### Word (COM Interop):
- Bookmarks: `BPM_ProjectName`, `BPM_Address`, `BPM_Date`
- Oder Content Controls mit Tags

### Mapping:

| Platzhalter | Quelle (Registry) |
|-------------|-------------------|
| `BPM_ProjectName` | project.name |
| `BPM_ProjectNumber` | project.projectNumber |
| `BPM_FullName` | project.fullName |
| `BPM_Address` | project.location.address |
| `BPM_Municipality` | project.location.municipality |
| `BPM_Buildings` | project.buildings (formatiert) |
| `BPM_Start` | project.timeline.constructionStart |
| `BPM_End` | project.timeline.plannedEnd |
| `BPM_Date` | Aktuelles Datum |

---

## 8. VBA-Anbindung (Entscheidung offen)

Zwei Optionen für bestehende Excel-VBA-Makros die auf Registry zugreifen:

| Option | Beschreibung | Vorteil | Nachteil |
|--------|-------------|---------|----------|
| **Add-In (.xlam)** | BPM_Helper.xlam einmal installieren | Code nur 1x pflegen, überall verfügbar | Pro Gerät aktivieren |
| **Gemeinsame .bas** | VBA-Modul in jede Vorlage importieren | Kein Add-In nötig | Code in jeder Datei kopiert |

Entscheidung wird getroffen wenn das Vorlagen-Modul entwickelt wird.

---

## 9. Abhängigkeiten

| Abhängigkeit | Pflicht? |
|---|---|
| Einstellungen (Registry) | Ja — Projektdaten zum Befüllen |
| Office (Excel/Word) | Ja für .xlsm/.dotx — Nein für .xlsx |
| templates.json | Ja — Vorlagen-Verzeichnis |

---

*Erstellt: 27.03.2026 | Phase 4+ (nach V1)*
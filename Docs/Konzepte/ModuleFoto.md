# BauProjektManager — Modul: Foto-Verwaltung

**Status:** Später (Phase 4+)  
**Abhängigkeiten:** Einstellungen (Foto-Pfad pro Projekt)  
**Referenz:** Architektur v1.4, Kapitel 11.5  

---

## 1. Konzept

Zeigt Baustellenfotos aus OneDrive nach Projekt und Datum an. Keine eigene Foto-Aufnahme — Fotos kommen vom Handy/Kamera und liegen bereits auf OneDrive im Projektordner unter `Fotos/`.

Hauptnutzen: Schneller Zugriff auf Fotos eines bestimmten Tages (für Bautagebuch, Dokumentation, Nachweise).

---

## 2. Funktionen

| Funktion | Beschreibung |
|---------|-------------|
| Tagesansicht | Alle Fotos eines Datums als Thumbnails |
| Projekt-Filter | Fotos nach Projekt filtern |
| Datum-Navigation | Kalender oder Pfeile [◀] [▶] |
| Thumbnail-Vorschau | Kleine Vorschaubilder, Klick → Vollbild |
| Foto für Bautagebuch wählen | Checkbox → wird ins Bautagebuch übernommen |
| EXIF-Daten anzeigen | Datum, Uhrzeit, GPS (wenn vorhanden) |
| Ordnerstruktur | Erwartet: Fotos/YYYY-MM-DD/ oder Fotos/ (flach) |

---

## 3. GUI-Mockup

```
╔══════════════════════════════════════════════════════════════════╗
║  Fotos — 202512_ÖWG-Dobl-Zwaring                              ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  Datum: [ 27.03.2026 ]  [◀] [▶] [📅]                           ║
║  Ordner: ...\Fotos\2026-03-27\  (12 Fotos)                     ║
║                                                                  ║
║  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐       ║
║  │ 📷   │ │ 📷   │ │ 📷   │ │ 📷   │ │ 📷   │ │ 📷   │       ║
║  │09:14 │ │09:31 │ │10:42 │ │11:05 │ │13:07 │ │13:22 │       ║
║  │☐     │ │☐     │ │☑     │ │☐     │ │☑     │ │☐     │       ║
║  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘       ║
║  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐       ║
║  │ 📷   │ │ 📷   │ │ 📷   │ │ 📷   │ │ 📷   │ │ 📷   │       ║
║  │14:15 │ │14:30 │ │15:31 │ │15:45 │ │16:02 │ │16:10 │       ║
║  │☐     │ │☑     │ │☐     │ │☐     │ │☑     │ │☐     │       ║
║  └──────┘ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘       ║
║                                                                  ║
║  3 Fotos ausgewählt                                              ║
║  [ → Ins Bautagebuch ]  [ Im Explorer öffnen ]  [ Schließen ]  ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 4. Foto-Suche

Das Modul sucht Fotos in folgender Reihenfolge:
1. `<Projekt>/Fotos/YYYY-MM-DD/` (Unterordner pro Tag)
2. `<Projekt>/Fotos/` (flach, nach EXIF-Datum oder Datei-Datum filtern)
3. Falls Fotos von Handy-Kamera automatisch auf OneDrive landen: Konfigurierbarer Quellordner

---

## 5. EXIF-Daten

```csharp
public class PhotoInfo
{
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public DateTime DateTaken { get; set; }       // Aus EXIF oder Datei-Datum
    public double? Latitude { get; set; }          // GPS aus EXIF
    public double? Longitude { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

---

## 6. Integration mit Bautagebuch

Wenn der User im Foto-Modul Bilder auswählt (Checkboxen) und "Ins Bautagebuch" klickt, werden die Dateinamen in den Tageseintrag übernommen. Das Bautagebuch zeigt dann Thumbnails dieser Fotos.

---

## 7. Performance

- Thumbnails werden beim ersten Laden generiert und gecacht (lokal, %LocalAppData%)
- Große Fotos (10+ MB) nicht komplett in den Speicher laden — nur Thumbnail
- WPF `BitmapImage` mit `DecodePixelWidth` für effiziente Thumbnails

---

*Erstellt: 27.03.2026 | Phase 4+ (nach V1)*
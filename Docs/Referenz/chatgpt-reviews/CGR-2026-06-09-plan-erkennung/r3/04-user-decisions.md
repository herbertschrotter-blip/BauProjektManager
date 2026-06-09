# Review Runde 3 — Herberts Entscheidungen (Serie-Sign-off)

## Sign-off
Strategie + Radial-UI **signiert** (B als Kern, A nur Assist; Radial primär + Caps + Listen-Fallback + Pending Assignments).

## 5 Design-Detail-Entscheidungen
1. **Geschoss = dritter Radial-Ring** (Plantyp → Bauteil → Geschoss, alle im Radial). [Abweichung von Claudes Empfehlung „nur Panel" — Herbert will Geschoss im Ring.] Cap wie ChatGPT: ≤6 direkt, ab 7 Liste.
2. **Bauteil-Sortierung:** kontextbasierter Vorschlag zuerst (Dateiname-/PlanNr-Kandidat + zuletzt verwendet), dann natural sort.
3. **„+ Bauteil":** Inline-Schnellanlage (schreibt building_parts) + Link zu Projekt-Einstellungen.
4. **PDF+DWG:** default „eine Revision" vorschlagen, im Panel bestätigen.
5. **Listen-Fallback:** dauerhaftes rechtes Detail-Panel (Preview + Editor in einem).

## Festschreibung
Gewählt: **ADR + Tickets jetzt**. → ADR-059 „Recognition v2 / Plan-Erfassung" + ClickUp-Ticket-Umbau.

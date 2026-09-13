# Changelog — easySkills Crosshair

Hier steht, welche Features wann eingebaut wurden, **neueste zuerst**.
Neue Einträge kommen unter „Unveröffentlicht", bis eine Version getaggt wird.

Kategorien: **Neu** · **Geändert** · **Behoben** · **Entfernt**

---

## Unveröffentlicht

### 2026-09-13 — Sensitivity Converter: Trial/Pro-Aufteilung · `21098ff`
**Neu**
- Pro-Spielebibliothek mit 18 zusätzlichen Spielen (Source, GoldSrc, id Tech, klassische CoD-Titel), z. B. Deadlock, CS: Source, Left 4 Dead 2, Quake III, CoD4.
- Pro-Spiele sind im Trial sichtbar, aber mit PRO-Schloss gesperrt; beim Wechsel Pro → Trial springt die Auswahl automatisch auf ein Trial-Spiel.
- Markierung „ungeprüft" für Faktoren, die noch nicht gegen eine Referenz verglichen wurden.
- Prüftabelle `docs/SENSITIVITY_VERIFICATION.md` zum Abgleich mit gamingsmart.

**Geändert**
- Trial enthält die 12 Kernspiele plus „Benutzerdefiniert".
- `games.json` hat neue Felder `tier` (trial/pro, fehlend = pro) und `verified`.

### 2026-09-13 — Sensitivity Converter · `ea86a15`
**Neu**
- Umrechnung der Sensitivity zwischen Spielen (Von/Zu) nach der 360°-Distanz-Methode, DPI getrennt je Seite.
- Ergebnis gerundet auf die Einstellungs-Präzision des Zielspiels, dazu exakter Wert, cm/360°, inch/360°, eDPI.
- Tausch-Button, Kopieren-Button, letzte Eingabe wird gespeichert; Komma und Punkt als Dezimaltrenner.
- Spieldaten in `Data\games.json` neben der exe editierbar; bei defekter Datei Rückfall auf eingebaute Werte mit Warnung.

**Entfernt**
- Alter Einzelspiel-DPI-Rechner (im Converter enthalten).

**Behoben**
- Tausch-Button übernahm einen falsch neu berechneten Wert.

### 2026-09-13 — Pro-Edition · `a9fb52d`
**Neu**
- Ebenen-Editor: beliebig kombinierbare Ebenen (Kreuz/T-Form, Punkt, Kreis, X, Box, Chevron, Linie, Bild) mit eigener Farbe, Kontur, Position, Rotation.
- Eigene Fadenkreuz-Bilder: PNG, animiertes GIF, SVG.
- Dynamic Reactions: Bloom beim Schießen, Ausblenden beim Zielen, T-Form beim Laufen (per Raw Input, ohne Eingabe-Latenz).
- Schießstand: Modi Flick, Präzision, Tracking; Dauer 0,5–30 min; Score, Treffer, Präzision, Reaktionszeit.
- Community: 8 Vorlagen, Teilen per Share-Code oder `.escrosshair`-Datei.
- Pro/Trial-Schalter in Settings (in Debug-Builds immer verfügbar).

### 2026-09-13 — GUI & Grundgerüst · `3f51259`
**Neu**
- WPF-App mit Schwarz-Gold-Design, Sidebar-Navigation, Live-Vorschau.
- Passives Topmost-Overlay (klick-durchlässig, Multi-Monitor/DPI).
- Lizenzierung über Steam-DLC mit Trial-Fallback, Tray-Icon mit sauberem Beenden.
- Profile pro Spiel speichern/laden.

### 2026-09-12 — Projektstart · `28aead7`
- Leeres Projekt angelegt.

# Sensitivity-Converter: Prüftabelle

Abgleich unserer Faktoren in `src/easySkillsCrosshair.Core/Sensitivity/games.json` mit einem Referenz-Converter
(z. B. https://gamingsmart.com/mouse-sensitivity-converter/). Die Referenz dient nur zum **Gegenprüfen einzelner Werte** —
ihre Daten werden nicht übernommen.

## So prüfst du

1. Auf der Referenzseite einstellen: **Von: Counter-Strike 2**, **Sensitivity: 1**, **DPI: 800** (beide Seiten).
2. Als Ziel das Spiel aus der Tabelle wählen und den angezeigten Wert in die Spalte *Referenz* eintragen.
3. Stimmt er mit *Erwartet* überein (kleine Abweichungen in der letzten Nachkommastelle sind Rundung): *OK* ankreuzen.
4. Für jedes bestätigte Spiel in `games.json` `"verified": true` setzen. Weicht ein Wert ab oder fehlt das Spiel
   in der Referenz, in *OK* „abweichend" bzw. „nicht vorhanden" notieren — dann den Eintrag korrigieren oder entfernen.

Gegenprobe für die Mausdistanz: Bei jedem Spiel ergibt das Ergebnis dieselben **51.95 cm / 360°** (CS2, 1.0, 800 DPI).

Erzeugt aus `games.json` — nach Änderungen an den Faktoren neu erzeugen.

| Spiel | Stufe | Erwartet (gerundet) | Exakt | Referenz | OK? |
|---|---|---|---|---|---|
| Andere Source-Spiele | Trial | 1 | 1 |  |  |
| Apex Legends | Trial | 1 | 1 |  |  |
| Call of Duty (MW / Warzone / BO6) | Trial | 3.33 | 3.333333 |  |  |
| Counter-Strike 1.6 | Trial | 1 | 1 |  |  |
| Counter-Strike 2 | Trial | 1 | 1 |  |  |
| Counter-Strike: Global Offensive | Trial | 1 | 1 |  |  |
| Fortnite | Trial | 0.04 | 0.039604 |  |  |
| Overwatch 2 | Trial | 3.33 | 3.333333 |  |  |
| Quake Live | Trial | 1 | 1 |  |  |
| Team Fortress 2 | Trial | 1 | 1 |  |  |
| Titanfall 2 | Trial | 1 | 1 |  |  |
| Valorant | Trial | 0.314 | 0.314286 |  |  |
| Black Mesa | Pro | 1 | 1 |  |  |
| Call of Duty 4: Modern Warfare (2007) | Pro | 1 | 1 |  |  |
| Call of Duty: Black Ops (2010) | Pro | 1 | 1 |  |  |
| Call of Duty: Modern Warfare 2 (2009) | Pro | 1 | 1 |  |  |
| Call of Duty: World at War | Pro | 1 | 1 |  |  |
| Counter-Strike: Condition Zero | Pro | 1 | 1 |  |  |
| Counter-Strike: Source | Pro | 1 | 1 |  |  |
| Day of Defeat: Source | Pro | 1 | 1 |  |  |
| Deadlock | Pro | 1 | 1 |  |  |
| Garry's Mod | Pro | 1 | 1 |  |  |
| Insurgency (2014) | Pro | 1 | 1 |  |  |
| Left 4 Dead 2 | Pro | 1 | 1 |  |  |
| Portal 2 | Pro | 1 | 1 |  |  |
| Quake III Arena | Pro | 1 | 1 |  |  |
| Titanfall (2014) | Pro | 1 | 1 |  |  |
| Warsow / Warfork | Pro | 1 | 1 |  |  |
| Wolfenstein: Enemy Territory | Pro | 1 | 1 |  |  |
| Xonotic | Pro | 1 | 1 |  |  |
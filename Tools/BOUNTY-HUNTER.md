# Bounty Hunter – Kit 6

| Taste | Fähigkeit | Verhalten |
| --- | --- | --- |
| 1 | Twin Pulses | 1 s Cast; zweimal 10 Schaden; 8 Energie pro abgeschlossenem Cast. 10% Chance, Pulse Kick zurückzusetzen. |
| 2 | Jetpack Boost | 8 m in Bewegungsrichtung bei 12 m/s; 5 s Cooldown. Laufende Casts bleiben erhalten, weitere Zauber können während des Schubs gestartet werden. |
| 3 | Ion Grenade | Kostenlos, 8 s Cooldown, 2,6 m Radius. 15 Schaden und 15 s lang 6 Schaden pro Sekunde sowie 50% Slow auf alle Getroffenen. Jeder erfolgreiche Treffer/Tick erzeugt 3 Energie pro Gegner. |
| 4 | Pulse Kick | Vorstoß, 35 Schaden, 2 s Stun, 8 Energie pro Treffer; 20 s Cooldown. |
| 5 | Explosive Shot | Zielverfolgender Schuss auf den ausgewählten Gegner, ohne Bodenvorschau. Kostenlos, 5 s Cooldown. Beim Treffer jeweils 70 Schaden am Ziel und allen Gegnern im Radius von 2,8 m. Wände stoppen das Projektil. |
| F | Charged Shot | 1,5 s Cast im Stand, 30 Energie, 180 Schaden; bisheriger eigener Cooldown von 7 s. |
| C | Aegis Shield | 5 s lang 50% Schadensreduktion, 30 s Cooldown. Maus über einen echten Verbündeten in 20 m Reichweite halten und C drücken; ansonsten Selbstanwendung. Bisherige Kosten: 15 Energie. |
| V | Overdrive | 3 s kanalisierte Salve aus beiden Waffen; 20 m Reichweite und 80° Kegel. 15 Wellen mit jeweils 40 Schaden pro Gegner. Bisheriger Cooldown: 35 s; Kosten: 75% der maximalen Energie (aktuell 75). |

Jeder erfolgreiche **Ion-DoT-Tick** hat 15% Chance auf einen einzelnen, nicht stapelbaren Charged-Shot-Proc: sofort, 216 Schaden (+20%), weiterhin 30 Energie. Wie beim bisherigen Overcharge umgeht der Proc den eigenen Charged-Shot-Cooldown. Der globale Cooldown bleibt bestehen. Twin Pulses erzeugt diesen Proc nicht mehr; dessen Kick-Reset hat einen eigenen leuchtenden Rahmen.

Overdrive gibt **ab Kanalstart 15 Sekunden** lang 30% kürzere Castzeiten und 50% höheres Lauftempo. Twin Pulses dauert dann 0,7 s, Charged Shot 1,05 s. Die Castleisten verwenden die beim Caststart festgelegte Dauer. Der globale Cooldown bleibt bei 1 s. Die Kanalrichtung steht nach Bestätigung fest; ein Jetpack-Schub unterbricht auch die Salve nicht. Ohne Jetpack steht der Hunter während der Salve.

## Bodenvorschau

3, 4 und V öffnen die Zielvorschau. Maus bewegen, **Linksklick bestätigt**, **Rechtsklick oder Escape bricht ab**. Erneutes Drücken derselben Fähigkeit bricht ebenfalls ab. Während der Vorschau bleibt der Cursor frei; WASD und Q/E funktionieren weiter. Erst die Bestätigung bezahlt Energie und startet den Cooldown. Tab-Ziele lenken bestätigte Skillshots nicht um. Explosive Shot (5) benötigt ein ausgewähltes Ziel und feuert direkt; ein späterer Zielwechsel lenkt das Projektil nicht um.

Kreis: Ion Grenade. Kurze Vorstoßfläche: Kick. Großer Kegel: Overdrive. Rot bedeutet zu wenig Energie oder einen Zielpunkt außerhalb der Reichweite. C verwendet den Verbündeten unter der Maus direkt, ohne Bodenvorschau. Das bestehende Dummy-Gruppenframe repräsentiert noch keinen echten Mitspieler und ist deshalb kein Schildziel.

## Pflege und Prüfung

Die Werte liegen in `Assets/Abilities`; die acht Referenzen in `Assets/Resources/Classes/02_BountyHunter.asset`. `HunterLoadoutSetup` migriert ältere Loadouts einmalig auf Version 6. Danach bleiben manuell angepasste Werte erhalten.

Laufzeit- und Editor-Code wurden mit dem Compiler der installierten Unity-Version geprüft. 33 Geometrieprüfungen decken Reichweite, Kegelgrenzen, gedrehte Richtungen und die Begrenzung des Mauspunktes ab. Spielgefühl, Animationen, Bodenvorschau und das Zusammenspiel im Kampf werden im Play Mode getestet.

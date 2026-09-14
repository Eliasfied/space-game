# The Crucible / Asterion

Sci-Fi-Bosskampf-Prototyp in Unity URP. Lokaler Einzelspieler-Prototyp mit Vanguard und Bounty Hunter; Multiplayer ist noch nicht implementiert.

Das aktuelle Level beginnt in einem Aegis-Zugangsgang mit drei Wachen. Nach ihrem Tod öffnet sich die Sicherheitstür zur größeren Bosskammer. Modulare Böden, Wände, Türen und Deckungen liegen unter `Assets/Resources/Environment/Aegis`; die Minimap oben rechts zeigt die Umgebung und aktuelle Gegner. Aufbau und Regenerierung sind in [Tools/AEGIS-LEVEL.md](Tools/AEGIS-LEVEL.md) beschrieben.

## Auf einem anderen Computer weiterarbeiten

1. **Unity 6000.6.0f1** über Unity Hub installieren (genaue Version steht in `ProjectSettings/ProjectVersion.txt`).
2. Repository klonen (oder in GitHub Desktop öffnen):
   ```sh
   git clone https://github.com/Eliasfied/space-game.git
   ```
3. In Unity Hub **Add / Projekt hinzufügen** wählen und den geklonten Ordner `space-game` öffnen.
4. Den ersten Paket- und Asset-Import abwarten. Dafür braucht Unity Internetzugang. `Library` wird lokal neu erzeugt.
5. `Assets/Scenes/Asterion.unity` öffnen, Play drücken und eine Klasse wählen.

Die FBX-Modelle, Texturen, Animationen, Materialien, Skripte, Szenen und alle zugehörigen `.meta`-Dateien sind im Repository enthalten. Git LFS ist für diesen Stand nicht erforderlich. Die eingebetteten Pakete unter `Packages` gehören zum Projekt und werden mitversioniert.

Die Meshy-Editor-Skripte erzeugen/aktualisieren beim Import die Charakter- und Gegner-Prefabs. Falls nötig stehen die Befehle unter **Asterion → Meshy → Rebuild … Integration** zur Verfügung.

## Arbeitsablauf

### Steuerung im Kampf

- **WASD:** Bewegung entlang der festen Kameraausrichtung.
- **Rechte Maustaste halten + Maus nach links/rechts:** Spieler drehen. Die Kamera behält ihren Winkel und folgt weiterhin der Spielerposition.
- **Rechte Maustaste + W/S:** vorwärts/rückwärts in Blickrichtung des Spielers; **A/D** bewegt ihn seitwärts, ohne ihn zu drehen.
- **Q/E:** jederzeit nach links/rechts relativ zur Blickrichtung strafen, auch ohne rechte Maustaste.
- **Beide Maustasten halten:** vorwärtslaufen; mit der Maus lenken. Solange WASD gedrückt ist, hat die Tastaturrichtung Vorrang (S rückwärts, A/D rein seitwärts, Kombinationen diagonal). Nach dem Loslassen läuft der Spieler wieder per Maus vorwärts. Q/E ergänzt das Mauslaufen weiterhin um seitliches Strafing.
- **1, 2, 3, 4, 5, F, C, V:** Fähigkeiten. Der zuvor auf Q liegende Spell liegt jetzt auf **5**.
- **Bounty Hunter:** 3, 4 und V öffnen eine Bodenvorschau; Linksklick bestätigt, Rechtsklick/Esc bricht ab. Explosive Shot (5) feuert direkt auf das ausgewählte Ziel. Details und aktuelle Werte stehen in [Tools/BOUNTY-HUNTER.md](Tools/BOUNTY-HUNTER.md).
- **Tab / kurzer Linksklick:** Ziel wählen. **Esc:** Pause. **R:** Neustart/Klassenauswahl.

Unity kann beim Bearbeiten offen bleiben. Vor Änderungen Play stoppen, danach Unity kompilieren/importieren lassen und manuell testen.

```sh
git status
git add Assets Packages ProjectSettings Tools
git commit -m "Beschreibe deine Änderungen"
```

**Asterion → Generate Arena** baut die Szene neu auf und kann eigene Szenenänderungen ersetzen; dieser Befehl gehört nicht zum normalen Startablauf.

Die externe Blender-Quelldatei aus dem bisherigen übergeordneten `ArtSource`-Ordner ist nicht Teil dieses Unity-Repositories. Die für das Spiel benötigten exportierten Modelle sind enthalten.

## GitHub-Synchronisierung

Repository: https://github.com/Eliasfied/space-game

Vor dem Weiterarbeiten auf einem anderen Rechner den neuesten Stand holen:

```sh
git pull --ff-only
```

Nach einem Commit die Änderungen hochladen:

```sh
git push
```

Vor dem Rechnerwechsel Änderungen committen und pushen. Auf dem anderen Rechner vor dem Öffnen in Unity pullen.

# The Crucible / Asterion

Sci-Fi-Bosskampf-Prototyp in Unity URP. Lokaler Einzelspieler-Prototyp mit Vanguard und Bounty Hunter; Multiplayer ist noch nicht implementiert.

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

Unity kann beim Bearbeiten offen bleiben. Vor Änderungen Play stoppen, danach Unity kompilieren/importieren lassen und manuell testen.

```sh
git status
git add Assets Packages ProjectSettings
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

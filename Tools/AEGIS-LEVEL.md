# Aegis Warden approach

The environment is authored directly in `Assets/Scenes/Asterion.unity`, using ten reusable prefabs in `Assets/Resources/Environment/Aegis`. Walls and props use the supplied Meshy FBX models; floors, doors and small architectural details use the original URP materials.

- Boss chamber: 40 × 40 metres, from X −20 to 20 and Z −18 to 22. Four Neon Vault crates provide cover and leave the central combat lane open. Foreground walls are low for camera visibility.
- Approach: a 10 × 28 metre corridor. The player starts at Z −43; three Crimson Sentinels form a pack at (−1.5, −26), (1.5, −26) and (0, −23.5). They wait until a player comes within 8 metres with line of sight or damages a guard, then engage together. Each has 85 HP and uses the existing melee AI, animations, hit reactions and CC handling.
- The security door slides open after all three guards die. Entering the chamber removes the boss's protection but does not start combat. He engages within 8.5 metres with line of sight, or when damaged. Reinforcement waves start only after engagement.
- Between abilities the boss pursues the player at 3.2 metres/second (12% faster in phase 2), using a runtime NavMesh built from arena colliders and capsule sweeps to avoid walls and cover. He stays inside the chamber and stops moving during casts. This uses Unity's bundled AI module.
- In melee range he telegraphs a 130° hammer cleave for 0.8 seconds, locking his facing so the player can dodge. The matching ground outline and boss cast bar show the windup. On impact, targets within 3.1 metres and line of sight take 14 damage (18 in phase 2). Stun, knockback, death and disabling the boss cancel the attack and its indicator.
- The static architecture is batched once at runtime. Door leaves are separate dynamic objects. Floors, walls, cover, terminals, jambs and door leaves have physical colliders.
- All 46 full-height walls use the supplied Crimson Vault panel (1,822 triangles) with albedo, normal, metallic/smoothness and restrained crimson emission. Panels fit the existing 4 × 3.15 × 0.8 metre modules. The eight foreground walls use an actual lower mesh section with a bronze cut cap, keeping their 1.25 metre height without squashing the surface details. Dedicated corner and door-frame assets are still to follow.
- Props replace all four old cover blocks, two old terminals and six freestanding pillars. `Cover` now contains a 2.5 metre-wide Neon Vault crate; `Terminal` contains the 1.9 metre-tall red Side Console; `Pillar` contains a 2.5 metre-tall Arc Reactor Capsule. Models use uniform scaling and box colliders fitted to their geometry. Their materials retain the original red/cyan accents, normal maps and metallic/roughness detail. Consoles stand at X ±3.9, Z −37, facing into the corridor. Reactors use the former pillar positions along the chamber edges.
- Graphite armor, bronze trim, restrained crimson conduits, pale deck plates and white lamps match the Aegis boss. The two directional lights and ambient fill are brighter and less saturated; bloom, contrast and vignette are reduced.
- Camera size is 8.2 instead of 7.4, with the existing pitch and fixed heading retained.
- The minimap follows the player with north up. It displays authored rooms and cover, live enemies, the selected target, facing direction, the boss (clamped to the edge when distant), gate status and mission time.

`AegisLevel` owns the approach guards and door state. `LevelLayout.json` provides the common floor bounds for the minimap, enemy movement, knockback and grenade placement. These systems no longer clamp the new level back into the old 13-metre arena.

The wall source and textures live in `Assets/Art/MeshyEnvironment/CrimsonWall`. `AegisWallImportSettings` handles static FBX/texture import, and `AegisWallSetup` updates the existing `WallModule` and `LowWall` prefabs automatically after import and before Play. Root IDs, prefab GUIDs and box colliders are retained, so scene placement and collision routes remain valid. The menu command `Asterion / Meshy / Rebuild Crimson Walls` rebuilds these visuals. `Tools/PrepareCrimsonWall.py` repacks metallic/roughness and isolates the red conduit emission from the original maps; it requires Pillow. The original supplied FBX and four maps are preserved.

The three prop sources live in `Assets/Art/MeshyEnvironment/Props`. `AegisPropImportSettings` and `AegisPropSetup` import them and replace the visuals inside the existing `Cover`, `Terminal` and `Pillar` prefabs, retaining their GUIDs/root IDs. The collider sizes and minimap cover footprints follow the new crate dimensions. `Asterion / Meshy / Rebuild Aegis Props` rebuilds them; `Tools/PrepareAegisProps.py` prepares the material texture channels without changing the four supplied maps or FBXs. The old procedural prop children are removed from these prefabs.

To regenerate the authored environment after changing the generator:

```powershell
python Tools/GenerateAegisLevel.py
python Tools/ValidateAegisLevel.py
```

The generator preserves GUIDs and imported wall/prop prefabs, disables the old arena and training dummy, and replaces only its own reserved scene IDs on subsequent runs. It also updates the player/boss/camera positions and the environment lighting. Manual edits to generated instances are replaced when regenerating. The old `Asterion / Generate Arena` editor command still creates the legacy circular test scene.

Validation checks local scene/prefab references, pack spacing and collision-space routes: all three guards are reachable before the gate, the closed gate blocks the chamber, and the open gate connects the spawn to the boss and each combat lane. It also checks routes around all four covers using the boss's clearance. It does not replace a Unity import, runtime NavMesh check or live gameplay/visual test.

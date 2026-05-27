# Unity Tower Defense Prototype Design

## Goal

Build the first playable Unity prototype for Dragon Dominion using the existing Unity project, the `CLAUDE.md` architecture, and the seven supplied dragon images.

## Scope

The prototype centers on `Assets/Scenes/BattleScene.unity`. A player starts in setup, selects dragon cards, places dragons on buildable grid tiles, starts waves, watches enemies follow the path, and wins or loses through the existing victory/defeat panel.

The slice includes all seven Phase 1 dragons:

- `voltaris_001`: fast lightning single-target attacker.
- `frostfang_002`: ice AoE/control attacker.
- `magmaclaw_003`: fire burst attacker.
- `tempest_glacion_004`: expensive lightning/ice fusion powerhouse.
- `stonehide_005`: cheap durable earth bruiser.
- `celestara_006`: long-range light support damage.
- `shadowfang_007`: shadow single-target damage.

## Architecture

Keep the current Unity architecture. Static dragon and skill data live in ScriptableObjects, player-owned dragons are represented by `DragonInstance`, placed towers use `DragonTower`, and combat progression flows through `GameManager`, `WaveManager`, `ResourceManager`, `PlacementManager`, and the existing UI scripts.

The implementation should extend editor bootstrap/factory code so the prototype can be regenerated instead of hand-wired. Runtime scripts should receive only narrow fixes needed for a playable battle loop.

## Assets

Copy the attached PNGs into these Unity asset paths:

- `Unity/Assets/Art/Dragons/celestara_006/portrait.png`
- `Unity/Assets/Art/Dragons/frostfang_002/portrait.png`
- `Unity/Assets/Art/Dragons/magmaclaw_003/portrait.png`
- `Unity/Assets/Art/Dragons/shadowfang_007/portrait.png`
- `Unity/Assets/Art/Dragons/stonehide_005/portrait.png`
- `Unity/Assets/Art/Dragons/tempest_glacion_004/portrait.png`
- `Unity/Assets/Art/Dragons/voltaris_001/portrait.png`

When Unity imports them, set them as `Sprite (2D and UI)` and bind each portrait to `DragonDefinition.visualData.portrait`.

## Gameplay

The first battle uses a readable fixed path on a 12 by 8 grid. Path tiles are unbuildable. Dragon cards appear in the bottom HUD. Clicking or tapping a card enters placement mode, and clicking a buildable tile spends mana and spawns the matching dragon tower.

Waves spawn enemies from the first waypoint and complete only after every spawned enemy has either died or reached the base. Rewards are granted once per wave, then the UI exposes the next-wave button. Victory triggers after the final wave; defeat triggers when lives reach zero.

## UI

Reuse the existing Unity UI layer. The HUD shows lives, wave, mana, and gold. The bottom card panel shows every starter dragon with portrait, name, and mana cost. The result panel shows victory or defeat and supports retry.

## Verification

Run Unity/editor compile checks if a Unity executable is available. Also run .NET backend tests only if relevant commands are already configured and do not require unrelated setup. At minimum, inspect code paths for compile issues and report any verification that could not be run locally.

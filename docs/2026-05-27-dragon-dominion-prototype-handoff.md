# Dragon Dominion Prototype Handoff - 2026-05-27

## Repository State

- Branch pushed: `main`
- Latest pushed commit: `2aa9f99 Build Dragon Dominion tower defense prototype`
- Previous active branch: `claude/dragon-tower-defense-rpg-okxJw`
- Unity project root: `Unity/`
- Main playable scene: `Unity/Assets/Scenes/BattleScene.unity`
- Scene generator menu item: `Dragon Dominion > Build Battle Scene`
- Batch generator method: `DragonTD.Editor.SceneBootstrapper.Build`

This handoff continues the earlier update log at `docs/2026-05-26-dragon-dominion-update-log.md`.

## Current Prototype Summary

The Unity prototype is now a playable 3-wave tower defense slice with:

- Seven Phase 1 dragons available as placement cards.
- Portrait-based deployed dragon visuals.
- Enemy waves with previews, rewards, summaries, and victory after wave 3.
- Tower selection, sell, active skill casting, upgrades, and fusion.
- Enemy health bars, trait labels, damage numbers, shield visuals, regen pulses, flying/runner markers, projectile trails, death pops, and battle summary stats.
- Prototype balance constants centralized in `Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs`.

The backend was not part of this prototype pass.

## Important Gameplay Rules Implemented

### Placement

- Dragon cards appear at the bottom of the battle HUD.
- Selecting a card enters placement mode.
- Dragons can only be placed on buildable grid tiles.
- Placement spends mana.
- Starting resources are defined in `PrototypeBalance`:
  - Mana: `300`
  - Gold: `130`

Key files:

- `Unity/Assets/Scripts/TowerDefense/Placement/PlacementManager.cs`
- `Unity/Assets/Scripts/TowerDefense/Grid/GridManager.cs`
- `Unity/Assets/Scripts/TowerDefense/Grid/GridTile.cs`
- `Unity/Assets/Scripts/UI/DragonPlacementCard.cs`

### Combat

- `DragonTower` finds enemies in range and fires projectile attacks.
- Projectiles have clearer visuals, trails, colors, and damage-source attribution.
- Active skills use separate skill visuals and can apply status effects.
- Enemy damage shows categorized feedback:
  - projectile
  - lightning projectile
  - skill
  - burn
  - heal/regen
  - weak/resist feedback
  - shield break

Key files:

- `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs`
- `Unity/Assets/Scripts/TowerDefense/Combat/ProjectileBase.cs`
- `Unity/Assets/Scripts/TowerDefense/Combat/DamageIndicator.cs`
- `Unity/Assets/Scripts/TowerDefense/Combat/SkillCastEffect.cs`
- `Unity/Assets/Scripts/Dragons/AbilityExecutor.cs`
- `Unity/Assets/Scripts/Dragons/ElementInteraction.cs`

### Upgrades

- Towers upgrade with gold through the selected tower panel.
- Max normal upgrade level is `3`.
- Upgrade costs are based on `PrototypeBalance.UpgradeBaseCost`.
- Projectile visuals change by tower level.
- Level badge and tower scale update when upgraded.

Key files:

- `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs`
- `Unity/Assets/Scripts/UI/BattleHUD.cs`
- `Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs`

### Fusion / Merge

Current fusion rule:

- Both dragons must be level 3.
- A tower can only fuse once.
- Same-dragon level 3 fusion creates a stronger fused tower.
- Different level 3 dragon fusion creates a hybrid fused tower.
- Hybrid fusion keeps the primary tower, consumes the target tower, and inherits a passive effect from the consumed dragon element.
- Hybrid fusion is intentionally stronger than two separate level 3 dragons for this prototype.
- Fusion uses a two-click confirmation:
  1. Select a level 3 tower.
  2. Click `Fuse`.
  3. Click another valid level 3 tower once to preview.
  4. Click the same target again to confirm.

Balance values:

- Same-dragon fused damage multiplier: `2.35`
- Hybrid fused damage multiplier: `3.1`
- Fused tower display level: `4`
- Hybrid passive cooldown: `1.25`

Key files:

- `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs`
- `Unity/Assets/Scripts/TowerDefense/Combat/TowerSelectionManager.cs`
- `Unity/Assets/Scripts/UI/BattleHUD.cs`
- `Unity/Assets/Scripts/Core/BattleStatsTracker.cs`

### Enemy Traits

Special enemy traits were added for prototype clarity:

- Runner: faster enemy with `RUN` badge.
- Shielded: shield ring, reduced projectile damage until shield break, `SHIELD` or `CRACK` badge.
- Regenerating: heals over time, shows `REGEN` badge and green heal pulses. Fire/burn suppresses regen briefly.
- Flying: offset movement/visuals, `FLY` badge, weaker slow response.

Key files:

- `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs`
- `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyData.cs`
- `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyHealthBar.cs`
- `Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs`
- `Unity/Assets/Editor/SceneBootstrapper.cs`

### Waves

The prototype currently has 3 waves:

- `Unity/Assets/ScriptableObjects/Waves/Wave01.asset`
- `Unity/Assets/ScriptableObjects/Waves/Wave02.asset`
- `Unity/Assets/ScriptableObjects/Waves/Wave03.asset`

Wave generation happens in `SceneBootstrapper.cs`. Victory triggers after wave 3. The current design document says Phase 1 should eventually be 5 waves, so this is still short of the full Phase 1 target.

Key files:

- `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`
- `Unity/Assets/Scripts/TowerDefense/Waves/WaveData.cs`
- `Unity/Assets/Editor/SceneBootstrapper.cs`
- `Unity/Assets/Scripts/Core/GameManager.cs`

### HUD / UI

The battle HUD now includes:

- Lives, wave, mana, and gold.
- Next wave button.
- Wave preview panel.
- Wave summary panel.
- Tower selection panel.
- Upgrade, sell, skill, and fuse controls.
- Fusion preview and selection messages.
- Victory/defeat summary with kills, leaks, skills, fusion counts, and best damage source.

Key files:

- `Unity/Assets/Scripts/UI/BattleHUD.cs`
- `Unity/Assets/Scripts/UI/VictoryDefeatPanel.cs`
- `Unity/Assets/Scripts/Core/BattleStatsTracker.cs`

## Generated Assets

The scene bootstrapper creates or updates:

- `Unity/Assets/Prefabs/Dragons/*.prefab`
- `Unity/Assets/Prefabs/Enemies/*.prefab`
- `Unity/Assets/Prefabs/Projectile.prefab`
- `Unity/Assets/Prefabs/GridTile.prefab`
- `Unity/Assets/Prefabs/UI/PlacementCard.prefab`
- `Unity/Assets/ScriptableObjects/Dragons/*.asset`
- `Unity/Assets/ScriptableObjects/Skills/*.asset`
- `Unity/Assets/ScriptableObjects/Enemies/*.asset`
- `Unity/Assets/ScriptableObjects/Waves/*.asset`
- `Unity/Assets/Scenes/BattleScene.unity`

Do not hand-edit generated scene wiring unless there is a specific reason. Prefer updating `SceneBootstrapper.cs` and regenerating the scene.

## How To Regenerate

From the Unity editor:

```text
Dragon Dominion > Build Battle Scene
```

From PowerShell, with Unity closed:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'D:\DragonTD\DragonTD\Unity' `
  -executeMethod DragonTD.Editor.SceneBootstrapper.Build `
  -logFile 'D:\DragonTD\DragonTD\unity-prototype-regenerate.log'
```

If Unity is already open with this project, batchmode regeneration will fail because Unity locks the project. Use the editor menu item in that case.

## Manual Test Checklist

Use `Unity/Assets/Scenes/BattleScene.unity`.

- Press Play.
- Confirm all seven dragon cards render.
- Place at least two dragons.
- Start wave 1 with `Next Wave`.
- Confirm enemies follow the path and take projectile damage.
- Confirm damage numbers, trails, health bars, and death pops appear.
- Clear a wave and confirm rewards, wave summary, and next-wave preview appear.
- Upgrade two dragons to level 3.
- Fuse two level 3 dragons and confirm the target dragon is consumed.
- Fuse two different level 3 dragons and confirm the result shows as hybrid fused.
- Cast an active skill from the selected tower panel.
- Play through wave 3 and confirm victory panel appears.

## Known Caveats

- The prototype is still a Unity-generated slice, not final production content.
- Phase 1 in `AGENTS.md` says the goal is 5 waves; this prototype currently has 3.
- TextMesh Pro assets are included because the generated UI depends on runtime text rendering.
- Unity-generated YAML may contain trailing whitespace. Avoid bulk rewriting Unity `.asset`, `.prefab`, `.unity`, or `.meta` files just to clean whitespace because that can create noisy diffs.
- Runtime logs and Unity recovery folders are ignored by `.gitignore`.
- Backend API, database, gacha persistence, Addressables, Nakama, and mobile build hardening were not implemented in this pass.

## Recommended Next Development Pass

1. Expand from 3 waves to the Phase 1 target of 5 waves.
2. Add a proper pre-wave planning state with clearer affordances for build, upgrade, fuse, and start wave.
3. Add keyboard/touch polish for skill and fuse targeting cancellation.
4. Move prototype-only generated UI layout toward prefabs that designers can edit safely.
5. Add automated Unity playmode tests for wave progression, upgrade costs, skill casts, and fusion rules.
6. Start converting prototype balance constants into designer-owned ScriptableObject assets.
7. Add mobile safe-area layout checks and touch-first sizing.
8. Wire post-battle rewards into the broader progression model.

## Main Files To Read First

- `AGENTS.md`
- `docs/2026-05-26-dragon-dominion-update-log.md`
- `Unity/Assets/Editor/SceneBootstrapper.cs`
- `Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs`
- `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs`
- `Unity/Assets/Scripts/TowerDefense/Combat/TowerSelectionManager.cs`
- `Unity/Assets/Scripts/UI/BattleHUD.cs`
- `Unity/Assets/Scripts/Core/BattleStatsTracker.cs`
- `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`
- `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs`

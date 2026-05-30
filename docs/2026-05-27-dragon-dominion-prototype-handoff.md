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

The Unity prototype is now a playable 5-wave tower defense slice with:

- Seven Phase 1 dragons available as placement cards.
- Portrait-based deployed dragon visuals.
- Enemy waves with previews, rewards, summaries, and victory after wave 5.
- Pre-wave planning phases for placement, sell, upgrades, and fusion.
- Wave-only active skill casting.
- Enemy health bars, trait labels, damage numbers, shield visuals, regen pulses, flying/runner markers, projectile trails, death pops, and battle summary stats.
- Prototype balance constants centralized in `Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs`.

The backend was not part of this prototype pass.

## Important Gameplay Rules Implemented

### Placement

- Dragon cards appear at the bottom of the battle HUD.
- Selecting a card enters placement mode.
- Dragon cards dim and stop accepting clicks during active waves.
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
- Upgrades are planning-phase only.
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
- Fusion is planning-phase only.

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

The prototype currently has 5 waves:

- `Unity/Assets/ScriptableObjects/Waves/Wave01.asset`
- `Unity/Assets/ScriptableObjects/Waves/Wave02.asset`
- `Unity/Assets/ScriptableObjects/Waves/Wave03.asset`
- `Unity/Assets/ScriptableObjects/Waves/Wave04.asset`
- `Unity/Assets/ScriptableObjects/Waves/Wave05.asset`

Wave generation happens in `SceneBootstrapper.cs`. Victory triggers after wave 5, matching the Phase 1 wave target.

Key files:

- `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`
- `Unity/Assets/Scripts/TowerDefense/Waves/WaveData.cs`
- `Unity/Assets/Editor/SceneBootstrapper.cs`
- `Unity/Assets/Scripts/Core/GameManager.cs`

### HUD / UI

The battle HUD now includes:

- Lives, wave, mana, and gold.
- Start wave button during planning phases.
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

Owner: manual tester.

Record each item as `Pass`, `Fail`, or `Blocked`. Add short notes with exact repro steps for anything that fails.

### Test Setup

- [ ] Open the Unity project at `D:\DragonTD\DragonTD\Unity`.
- [ ] Open `Unity/Assets/Scenes/MainMenu.unity`.
- [ ] Press Play.
- [ ] Confirm the first screen is the Main Menu hub, not a blank scene or direct battle scene.
- [ ] Confirm no blocking errors appear in the Unity Console during boot.
- [ ] Optional backend test: run `Backend/DragonTD.API` locally and use the in-game/device login flow if available.

Notes:

```text
Tester:
Date:
Unity version:
Backend running: yes/no
Device/build target:
```

### Main Menu Hub

- [ ] Confirm Gold, Gems, Save/Sync status, Events, Battle, Dragons, Profile, Clan, Store, and chest slots are visible.
- [ ] Click `Battle` and confirm the stage selector opens.
- [ ] Click `Events` and confirm the Events panel opens.
- [ ] Click `Dragons` and confirm the Dragons collection mode opens.
- [ ] Click `Profile` and confirm the account progression mode opens.
- [ ] Click `Clan` and confirm the locked clan shell opens.
- [ ] Click `Store` and confirm the Gem Store panel opens.
- [ ] Close each panel and confirm you return cleanly to the Main Menu.
- [ ] Confirm no panels overlap in a way that blocks required buttons.

Notes:

```text

```

### Stage Selection

- [ ] Open the stage selector from `Battle`.
- [ ] Confirm Chapter 1 stages show lock/open/cleared state.
- [ ] Select each available stage and confirm title, enemy theme, recommended level, difficulty, rewards, objectives, and recommended roles update.
- [ ] Start a stage with missing recommended roles and confirm the warning appears.
- [ ] Press `Start Stage` again after the warning and confirm battle starts.

Notes:

```text

```

### Battle Loop

- [ ] Confirm all dragon placement cards render.
- [ ] Place at least two dragons.
- [ ] Start wave 1 with `Next Wave`.
- [ ] Confirm enemies follow the path and take projectile damage.
- [ ] Confirm damage numbers, trails, health bars, and death pops appear.
- [ ] Select a placed dragon and confirm tower details appear.
- [ ] Cast an active skill from the selected tower panel.
- [ ] Upgrade two dragons to level 3.
- [ ] Fuse two level 3 dragons and confirm the target dragon is consumed.
- [ ] Fuse two different level 3 dragons and confirm the result shows as hybrid fused.
- [ ] Clear a wave and confirm rewards, wave summary, and next-wave preview appear.
- [ ] Play through wave 5 and confirm the victory panel appears.
- [ ] Return to Main Menu and confirm the latest battle reward popup/summary appears.

Notes:

```text

```

### Progression And Rewards

- [ ] Confirm battle completion grants Gold, Essence, Gems, summon tickets, and/or chest rewards according to the result.
- [ ] Confirm stage stars appear on the victory panel.
- [ ] Confirm clearing a stage unlocks the next available stage.
- [ ] Return to stage selector and confirm best stars and cleared state persisted.
- [ ] Exit Play Mode, re-enter Play Mode, and confirm saved currencies, chest state, stage progress, and owned dragons reload.

Notes:

```text

```

### Dragons And Profile

- [ ] Open `Dragons`.
- [ ] Select dragons from the clickable list and confirm the detail panel updates.
- [ ] Test role filters: All, Damage, Slow, AoE, Support, Anti-Shield, Anti-Flying.
- [ ] Test loadout presets: Balanced, Boss, Fast Enemies, Shield Break.
- [ ] Equip and unequip dragons until the 6-dragon cap is reached.
- [ ] Level up a dragon and confirm Gold is spent.
- [ ] Train bond and confirm Essence is spent and bond XP changes.
- [ ] Try evolution requirements and confirm blocked/allowed states make sense.
- [ ] Open `Profile` and confirm account buffs, sync status, and account summary display correctly.

Notes:

```text

```

### Chests And Daily Objectives

- [ ] Win a battle and confirm a chest is awarded if a slot is empty.
- [ ] Start unlocking a chest.
- [ ] Confirm the countdown appears.
- [ ] Tap an unlocking chest and confirm Gem speed-up confirmation appears.
- [ ] Cancel speed-up and confirm Gems are unchanged.
- [ ] Confirm speed-up and verify Gems are spent and the chest becomes ready.
- [ ] Open a ready chest and confirm rewards are granted and the slot becomes empty.
- [ ] Open Events and confirm daily objective progress updates after battle/chest/summon actions.
- [ ] Claim a completed daily objective and confirm rewards are granted once.

Notes:

```text

```

### Events

- [ ] Open Events.
- [ ] Confirm `Daily Hunt`, `Gem Rush`, and `Clan Raid` are visible.
- [ ] Claim `Daily Hunt` and confirm rewards are granted.
- [ ] Try claiming `Daily Hunt` again and confirm it is blocked for the same UTC day.
- [ ] Confirm `Gem Rush` is preview-only.
- [ ] Confirm `Clan Raid` is locked.

Notes:

```text

```

### Store And IAP

Editor path:

- [ ] Open Store.
- [ ] Buy each Gem pack once using the editor mock purchase path.
- [ ] Confirm each purchase shows `Purchase validated`.
- [ ] Confirm Gems increase by the pack amount.
- [ ] Confirm failed/blocked states leave Gems unchanged.

Device/non-editor path:

- [ ] Build to target device.
- [ ] Confirm Unity IAP products load.
- [ ] Start a purchase and confirm the store purchase sheet appears.
- [ ] Complete a purchase and confirm the receipt is validated before Gems are granted.
- [ ] Cancel a purchase and confirm Gems are unchanged.
- [ ] Confirm duplicate transaction handling does not grant duplicate Gems.

Notes:

```text

```

### Backend Sync

Run this section only if the backend is running.

- [ ] Start `Backend/DragonTD.API`.
- [ ] Log in with a device ID from Unity.
- [ ] Confirm sync status changes from local/offline to API/saved state.
- [ ] Save progression and confirm no auth or sync errors appear.
- [ ] Complete a battle and confirm battle reward sync succeeds.
- [ ] Make a store purchase through the API-backed validator and confirm server validation succeeds.
- [ ] Restart Play Mode and confirm API-backed progression loads.

Notes:

```text

```

### Issue Log

Use one block per issue.

```text
ID:
Severity: Critical / High / Medium / Low
Area:
Steps to reproduce:
Expected:
Actual:
Screenshot/video:
Console errors:
Notes:
```

## Known Caveats

- The prototype is still a Unity-generated slice, not final production content.
- TextMesh Pro assets are included because the generated UI depends on runtime text rendering.
- Unity-generated YAML may contain trailing whitespace. Avoid bulk rewriting Unity `.asset`, `.prefab`, `.unity`, or `.meta` files just to clean whitespace because that can create noisy diffs.
- Runtime logs and Unity recovery folders are ignored by `.gitignore`.
- Backend API, database, gacha persistence, Addressables, Nakama, and mobile build hardening were not implemented in this pass.

## Follow-Up Update - 2026-05-27

### Phase 1 Wave Expansion

- Expanded the generated prototype from 3 waves to 5 waves.
- Added generation for `Wave04.asset` and `Wave05.asset` in `Unity/Assets/Editor/SceneBootstrapper.cs`.
- Wired both new wave assets into the generated `WaveManager` on `BattleScene.unity`.
- Updated the victory fallback message in `Unity/Assets/Scripts/Core/GameManager.cs` so it no longer hardcodes "all 3 waves".
- Added `tools/VerifyPhase1Waves.ps1` to verify the generator, generated wave assets, and scene wave wiring are all aligned around 5 waves.

Verification performed:

```powershell
powershell -ExecutionPolicy Bypass -File tools/VerifyPhase1Waves.ps1 -RequireGeneratedAssets
```

Expected result:

```text
Phase 1 wave configuration verified for 5 waves.
```

### PlayMode Test Coverage

Added Unity PlayMode tests under `Unity/Assets/Tests/PlayMode/`:

- `DragonTD.PlayModeTests.asmdef`
- `PrototypeCoreLoopTests.cs`

Covered behaviors:

- Wave progression reaches victory after 5 configured waves.
- Tower upgrades spend expected gold and stop at level 3.
- Active skill casting damages a target and records skill use.
- Hybrid fusion consumes the target tower and records fusion stats.
- Wave clear applies gold and mana rewards.
- Planning state brackets waves.
- Tower upgrades are planning-only.
- Active skills are wave-only.

Verification performed in the Unity PlayMode Test Runner:

```text
Passed total=7 passed=7 failed=0 skipped=0
```

### Documentation Rule

Going forward, every implementation update should include a concise entry in this handoff/update documentation before the task is considered complete.

### Planning Phase and Input Polish

- Added `GameState.Planning` as the pre-wave and between-wave build phase.
- `GameManager.StartBattle()` now enters planning before wave 1, and cleared waves return to planning until the final victory state.
- Placement, upgrades, fusion, and sell actions are planning-phase only.
- Active skills are wave-only and show `Wave Only` on the selected tower panel outside combat.
- Escape cancels placement, skill targeting, and fusion targeting.
- Two-finger touch cancels placement or targeting on mobile.
- Placement cards now dim and stop accepting clicks during active waves.
- Empty or invalid wave completion is deferred by one frame so wave state and counters finish updating before completion callbacks run.

User verification:

```text
Planning-phase flow reported working by user.
Placement cards were confirmed non-clickable during waves; visual dimming needed a stronger direct graphic-alpha pass.
Fortify was confirmed to add 12 mana and show its notification.
```

Follow-up adjustment:

- Placement cards now apply dimming directly to child UI graphics while preserving their original alpha values, instead of relying only on `CanvasGroup` alpha.

### Cleanup Follow-Up

- Removed the temporary `Unity/Assets/Editor/CodexTestRunner/` editor test-runner scaffold from the working tree.
- Marked the older `docs/superpowers/plans/2026-05-26-unity-tower-defense-prototype.md` as historical because it describes the original 3-wave implementation plan.

### Prototype Hardening Pass

- Added prefab-first UI scene assembly in `Unity/Assets/Editor/SceneBootstrapper.cs`.
- `SceneBootstrapper` now creates default editable UI prefabs only when missing:
  - `Unity/Assets/Prefabs/UI/BattleHUD.prefab`
  - `Unity/Assets/Prefabs/UI/DragonCollectionPanel.prefab`
  - `Unity/Assets/Prefabs/UI/VictoryDefeatPanel.prefab`
- Regenerated scenes instantiate those UI prefabs instead of rebuilding every UI object inline, so designer edits to existing prefabs survive future regeneration.
- `PlacementCard.prefab` is also no longer overwritten when it already exists.
- Added `Unity/Assets/Scripts/UI/SafeAreaPanel.cs` and wrapped regenerated UI under `SafeAreaRoot` for mobile notch/home-indicator protection.
- Added `Unity/Assets/Scripts/TowerDefense/PrototypeBalanceConfig.cs`.
- `PrototypeBalance` now reads designer-owned values from `Resources/PrototypeBalanceConfig.asset` when present, with code defaults as fallback.
- `SceneBootstrapper` creates `Assets/Resources/PrototypeBalanceConfig.asset` if missing during regeneration.
- Moved battle completion rewards behind `PlayerInventory.GrantBattleCompletionRewards(...)`.
- Victory now grants local bond XP through `PlayerInventory`, emits an inventory refresh, and appends a progression reward summary to the victory message.
- Fortify remains wave-only for now. It is treated as an active combat utility that grants `+12 MP` and shows a notification; planning-compatible prep skills can be added later with an explicit per-skill rule.

### Mobile Layout and Save-Ready Rewards

- Added `Unity/Assets/Scripts/UI/ResponsiveBattleUILayout.cs`.
- Regenerated scenes attach `ResponsiveBattleUILayout` to `SafeAreaRoot` so HUD controls, wave panels, victory panel, and placement cards adjust for compact and portrait screens.
- Placement cards get runtime `LayoutElement` sizing after inventory population, keeping touch targets readable on mobile.
- Top-right controls use larger touch sizes on compact screens, and wave preview/summary panels move upward to reduce bottom playfield overlap.
- Added `Unity/Assets/Scripts/Core/BattleRewardResult.cs` with serializable `BattleRewardResult` and `DragonBattleRewardEntry` DTOs.
- `PlayerInventory.GrantBattleCompletionRewards(...)` now returns a save-ready reward result while preserving the existing user-facing summary string.
- `PlayerInventory` stores `LastBattleRewardResult` and emits `OnBattleRewardResultGranted` for future save/backend integration.

### Local Saves and Account Buff Upgrades

- Added local JSON progression persistence through `PlayerInventory`.
- Save file path at runtime: `Application.persistentDataPath/dragon_dominion_progression.json`.
- Saved dragon fields:
  - `dragonId`
  - `level`
  - `bondLevel`
  - `bondXp`
  - `totalBattles`
  - `evolutionStage`
  - `skillLevel`
- Starter dragons are now only seeded when no valid local save exists.
- Added `PlayerInventory.ResetLocalProgression()` for testing reset-save flows.
- Added account progression data:
  - `essence`
  - `damageBuffLevel`
  - `attackSpeedBuffLevel`
  - `startingManaBuffLevel`
- Victory rewards now grant essence in addition to bond XP.
- Added upgradeable out-of-battle account buffs:
  - Damage Training: `+3%` dragon damage per level.
  - Attack Drill: `+2.5%` attack speed per level.
  - Mana Reserve: `+15` starting mana per level.
- Buffs are max level `10` and cost essence to upgrade.
- Added `Unity/Assets/Scripts/UI/AccountBuffPanel.cs` during the first buff pass; this battle-scene upgrade surface was later superseded by the Profile panel.
- Account buffs are saved immediately after upgrade and loaded on the next play session.

### Main Menu Profile Screen

- Added generated `Unity/Assets/Scenes/MainMenu.unity`.
- `SceneBootstrapper` now generates both `BattleScene.unity` and `MainMenu.unity`, and registers both in Unity build settings.
- Added `Unity/Assets/Scripts/UI/MainMenuController.cs`.
- Main menu actions:
  - `Battle`: loads `BattleScene`.
  - `Profile`: opens the progression/profile panel.
  - `Quit`: exits the application.
- Added `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`.
- Added `Unity/Assets/Prefabs/UI/ProfileProgressionPanel.prefab` generation.
- Profile panel shows:
  - essence
  - account buff levels
  - owned dragons
  - selected dragon bond level, bond XP, battles, evolution stage, skill level
  - base attack vs boosted attack
  - base attack speed vs boosted attack speed
- Account buff upgrades now live in the Profile panel rather than the battle HUD.
- The battle scene no longer instantiates `AccountBuffPanel`; battle earns rewards, Profile spends them.
- `SceneBootstrapper` no longer creates the old `AccountBuffPanel.prefab` upgrade surface.
- Added `Unity/Assets/Scripts/Core/ProgressionApiDtos.cs` with snake_case backend-facing DTOs for future `/api/v1/` persistence.
- `PlayerInventory.BuildApiDto()` converts the current local progression state into the backend-facing DTO shape.

Generation fix:

- Fixed `ResourceManager` so serialized default fields no longer call `PrototypeBalance`/`Resources.Load` during MonoBehaviour construction.
- Fixed `SafeAreaPanel` and `SceneBootstrapper` so `SafeAreaRoot` always has a `RectTransform` before safe-area layout runs.
- If `MainMenu.unity` is missing after this pass, rerun `Dragon Dominion > Build Battle Scene` from the open Unity editor; batchmode cannot run while the project is already open.

### Progression Persistence Service Boundary

- The user buff upgrade currency is `Essence`.
- Essence is earned from battle completion and victory rewards, then spent on Profile account buff upgrades.
- Added `Unity/Assets/Scripts/Core/IProgressionPersistenceService.cs`.
- Added `Unity/Assets/Scripts/Core/LocalProgressionPersistenceService.cs`, which wraps the current local JSON save/load path.
- Added `Unity/Assets/Scripts/Core/ApiProgressionPersistenceService.cs` as an API-ready disabled shell for future `/api/v1/progression` and `/api/v1/progression/battle-rewards` calls.
- Added `Unity/Assets/Scripts/Core/ProgressionPersistenceResult.cs`.
- `PlayerInventory` now saves, loads, and syncs battle rewards through the persistence service boundary instead of directly owning file I/O.
- Profile now displays sync status:
  - `Local`
  - `Saving...`
  - `Saved`
  - `Loaded`
  - `Sync failed`
- `ProfileProgressionPanel` creates a sync-status text field at runtime if an older preserved prefab does not have one wired yet.

Battle dragon list regression fix:

- Root cause: the generated `MainMenu.unity` could contain a persistent `PlayerInventory` with null `_starterDragons` references.
- That empty persistent inventory survived into `BattleScene`, caused the correctly wired Battle inventory to destroy itself, and left `DragonCollectionPanel` with no owned dragons to render.
- `PlayerInventory` now lets an incoming duplicate inventory pass valid starter dragon references into the persistent singleton before the duplicate destroys itself.
- If the persistent inventory has no owned dragons when it absorbs valid starters, it seeds the starter dragons and refreshes listeners.
- `SceneBootstrapper.BuildMainMenuScene` now reloads Phase 1 dragon assets before wiring `PlayerInventory`, so regenerated Main Menu scenes should serialize valid starter dragon references.

Readable font pass:

- Added `Unity/Assets/Scripts/UI/RuntimeFontScaler.cs` and `RuntimeFontScaleMarker.cs`.
- Existing runtime UI now scales labels upward without requiring prefab regeneration:
  - Battle HUD
  - Dragon placement cards
  - Victory/defeat panel
  - Main Menu
  - Profile progression panel
- `ResponsiveBattleUILayout` now preserves larger button and victory summary font sizes during layout refreshes.
- `SceneBootstrapper` now generates larger default label, button, and placement-card text for future regenerated scenes and prefabs.

Profile Dragons menu and battle loadout:

- Profile now acts as the Dragons menu for collection review and battle roster selection.
- Added a 6-dragon battle loadout cap:
  - `PlayerInventory.MaxEquippedDragons = 6`
  - `PlayerInventory.EquippedDragonIds`
  - `PlayerInventory.GetBattleDragons()`
  - `PlayerInventory.TryToggleEquipDragon(...)`
- New and migrated saves auto-equip the first available owned dragons up to 6 slots.
- `PlayerProgressionSaveData` now stores `equippedDragonIds`.
- `PlayerProgressionApiDto` now exposes `equipped_dragon_ids` for future backend persistence.
- `DragonCollectionPanel` now populates placement cards from equipped battle dragons only, not the entire owned collection.
- Battle completion bond XP now rewards equipped battle dragons only; Essence remains an account reward.
- Profile dragon list now marks equipped dragons with `[E]` and shows `Battle Loadout: X/6`.
- Profile selected dragon details now show rarity, element, class, role/range, active skill, bond XP, boosted attack, boosted attack speed, HP, armor, and account buff effects.
- Profile now includes an Equip/Unequip button. If 6 dragons are already equipped, reserve dragons show `Loadout Full`.
- Profile now includes a selected dragon portrait surface. It uses the dragon portrait when available and a neutral panel when art is missing.
- `SceneBootstrapper` now wires the Profile portrait and Equip button for newly generated `ProfileProgressionPanel.prefab`.

Dragon summoning:

- Added `Dragon Summon Tickets` as the item used to summon dragons.
- Fresh/reset local progression starts with `PlayerProgression.StartingSummonTickets = 6`.
- Victory rewards grant `+1` Dragon Summon Ticket in addition to Essence and bond XP.
- `PlayerProgressionSaveData` now stores `summonTickets`.
- `PlayerProgressionApiDto` now exposes `summon_tickets` for future backend persistence.
- `PlayerInventory.TrySummonDragon(...)` spends 1 Dragon Summon Ticket and adds one unowned dragon from the available Phase 1 dragon definitions.
- Summoned dragons are auto-equipped if the battle loadout has fewer than 6 dragons.
- If all available dragons are already owned, summoning is blocked with `No new dragons are available to summon`.
- Profile now includes:
  - summon ticket count
  - last summon result
  - `Summon Dragon` button
- Existing generated Profile prefabs are supported by runtime fallback controls, so regeneration is recommended but not strictly required for editor testing.
- `PlayerInventory` can recover missing starter dragon references in Unity Editor play mode by loading `DragonDefinition` assets from `Assets/ScriptableObjects/Dragons`.

Dragons menu selection fix:

- Root cause: the owned dragon collection in Profile was rendered as plain text, so players could only change selection with Prev/Next.
- `ProfileProgressionPanel` now creates clickable dragon row buttons in `DragonButtonContainer`.
- Clicking a dragon row selects that dragon and refreshes the detail/portrait/equip button.
- The selected dragon row is highlighted.
- `SceneBootstrapper` now creates and wires `DragonButtonContainer` for regenerated Profile prefabs.

Logged-in hub, Gems, Store, and chests:

- Gems are now wired as an account progression currency instead of only a battle `ResourceManager` runtime field.
- `PlayerProgression` now tracks:
  - `Gold`
  - `Gems`
  - `ChestSlots`
- Fresh/reset progression starts with `250` Gems.
- `PlayerProgressionSaveData` now stores `gold`, `gems`, and `chestSlots`.
- `PlayerProgressionApiDto` now exposes `gold`, `gems`, and `chest_slots`.
- Added chest slot progression:
  - 4 chest slots
  - empty slots can receive battle chest drops
  - Common chest: 3 hours
  - Rare chest: 8 hours
  - Epic chest: 12 hours
  - Legendary chest: 24 hours
- Chest rewards are now real local account rewards:
  - Common: 120 gold, 5 gems
  - Rare: 300 gold, 15 gems
  - Epic: 650 gold, 40 gems
  - Legendary: 1500 gold, 120 gems
- `PlayerInventory` now exposes chest actions:
  - `TryStartChestUnlock(slotIndex, out message)`
  - `TryOpenChest(slotIndex, out message)`
  - `TryOpenChest(slotIndex, out ChestRewardResult reward, out message)`
- `PlayerProgression.TryAwardBattleChest(...)` fills the first empty chest slot.
- Victory battle rewards now attempt to award one chest:
  - Common: 60%
  - Rare: 30%
  - Epic: 9%
  - Legendary: 1%
- If all 4 chest slots are occupied, victory reward text reports `chest slots full`.
- Opening a chest now empties that slot so future battle victories can place a new chest there.
- `BattleRewardResult` and `BattleRewardApiDto` now include chest award fields for future backend sync:
  - `chestAwarded` / `chest_awarded`
  - `chestSlotIndex` / `chest_slot_index`
  - `chestRarity` / `chest_rarity`
- Added `ChestRewardResult` for structured chest opening rewards.
- Main Menu chest buttons now show:
  - chest rarity
  - locked/unlocking/ready state
  - reward preview
  - tap action
- Chest countdown labels refresh while the Main Menu is open.
- Opening a ready chest now displays a reward popup, updates Gold/Gems immediately, and frees the slot.
- `SceneBootstrapper` now creates and wires `ChestRewardPanel` for regenerated `MainMenu.unity`.
- `PlayerInventory.AddStoreGems(amount)` remains as a development helper, but Store UI no longer uses it.
- Added `Unity/Assets/Scripts/UI/StorePanel.cs`.
- Store button now opens a Store panel instead of directly granting Gems.
- Added store/IAP validation boundary:
  - `GemStoreCatalog`
  - `StorePurchaseRequest`
  - `StorePurchaseResult`
  - `IStorePurchaseService`
  - `IIapReceiptValidator`
  - `EditorMockIapPurchaseService`
  - `LocalIapReceiptValidator`
  - `ApiIapReceiptValidator`
- Store purchases no longer grant Gems directly from UI.
- `StorePanel` now calls `PlayerInventory.PurchaseGemPackAsync(productId)`.
- Gems are granted only after the purchase result returns `success=true` and `validated=true`.
- Current editor behavior uses mock receipts validated locally:
  - receipt format: `editor_mock_receipt:{productId}:{gems}`
  - product ID and Gem amount must match `GemStoreCatalog`
- `ApiIapReceiptValidator` is the future backend receipt validation shell for app store / play billing receipts.
- Store panel includes mock Gem packs with future product IDs:
  - `com.dragondominion.gems.small` - Small Gem Pouch - 500 Gems - `$0.99`
  - `com.dragondominion.gems.medium` - Gem Bundle - 1200 Gems - `$1.99`
  - `com.dragondominion.gems.large` - Dragon Hoard - 3000 Gems - `$4.99`
  - `com.dragondominion.gems.epic` - Epic Vault - 6500 Gems - `$9.99`
  - `com.dragondominion.gems.legendary` - Legendary Treasury - 14000 Gems - `$19.99`
- Store purchases are currently editor/mock purchases, but still pass through receipt validation before Gems are added.
- Added chest speed-up with Gems:
  - unlocking chest labels show the current speed-up Gem cost
  - tapping an unlocking chest spends Gems and completes the timer immediately
  - speed-up cost is `ceil(remaining minutes / 10)`, minimum `1` Gem
  - tapping the ready chest after speed-up opens it and grants rewards
- `SceneBootstrapper` now creates and wires the Store panel for regenerated `MainMenu.unity`.
- Main Menu is now a logged-in hub layout:
  - top account currencies
  - center Battle button
  - side Events text
  - chest slots below Battle
  - bottom nav: Dragons, Profile, Battle, Clan
  - Store button for future Gem purchases
- Existing simple `MainMenu.unity` scenes are supported by runtime fallback UI creation in `MainMenuController`; regeneration is recommended for cleaner serialized wiring.

Events, backend sync shell, Clan shell, and dragon detail polish:

- Added saved prototype event claim state:
  - `EventClaimSaveData`
  - `PlayerProgressionSaveData.eventClaims`
  - `PlayerProgressionApiDto.event_claims`
- Added `PrototypeEventCatalog` with three prototype events:
  - `Daily Hunt`: claimable once per UTC day, rewards `+250 Gold` and `+35 Essence`
  - `Gem Rush`: preview-only short challenge, previews `+25 Gems`
  - `Clan Raid`: locked placeholder until Clan/social backend exists
- Added `EventsPanel`.
- Main Menu event text is clickable and opens the Events panel.
- Events panel shows event description, status, and reward preview.
- Daily Hunt claim state is saved locally and blocks repeat claims on the same UTC day.
- Added `ClanPanel` as a locked social shell.
- Clan bottom nav now opens the Clan shell instead of only showing a toast.
- Added backend/auth sync shell:
  - `AuthSessionData`
  - `AuthRequestDto`
  - `AuthResponseDto`
  - `ApiAuthService`
  - `PlayerInventory.LoginWithDeviceAndUseApiAsync(...)`
  - `PlayerInventory.UseLocalPersistence()`
- `ApiProgressionPersistenceService` now has bearer-token request scaffolding for:
  - `GET /api/v1/progression`
  - `PUT /api/v1/progression`
  - `POST /api/v1/progression/battle-rewards`
- Local JSON persistence remains the default until a backend URL and auth session are provided.
- Added `UnityIapReceiptCaptureService` as the compile-safe Unity IAP receipt capture adapter shell.
- Real Unity IAP capture still requires installing `com.unity.purchasing`; until then, Store uses validated editor mock receipts.
- Profile dragon detail received a visual polish pass:
  - rarity-colored portrait fallback panel
  - dragon art caption with name, rarity, and element
- `SceneBootstrapper` now creates and wires Events and Clan panels for regenerated `MainMenu.unity`.

Main Menu and Profile layout cleanup:

- Profile panel now applies a stable two-zone runtime layout:
  - left side: account summary, sync status, summon status, loadout header, clickable dragon rows
  - right side: selected dragon portrait, art caption, detail text, previous/next/equip controls
  - bottom row: account buff upgrades, reset save, close
- Removed the duplicate full text dragon list when clickable dragon rows are present; the text area now only shows `Battle Loadout: X/6`.
- Dragon row container now has a fixed readable area so rows do not overlap the detail panel.
- Main hub no longer prints the full chest summary over the Battle button.
- Main hub chest area now uses a compact `Chest Slots` heading and relies on the individual chest cards for state.
- Main hub Battle button, message text, chest cards, and Events card were re-anchored to reduce overlap in Free Aspect and mobile-like views.
- `SceneBootstrapper` mirrors these Main Menu and Profile layout anchors for regenerated scenes/prefabs.

Economy UX confirmation pass:

- Added a Main Menu confirmation panel for Gem chest speed-ups.
- Tapping an unlocking chest now asks before spending Gems.
- Confirming spends Gems and completes the timer; cancelling leaves the chest untouched.
- Added a shared reward popup path on `MainMenuController.ShowRewardMessage(...)`.
- Daily Hunt claims now show a reward popup with the claimed reward.
- Events panel now shows clearer disabled states:
  - Gem Rush is labeled preview-only
  - Clan Raid is labeled locked
  - claimed Daily Hunt button changes to `Claimed`
- Store panel now has purchase state feedback:
  - pending validation text
  - pack button label changes to `Validating...`
  - success text shows `Purchase validated` and the Gem amount
  - failed purchases keep the failure message
- Main hub now shows a sync/status line (`Save: Local`, `Saving...`, etc.) next to the currency header.
- `SceneBootstrapper` now wires the hub sync text and confirmation panel for regenerated `MainMenu.unity`.

Profile/Dragons nav split:

- `ProfileProgressionPanel` now has explicit `Profile` and `Dragons` modes.
- Bottom nav `Profile` opens account progression only:
  - account summary
  - sync status
  - account buff upgrades
  - reset/close controls
- Bottom nav `Dragons` opens collection management only:
  - summon ticket status
  - pinned `Summon Dragon` button
  - battle loadout count
  - clickable dragon rows
  - selected dragon portrait/detail
  - previous/next/equip controls
- Switching from Dragons to Profile now changes panel mode instead of closing the shared panel.
- Follow-up compile fix: `MainMenuController` now sends `SetProfileMode` / `SetDragonsMode` messages after activating the shared panel instead of referencing the panel's nested mode enum directly, avoiding stale Unity type metadata errors while scripts reimport.

Session loop and progression polish:

- Battle end flow now grants progression rewards on defeat as well as victory, using the existing battle reward pipeline.
- The victory/defeat panel's return button is labeled `Main Menu`.
- Main Menu shows a one-time battle reward popup after returning from battle, including the latest reward summary.
- The Battle buttons now open a prototype stage selector instead of immediately loading combat:
  - `Chapter 1`
  - `Stage 1-1: Dominion Road`
  - 5-wave reward preview
  - `Start Stage` / `Close`
- Dragons mode now has selected-dragon progression actions:
  - `Level Up` spends Gold and raises dragon level up to 20.
  - `Train Bond` spends Essence and grants bond XP.
  - `Evolve` spends Essence, requires Bond 5, and advances evolution stage until Titan.
- Added progression spend helpers for Gold and Essence.
- Chest interactions now show clearer popups:
  - starting an unlock shows rarity, reward preview, and status
  - speed-up completion shows a `Chest Ready` popup
  - opening a chest shows rewards and that the slot is empty
- Summoning now opens a dedicated confirmation/result panel from Dragons mode instead of immediately spending the ticket.
- The summon panel shows ticket count, last summon result, `Use Ticket`, and `Close`.

Chapter 1 stage progression and dailies:

- Added `StageCatalog` with four Chapter 1 prototype stages:
  - `Stage 1-1: Dominion Road`
  - `Stage 1-2: Ember Crossing`
  - `Stage 1-3: Frost Gate`
  - `Stage 1-4: Storm Watch`
- Player progression now saves:
  - current selected stage
  - highest unlocked stage index
  - cleared stage IDs
- Main Menu stage selector now shows a small stage list with:
  - locked/open/current/cleared state
  - enemy theme
  - recommended level
  - difficulty multiplier
  - reward multiplier
  - chest rarity preview
- Clearing a stage unlocks the next Chapter 1 stage.
- Battle difficulty now scales enemies by the selected stage's difficulty multiplier.
- Wave gold/mana rewards and battle Essence rewards now scale by the selected stage's reward multiplier.
- Victory chest rarity rolls improve on later stages.
- Added `DailyObjectiveCatalog` with three saved daily objectives:
  - Win 1 battle
  - Open 1 chest
  - Summon 1 dragon
- Daily objective progress resets by UTC date and is saved locally.
- Player actions now track daily objective progress:
  - battle victory advances `Win 1 battle`
  - chest opening advances `Open 1 chest`
  - successful summon advances `Summon 1 dragon`
- Events panel now includes daily objective cards alongside prototype events, with claimable rewards when objectives are ready.
- Main Menu Events text now summarizes daily objective status.
- Follow-up compile fix: renamed the stage selector fallback `buttons` local to avoid a C# local-scope collision in `MainMenuController.EnsureStageSelectPanel()`.

Stage missions, stars, and stage-specific battle content:

- Chapter 1 stages now carry mission and pressure data:
  - extra enemies per wave group
  - spawn-rate multiplier
  - lives requirement
  - leak limit
  - dragon placement limit
- Wave spawning now uses the selected stage to vary battle pressure:
  - later stages add more enemies per spawn group
  - later stages reduce spawn interval
  - existing stage difficulty multiplier still scales enemy stats
- Battle results now evaluate stage missions on victory:
  - 1 star for clearing the stage
  - +1 star for meeting the lives requirement
  - +1 star for meeting both leak and dragon-placement goals
- Player progression now saves best stars per stage.
- Stage selector now shows best stars and objective text for the selected stage.
- Battle reward results now include:
  - stage ID/title
  - stars earned
  - best stars
  - first-clear flag
  - objective summary
  - stage bonus rewards
- First clear bonuses now grant Gold, Essence, and Gems.
- 3-star clears now grant extra Gems and Essence, with a prototype summon-ticket chance.
- Victory panel now shows the stage title, stars earned, best stars, and mission objective breakdown.
- API DTOs now include stage-star saves and stage result reward fields for future backend sync.

Dragon roster strategy pass:

- Added prototype dragon role tags:
  - Damage
  - Slow
  - AoE
  - Support
  - Anti-Shield
  - Anti-Flying
- Added `DragonRoleUtility`, which derives prototype roles from dragon class, element, base stats, active skill, AoE fields, and status-effect keywords.
- Chapter 1 stages now declare recommended role coverage.
- Stage selector now shows recommended roles in the objective text.
- Stage selector warns before starting if the current battle loadout is missing recommended role coverage; pressing `Start Stage` again continues anyway.
- Dragons menu now includes role filter buttons:
  - All
  - Damage
  - Slow
  - AoE
  - Support
  - Anti-Shield
  - Anti-Flying
- Dragon rows now show derived role tags.
- Selected dragon details now show:
  - derived roles
  - compare against equipped loadout average attack/speed/range
  - current loadout coverage summary
- Added prototype quick loadout presets:
  - Balanced
  - Boss
  - Fast Enemies
  - Shield Break
- Presets auto-equip up to 6 owned dragons based on role priorities, then fill remaining slots with owned dragons.
- Added Unity `.meta` files for the new role tag and role utility scripts so their asset GUIDs are stable.

## Backend Sync / Store / Events Pass - 2026-05-29

Implemented the first backend-backed account and live-ops shell:

- Added device auth at `POST /api/v1/auth/device`.
  - Creates or reuses a `Player` by `device:{device_id}`.
  - Returns the Unity-compatible `{ success, data, error }` envelope with `playerId`, `accessToken`, `refreshToken`, and `expiresUtcTicks`.
- Added JWT bearer authentication for new `/api/v1/*` gameplay routes.
- Added progression sync endpoints:
  - `GET /api/v1/progression`
  - `PUT /api/v1/progression`
  - `POST /api/v1/progression/battle-rewards`
  - Progression saves are stored as raw Unity JSON so the current `JsonUtility` client path remains compatible.
- Added server-side IAP validation at `POST /api/v1/iap/validate`.
  - Validates known Gem product IDs against the Unity `GemStoreCatalog`.
  - Stores transaction IDs idempotently.
  - Grants Gems server-side on the `Player` record.
  - Unity `ApiIapReceiptValidator` now sends bearer tokens and uses `/api/v1/iap/validate` after device login.
- Added authenticated Events endpoints:
  - `GET /api/v1/events`
  - `POST /api/v1/events/{eventId}/claim`
  - `POST /api/v1/events/{eventId}/score`
  - Daily Hunt claims persist once per UTC day; Gem Rush has score tracking; Clan Raid remains locked.
- Added authenticated Clan shell:
  - `GET /api/v1/clan/me`
  - Returns a locked shell response until real clan membership/social backend work begins.
- Added backend regression tests in `Backend/DragonTD.Tests`.
  - Coverage includes auth creation/reuse, protected progression sync, battle reward sync, IAP validation/idempotency, event claims/scores, and clan lock state.
- Added EF Core migration `AccountSyncStoreEventsClan` for the current backend schema, including player progression, IAP receipt, event score, and event claim tables.
- Added `com.unity.purchasing` to `Unity/Packages/manifest.json`.
- Added `Unity.Purchasing` to the `DragonTD` assembly definition.
- Replaced the `UnityIapReceiptCaptureService` placeholder with a Unity IAP 5 `StoreController` bridge for non-editor builds:
  - connects to the store
  - fetches Gem catalog products as consumables
  - starts product purchases
  - captures pending order receipts and transaction IDs
  - sends receipts through `IIapReceiptValidator`
  - confirms the purchase only after validator success
  - keeps editor builds on the fast mock receipt purchase path

Verification:

```powershell
dotnet test Backend\DragonTD.sln -v minimal
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\DragonTD\DragonTD\Unity' -logFile 'D:\DragonTD\DragonTD\unity-iap-real-callbacks.log'
```

Result:

- Backend tests: `12 passed`.
- Unity script compilation: successful batchmode compile. Existing warnings remain in `PlayerInventory` for fire-and-forget saves and in `ResourceManager` for unused serialized defaults.

Remaining next steps:

1. Run the EF migration against a local PostgreSQL database and verify Unity API sync against `https://localhost:5001`.
2. Add store-provider credential validation for real Google Play / Apple receipts; the current backend accepts the editor mock receipt format for automated prototype tests.
3. Add Unity PlayMode/manual regression coverage for menu navigation, API sync status, store purchases, event claims, chest flows, save/load, and battle return flow.
4. Expand Events from static prototype definitions into dated calendars and scored challenge reward tiers.
5. Implement full Clan membership and Clan Raid after account persistence has been exercised against a real database.

## Battle Deck Layout Pass - 2026-05-29

Changed the cramped Dragons menu toward a Raid Rush-inspired deck/collection layout:

- Dragons mode now uses a `BATTLE DECK` presentation instead of the old profile-detail layout.
- Equipped dragons are shown as six expanded deck slots across the top.
- The large upper-left selected portrait is hidden in Dragons mode so equipped slots get the space.
- Added collection tabs:
  - `Dragons`
  - `Skills`
  - `Parts`
  - `Items`
- `Dragons` shows owned dragons as card tiles instead of text rows.
- `Skills`, `Parts`, and `Items` show clean placeholder cards until those systems are implemented.
- Selected dragon actions remain available near the lower detail strip:
  - Equip/Unequip
  - Level Up
  - Train Bond
  - Evolve
- Profile mode remains the account progression screen.
- Follow-up overlap fix:
  - Hub Battle/Events/chest/title UI is hidden while overlay panels are open.
  - Main currency, Store, and bottom nav remain visible.
  - The old static `Profile & Progression` header is hidden in Dragons mode.
  - Dragons detail/actions were lifted above the bottom nav.

Verification note:

- Unity batchmode compile could not run because this project was already open in another Unity instance.
- Manual visual verification should be done in the open editor:
  - Open `MainMenu.unity`
  - Press Play
  - Open `Dragons`
  - Check equipped slots, tabs, owned dragon card grid, selected dragon detail/actions, and panel overlap.

## DB Migration + API Smoke Test — 2026-05-29

Migrations applied to local PostgreSQL (`postgresql-x64-17`), password `amp123!`:
- `AccountSyncStoreEventsClan` — created all baseline tables (Players, DragonDefinitions, PlayerDragons, PlayerProgressionStates, IapPurchaseReceipts, EventChallengeStates, PlayerEventClaims) and seeded 3 dragon definitions.
- `AddEventDefinitions` — created `EventDefinitions` table and seeded 3 prototype events (Daily Hunt, Gem Rush, Clan Raid).
- `AddClanMembership` — created `Clans` and `ClanMembers` tables.

API started on `https://localhost:60733` / `http://localhost:60734` (dynamic port from launchSettings.json).

Smoke test results:
- Device auth (`POST /api/v1/auth/device`): PASS — JWT returned, token starts `eyJhbGciOiJIUzI1NiIs`
- Progression GET (`GET /api/v1/progression`): PASS — `success=True`
- Events GET (`GET /api/v1/events`): PASS — 3 events returned (Daily Hunt, Gem Rush, Clan Raid)

## Dragons Drag-and-Drop Deck — 2026-05-29

Replaced Prev/Next navigation buttons and Equip/Unequip button in the Dragons menu with drag-and-drop:

- New `Unity/Assets/Scripts/UI/DragCardHandler.cs` — IBeginDragHandler/IDragHandler/IEndDragHandler on collection card tiles; dims source to alpha 0.4 during drag; spawns semi-transparent ghost on root Canvas that follows the pointer; OnDisable cleans up ghost to prevent leaks.
- New `Unity/Assets/Scripts/UI/DeckSlotDropHandler.cs` — IDropHandler on deck slots; empty slot → equip; occupied slot → TrySwapEquipped.
- `PlayerInventory` — added `FindOwnedDragonById` (public wrapper) and `TrySwapEquipped` (position-swap when both dragons already equipped; remove+add otherwise).
- `ProfileProgressionPanel` — removed Prev/Next/Equip button runtime creation and wiring; `TapDeckSlot` selects + unequips in one tap with single Refresh via event.

Manual verification: open `MainMenu.unity` → Play → Dragons → confirm drag-and-drop equip, swap, and tap-unequip work.

## IAP Platform Receipt Validation — 2026-05-29

Backend now routes IAP receipts by platform:

- `Backend/DragonTD.API/Services/IIapPlatformReceiptValidator.cs` — interface + result record.
- `Backend/DragonTD.API/Services/GooglePlayReceiptValidator.cs` — RSA-SHA1 signature validation; reads `GooglePlay:PublicKey` config; returns 503 when unconfigured.
- `Backend/DragonTD.API/Services/AppleReceiptValidator.cs` — calls Apple `/verifyReceipt` with sandbox fallback on status 21007; reads `Apple:SharedSecret` config; returns 503 when unconfigured.
- `IapController` — routes by `platform` field: Editor uses mock path; GooglePlay/AppleAppStore dispatch to registered validators; unknown platform → 400.
- Validators registered as singletons in `Program.cs`.

Production credentials needed: set `GooglePlay:PublicKey` (base64 SubjectPublicKeyInfo) and `Apple:SharedSecret` env vars before deploying.

Backend tests: 24 passed (includes 3 new IAP platform tests).

## Unity PlayMode Regression Suite — 2026-05-29

Added `Unity/Assets/Tests/PlayMode/InventoryProgressionTests.cs` (6 tests):

- `TrySwapEquipped_SwapsIncomingWithExistingSlot` — verifies position swap behavior.
- `TrySwapEquipped_NonEquippedExisting_ReturnsFalse` — verifies error path.
- `ChestAward_VictoryFillsFirstEmptySlot` — verifies TryAwardBattleChest.
- `ChestAward_AllSlotsFull_ReturnsFalse` — verifies capacity cap.
- `DailyObjective_WinBattleAdvancesProgress` — smoke test for objective tracking.
- `SaveLoad_RoundTripPreservesGoldAndEssence` — Gold + Essence survive JSON round-trip.

All type signatures verified against codebase before committing.

Manual regression items (require scene): menu nav, API sync status label transitions, store purchase flow, event claim popup, battle return reward popup.

## Events Dated Calendars + Scored Tiers — 2026-05-29

Events moved from a hardcoded static array to a DB-backed model with date filtering and scored challenge tiers:

- `Backend/DragonTD.API/Models/EventDefinitionModel.cs` — `EventDefinition` (id, eventId, displayName, description, eventType, startUtc, endUtc, goldReward, essenceReward, gemReward, rewardTiersJson) and `EventRewardTier`.
- `AppDbContext` — `EventDefinitions` DbSet + HasData seed for 3 prototype events.
- `EventsController` — `List` filters by active date window; `Claim` dispatches to `ClaimDaily` (per-UTC-day dedup) or `ClaimScoredTier` (one-time, highest qualifying tier, DateOnly.MaxValue sentinel).
- `TestApiFactory` — `CreateHost` override calls `EnsureCreated()` so HasData seeds apply to in-memory test DB.
- Migration `AddEventDefinitions` generated.

Backend tests: 24 passed (includes 4 new events tests: date filter, scored tier claim, no-score 400, duplicate 409).

## Clan Membership + Clan Raid — 2026-05-29

Full clan system implemented:

- `Clan` model: id, name (unique), tag (unique), ownerId, description, memberLimit=30, raidScore.
- `ClanMember` model: composite unique (clanId, playerId); role (Owner/Officer/Member); raidContribution.
- FK: `Clan.OwnerId → Player` (Restrict delete — must disband before deleting owner); `ClanMember.ClanId` (Cascade — disbanding removes members).

Endpoints:
- `POST /api/v1/clan` — create; 409 if already in clan or name taken.
- `GET /api/v1/clan/me` — clan data + members if in clan; `locked=false, status=not_in_clan` otherwise.
- `GET /api/v1/clan/{id}` — public clan info.
- `POST /api/v1/clan/{id}/join` — 409 if in clan; 400 if full.
- `POST /api/v1/clan/{id}/leave` — owner leaving disbands the clan (cascade).
- `POST /api/v1/clan/raid/contribute` — adds score to member + clan total.
- `GET /api/v1/clan/raid/leaderboard` — top 20 by RaidScore.

Migration `AddClanMembership` generated. Backend tests: 24 passed (includes 5 new clan tests).

## Phase 2 — Waves, Dragons, Gacha — 2026-05-29

### Waves 5 → 15

SceneBootstrapper now generates 15 waves (Wave01–Wave15). Waves 06–15 added with
escalating enemy counts (up to 30 runners + 20 of each other type in Wave15) and
faster spawn intervals. Gold rewards scale 700→2600, mana rewards 380→1100.

Run `Dragon Dominion > Build Battle Scene` in the Unity editor to generate the 10 new
wave assets and update BattleScene.unity.

### Dragons 7 → 10

Three new dragons added to `Phase1DragonData.All`:
- `emberveil_008` — Epic, Fire, Celestial class — 1200 HP, 310 ATK, AoE active skill "Celestial Fire"
- `tideclaw_009` — Rare, Water, Frost class — 1050 HP, 200 ATK, AoE active skill "Whirlpool"
- `zephyrwing_010` — Uncommon, Wind, Storm class — 700 HP, 165 ATK, single-target active "Gust Burst"

Run `Dragon Dominion > Build Battle Scene` to generate dragon assets, skill assets, and prefabs.

### Gacha System

GachaSystem (already implemented in `Scripts/Summoning/`) is now wired into the summon flow:

- Pity persisted in `PlayerProgressionSaveData`: `gachaPullsSinceLastEpic`, `gachaTotalPulls`
- Pity synced to/from `GachaSystem.PityTracker` via `PlayerProgression.SyncGachaPity` and `GachaSystem.RestorePity`
- `SummonPool_Phase1.asset` created at `Assets/Resources/SummonPool_Phase1.asset` on scene regeneration, containing all 10 Phase 1 dragons with standard rates (Common 40%, Uncommon 30%, Rare 20%, Epic 7%, Legendary 2.5%, Mythic 0.5%)
- Soft pity at pull 50 (Epic+ rates tripled), hard pity at pull 100 (force Mythic), 10-pull Rare+ guarantee
- Pull-before-spend pattern: currency spent only after a dragon is confirmed available

New `PlayerInventory` methods:
- `TrySummonDragon` (ticket) — uses GachaSystem with fallback to PickSummonDragon if pool not loaded
- `TrySummonDragonWithGems` — spends 300 Gems per pull
- `TryTenPullWithGems` — spends 2700 Gems for 10 pulls, records DailyObjective per summoned dragon

Summon panel updated: shows pity counter, soft-pity indicator, gem costs, and "300 Gems" / "2700 (10x)" buttons.

Pending: Run `Dragon Dominion > Build Battle Scene` to create `SummonPool_Phase1.asset`.

## Tile-Based Map System — 2026-05-30

- Replaced the single background image with a tile-based map: each tile type has its own sprite.
- `MapDefinition` ScriptableObject drives the grid via a text grid string (B=buildable, P=path, H/M/F/S=bonus tiles, X=blocked). Visual click-to-paint editor in the Inspector (`MapDefinitionEditor`), with a **Save & Build Battle Scene** button.
- Path auto-tiling: path sprites picked by neighbor bitmask (straight H/V + 4 corners), fields on `MapDefinition`; `GridManager.ApplyAutoTiling` resolves per tile.
- Tile sprite PPU auto-corrected from PNG header so each tile fills exactly 1 world unit; tile scale 1.01 to kill subpixel gaps; Point filter + Clamp wrap.
- Sorting: tiles −2, enemies 1, towers 2. Tile collider disabled when occupied so clicks reach the placed dragon.
- Waypoints auto-computed from the painted path; removed the hardcoded path/waypoint arrays from SceneBootstrapper.

## Battle Polish, Chapter 2, Nakama — 2026-05-30

- Tower portraits now set on runtime `DragonTower.Setup` (was build-time only); color fallback when no portrait.
- 10-pull shows a result screen (`SummonResultPanel`) listing all 10 dragons by rarity; `PlayerInventory.LastTenPullResults` exposes the structured results.
- Range rings and level badges confirmed pre-existing (`TowerSelectionManager.DrawRangePreview` / `DragonTower.EnsureLevelBadge`).
- Gacha pity persists to backend via the existing JSON-blob progression sync (verified by `ProgressionPityTests`; no migration needed — progression is a raw `SaveJson` blob).
- Nakama device auth (`NakamaAuthService`) + battle-score leaderboard submission (`NakamaLeaderboardService`); config via `Resources/NakamaConfig.asset`. Requires a running Nakama server (dev defaults: 127.0.0.1:7350, key `defaultkey`). SDK added to `manifest.json` (`com.heroiclabs.nakama-unity` v3.14.0). Score submitted on victory in `GrantBattleCompletionRewards`.
- Chapter 2 (Ice Tundra): 3 new enemies — `IceShard` (fast/low HP), `FrostBrute` (slow/high armor), `GlacialShield` (shielded with `ShieldRegenDelay=4s` shield restore). New `EnemyData.ShieldRegenDelay` field; `EnemyBase.Tick` restores shields; `TrollEnemy.Tick` now calls `base.Tick`.
- Runtime chapter loading: `ChapterContent` SO bundles a `MapDefinition` + `WaveData[]`; static `ChapterContent.Active` overrides `GridManager`/`WaveManager` defaults. `GameManager._chapters` + `ApplyActiveChapter()` select the active chapter from the chosen stage (set in `SelectStageForNextBattle` and `StartBattle`). Chapter 1 keeps working (Active null → serialized arrays).
- `StageCatalog` gained a `chapter` field and 4 Chapter 2 stages (2-1 Frozen Pass … 2-4 Winter Throne).
- SceneBootstrapper generates `Chapter2Map.asset`, `Chapter2_Wave01`–`15`, `Chapter1Content.asset`/`Chapter2Content.asset`, the 3 ice enemy data/prefabs, and wires `GameManager._chapters`.
- Pending: run `Dragon Dominion > ★ Build Battle Scene` to generate the Chapter 2 assets; ice/snow tile sprites for `Chapter2Map` and 3 dragon portraits (emberveil_008, tideclaw_009, zephyrwing_010) are user-supplied art.

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

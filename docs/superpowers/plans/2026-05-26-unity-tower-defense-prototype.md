# Unity Tower Defense Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the existing Unity `BattleScene` a playable Dragon Dominion tower defense prototype with seven dragon placement cards and imported portrait assets.

**Architecture:** Extend the existing Unity C# architecture instead of replacing it. Dragon and skill data remain ScriptableObject-driven, editor factories regenerate assets and scene wiring, and runtime changes stay focused on reliable battle flow and UI updates.

**Tech Stack:** Unity 2022 LTS+ URP, C#, UnityEngine UI, ScriptableObjects, existing ASP.NET backend left untouched.

---

## File Structure

- Modify `Unity/Assets/Editor/SceneBootstrapper.cs`: regenerate all seven dragon prefabs, definitions, skills, starter inventory entries, waves, and UI card layout.
- Modify `Unity/Assets/Scripts/Dragons/Phase1DragonData.cs`: ensure Phase 1 static data has complete playable stats and skill metadata for all seven dragons.
- Modify `Unity/Assets/Scripts/UI/BattleHUD.cs`: update HUD immediately on enable and guard button wiring.
- Modify `Unity/Assets/Scripts/UI/DragonPlacementCard.cs`: make card setup resilient to missing optional fields and display mana with units.
- Modify `Unity/Assets/Scripts/UI/DragonCollectionPanel.cs`: verify it rebuilds cards from `PlayerInventory`.
- Modify `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`: prevent duplicate spawn coroutines and handle empty or invalid waves cleanly.
- Add or copy `Unity/Assets/Art/Dragons/*/portrait.png`: place supplied portraits at the paths declared in the design spec.

## Task 1: Import Dragon Portraits

**Files:**
- Create: `Unity/Assets/Art/Dragons/celestara_006/portrait.png`
- Create: `Unity/Assets/Art/Dragons/frostfang_002/portrait.png`
- Create: `Unity/Assets/Art/Dragons/magmaclaw_003/portrait.png`
- Create: `Unity/Assets/Art/Dragons/shadowfang_007/portrait.png`
- Create: `Unity/Assets/Art/Dragons/stonehide_005/portrait.png`
- Create: `Unity/Assets/Art/Dragons/tempest_glacion_004/portrait.png`
- Create: `Unity/Assets/Art/Dragons/voltaris_001/portrait.png`

- [ ] **Step 1: Copy provided PNG files**

Copy each user-provided dragon image from `C:\Users\LENOVO\Downloads` into the matching Unity asset folder using the mapping in `Unity/Assets/Art/Dragons/PORTRAITS.md`.

- [ ] **Step 2: Verify files exist**

Run: `Test-Path Unity\Assets\Art\Dragons\voltaris_001\portrait.png`

Expected: `True` for all seven portrait paths.

## Task 2: Complete Seven-Dragon Bootstrap Data

**Files:**
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`
- Modify: `Unity/Assets/Scripts/Dragons/Phase1DragonData.cs`

- [ ] **Step 1: Replace hardcoded three-dragon bootstrap loops with `Phase1DragonData.All`**

Use `Phase1DragonData.All` to create tower prefabs, normal attack skills, active skills where useful, dragon definitions, and starter inventory entries for every Phase 1 dragon.

- [ ] **Step 2: Bind portrait sprites and tower prefabs in generated `DragonDefinition` assets**

For each dragon ID, load `Assets/Art/Dragons/{id}/portrait.png` as a `Sprite` and assign it to `visualData.portrait`. Load `Assets/Prefabs/Dragons/{Name}Tower.prefab` and assign it to `visualData.hatchlingPrefab`.

- [ ] **Step 3: Keep generated tower prefabs simple and readable**

Create square/sprite placeholder tower prefabs with distinct colors by dragon element, attach `DragonTower`, wire `_projectilePrefab`, and add a `FirePoint` child.

## Task 3: Build A Multi-Wave Prototype

**Files:**
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`
- Modify: `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`

- [ ] **Step 1: Generate at least three waves**

Create `Wave01.asset`, `Wave02.asset`, and `Wave03.asset` with increasing enemy counts and rewards.

- [ ] **Step 2: Wire all generated waves into `WaveManager`**

Set `_waves` to the three generated assets so victory occurs after wave 3.

- [ ] **Step 3: Harden `WaveManager` state**

Track the active spawn coroutine and ignore `StartNextWave` calls while a wave is already spawning or active. If a wave contains zero valid enemies, complete it without leaving the game stuck.

## Task 4: Polish Runtime UI Reliability

**Files:**
- Modify: `Unity/Assets/Scripts/UI/BattleHUD.cs`
- Modify: `Unity/Assets/Scripts/UI/DragonPlacementCard.cs`
- Inspect: `Unity/Assets/Scripts/UI/DragonCollectionPanel.cs`

- [ ] **Step 1: Guard HUD button listeners**

Only add listeners when serialized buttons are non-null.

- [ ] **Step 2: Refresh HUD text immediately**

On enable and on state changes, refresh lives, wave, mana, and gold from current manager values.

- [ ] **Step 3: Make placement cards tolerate missing portraits**

If a portrait is missing, leave the existing placeholder image and still allow placement.

## Task 5: Generate And Verify The Unity Scene

**Files:**
- Modify generated assets under `Unity/Assets/ScriptableObjects`
- Modify generated prefabs under `Unity/Assets/Prefabs`
- Modify generated scene `Unity/Assets/Scenes/BattleScene.unity`

- [ ] **Step 1: Run Unity editor menu build path if possible**

Run the SceneBootstrapper menu item through Unity editor batch mode if a Unity executable is discoverable.

- [ ] **Step 2: Run compile verification**

Use Unity batchmode compile or an available project build command. Expected: no C# compiler errors.

- [ ] **Step 3: Manual play criteria**

Open `Unity/Assets/Scenes/BattleScene.unity`, press Play, place at least one dragon, start waves, confirm enemies lose health, wave reward updates resources, and victory appears after wave 3.

## Self-Review

The plan covers the approved scope: seven portraits, all seven dragons as placement cards, multi-wave playable battle, reliable state progression, and verification. No placeholders or deferred tasks are present. Type names match the current codebase names read from the Unity project.

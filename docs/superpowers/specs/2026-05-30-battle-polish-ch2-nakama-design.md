# Battle Polish, Chapter 2, Pity Sync, Nakama Auth — Design

Date: 2026-05-30

## Summary

Seven features across Unity client and .NET backend:
1. Range rings on selected towers
2. Level badges on placed towers
3. 10-pull gacha result screen
4. Tower portrait sprite (verify existing wiring)
5. Chapter 2 — Ice Tundra map + 3 new enemies + 15 waves
6. Backend gacha pity persistence
7. Nakama device auth + leaderboard score submission shell

---

## Feature 1: Range Rings

**File:** `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs` + `SceneBootstrapper.cs`

- Add a `LineRenderer` child to the tower prefab in `CreateDragonTowerPrefab`.
- 48-segment circle, radius = `DragonInstance.Range`, world-space, sorting order 3.
- Color = element color (reuse `DragonColorUtility.GetElementColor` if it exists; else a local element→color map in DragonTower).
- Line width 0.06, semi-transparent (alpha 0.65).
- Shown only when the tower is selected (`TowerSelectionManager` selects it); hidden otherwise.
- `DragonTower` exposes `SetRangeRingVisible(bool)`; selection manager calls it on select/deselect.
- Ring radius refreshes when tower upgrades (range can change).

## Feature 2: Level Badge

**File:** `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs` + `SceneBootstrapper.cs`

- Add a `TextMesh` child "LevelBadge" to tower prefab, local position `(0, 0.55, -0.1)`, sorting order 4.
- Text = `"LV{UpgradeLevel}"`, white, small character size (0.12), center-anchored.
- Updates whenever `UpgradeLevel` changes (in `TryUpgrade` and `Setup`).
- Fused towers show `"LV{level}"` using the existing display level (4 for fused).

## Feature 3: 10-Pull Result Screen

**Files:** Create `Unity/Assets/Scripts/UI/SummonResultPanel.cs`; modify `ProfileProgressionPanel.cs`

- A modal panel built at runtime (like other panels) showing the 10 pulled dragons.
- Layout: 2 rows × 5 columns of result cells; each cell shows dragon name + rarity, background tinted by rarity color.
- "OK" button dismisses the panel.
- `ProfileProgressionPanel` currently calls `TryTenPullWithGems(out _)` discarding results. Change to capture the `string[] messages` AND the actual `DragonDefinition[]`.
- Add `PlayerInventory.LastTenPullResults` (a `List<DragonDefinition>`) set by `TryTenPullWithGems`, so the UI can read structured results, not just strings.
- The result panel reads `LastTenPullResults` and renders cells.
- Single-pull (ticket / 300 gems) keeps the existing simple summary text — no result screen.

## Feature 4: Tower Portrait Sprite

**File:** `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs` (verify only)

- `Setup(DragonInstance)` should set the tower `SpriteRenderer.sprite` to `dragon.Definition.visualData.portrait` when non-null, white color; else fallback to element-colored square.
- This may already exist from the SceneBootstrapper prefab wiring. Verify; if the runtime `Setup` doesn't refresh the sprite, add it.
- No new art created here — relies on user-supplied transparent portraits.

## Feature 5: Chapter 2 — Ice Tundra

**Files:** `SceneBootstrapper.cs`, new `Chapter2Map.asset`, `StageCatalog.cs`, new enemy data/prefabs, Wave assets.

### Map
- New `MapDefinition` asset `Assets/ScriptableObjects/Maps/Chapter2Map.asset`.
- Distinct grid layout (different path shape from Chapter 1).
- Tile sprites: ice/snow buildable + frozen path. Slots left for user art (`mapName = "Chapter 2"`); if sprites unassigned, falls back to color tiles.

### New Enemies (EnemyData + prefabs)
- **IceShard** — fast, low HP (MoveSpeed high, MaxHp low). Blue-white color.
- **FrostBrute** — slow, high armor, high HP. Light-blue color.
- **GlacialShield** — shielded; shield regenerates a few seconds after breaking. Cyan color. Reuses existing shielded trait + a regen-shield flag.

`EnemyData` schema: check existing fields. If no "shield regen" field exists, add `ShieldRegenDelay` (float, 0 = no regen) to `EnemyData` and handle in `EnemyBase`.

### Waves
- Generate `Chapter2_Wave01`–`Chapter2_Wave15` in SceneBootstrapper, mixing the 3 new ice enemies with escalating counts. Harder baseline than Chapter 1.
- These are separate wave assets; the active wave set is chosen by the selected stage.

### Stage Catalog
- Extend `StageCatalog` with Chapter 2 stages (e.g. `Stage 2-1` … `Stage 2-4`), each referencing the Chapter 2 map and a difficulty multiplier.
- Wave selection: `WaveManager` uses the wave set tied to the selected stage's chapter. Add a chapter field or wave-set reference to `StageDefinition`.

**Scope note:** Chapter 2 is the largest item. If during planning it proves too big for one plan, split: (a) map + enemies + waves, (b) stage catalog wiring.

## Feature 6: Backend Gacha Pity Persistence

**Files:** `Backend/DragonTD.API/Models/PlayerProgressionState.cs`, new migration, verify `ProgressionController`.

- Add `int GachaPullsSinceLastEpic` and `int GachaTotalPulls` columns to `PlayerProgressionState`.
- New EF migration `AddGachaPityToProgression`.
- `ProgressionController` PUT stores the raw Unity JSON (per existing design) — confirm the Unity DTO already includes `gacha_pulls_since_last_epic` / `gacha_total_pulls` (added in Phase 2). If progression is stored as raw JSON blob, no column needed and this item reduces to a verification test. If stored as typed columns, add the two columns + migration.
- Add a backend test confirming pity round-trips through PUT then GET.

## Feature 7: Nakama Device Auth + Leaderboard Shell

**Files:** `Unity/Packages/manifest.json`, new `Unity/Assets/Scripts/Core/NakamaAuthService.cs`, `NakamaLeaderboardService.cs`, `PlayerInventory.cs`.

### Auth
- Add Nakama Unity SDK to `manifest.json` (`com.heroiclabs.nakama-unity` via git URL or scoped registry).
- `NakamaAuthService` — connects to a Nakama server (host/port/key from config), `AuthenticateDeviceAsync(deviceId)` returns an `ISession`.
- `PlayerInventory.LoginWithNakamaAsync(deviceId)` — parallels `LoginWithDeviceAndUseApiAsync`; stores the session; sets sync status.
- Nakama server URL/key come from a config object (serialized fields or a ScriptableObject), default to localhost dev values. **Do not commit real keys.**

### Leaderboard (score submission)
- `NakamaLeaderboardService` — `SubmitScoreAsync(string leaderboardId, long score)` and `ListTopAsync(string leaderboardId, int limit)`.
- Wired to Nakama's `WriteLeaderboardRecordAsync` / `ListLeaderboardRecordsAsync`.
- Battle victory submits the score (e.g. waves cleared × stars) when a Nakama session exists.
- No in-game leaderboard UI screen this pass — service + submission only. A debug log or existing panel can show top scores; full UI deferred.

**Compile safety:** If the Nakama SDK isn't installed when scripts compile, wrap Nakama-dependent code in `#if NAKAMA_AVAILABLE` or keep it isolated so the project still compiles. Prefer: add the package first, verify it resolves, then write the services.

## Files Summary

| Action | Path |
|--------|------|
| Modify | `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs` (range ring + level badge + portrait verify) |
| Modify | `Unity/Assets/Scripts/TowerDefense/Combat/TowerSelectionManager.cs` (toggle ring on select) |
| Modify | `Unity/Assets/Editor/SceneBootstrapper.cs` (ring + badge prefab children, Chapter 2 gen) |
| Create | `Unity/Assets/Scripts/UI/SummonResultPanel.cs` |
| Modify | `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` (10-pull result screen) |
| Modify | `Unity/Assets/Scripts/Core/PlayerInventory.cs` (LastTenPullResults, Nakama login) |
| Create | `Unity/Assets/ScriptableObjects/Maps/Chapter2Map.asset` (via bootstrapper) |
| Modify | `Unity/Assets/Scripts/Core/StageCatalog.cs` (Chapter 2 stages) |
| Modify | `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyData.cs` (shield regen if needed) |
| Modify | `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs` (shield regen behavior) |
| Modify | `Backend/DragonTD.API/Models/PlayerProgressionState.cs` (pity columns if typed) |
| Migration | `AddGachaPityToProgression` (if columns added) |
| Modify | `Unity/Packages/manifest.json` (Nakama SDK) |
| Create | `Unity/Assets/Scripts/Core/NakamaAuthService.cs`, `NakamaLeaderboardService.cs` |

## Out of Scope

- In-game leaderboard UI screen (service only)
- Real Nakama production server credentials
- Chapter 2 final art (placeholder colors until user supplies tiles/portraits)
- 3 new dragon portraits (user-supplied)

## Build/Test

- Unity: `Dragon Dominion > Build Battle Scene` regenerates scenes; manual play-test for rings/badges/10-pull.
- Backend: `dotnet test Backend/DragonTD.sln` must stay green; add pity round-trip test.

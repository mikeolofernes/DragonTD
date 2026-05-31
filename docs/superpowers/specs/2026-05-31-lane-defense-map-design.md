# LaneDefense Map Mode Design

Date: 2026-05-31

## Overview

Additive map mode alongside existing PathFollowing. Enemies spawn from the left edge, walk straight right, and damage a Wall structure. Wall HP = 0 → defeat. Dragons deploy on designated tiles inside and outside the wall.

## MapDefinition Extensions

Add to `MapDefinition.cs`:

```csharp
[Header("Lane Defense Mode")]
public MapType mapType = MapType.PathFollowing;
public int wallColumn = 8;      // column where the wall sits (0-indexed)
public float wallHp = 1000f;
```

New enum `MapType.cs`:
```csharp
public enum MapType { PathFollowing, LaneDefense }
```

## Map Layout

```
cols 0-7: combat zone (enemies walk through, dragons outside wall can attack)
col 8:    WALL (HP bar, blocks/takes damage from enemies)
cols 9-11: base zone (dragons deployed here are safe, still shoot)
```

Grid 'B' tiles mark deployment zones across both sides. 'P' tiles mark columns enemies walk through (all non-wall columns effectively).

## New Components

### WallBase (MonoBehaviour)

- Placed at `wallColumn` x position, centered vertically spanning all grid rows
- Has `float CurrentHp`, `float MaxHp`
- `TakeDamage(float dmg)` — reduces HP, fires `OnWallDestroyed` event at 0
- Visual: column of colored tiles (orange/brown wall sprites) + HP bar overlay
- `GameManager` subscribes to `OnWallDestroyed` → `SetState(Defeat)`

### LaneEnemyMovement

Not a new class — modify `EnemyBase.MoveTowardsWaypoint` to detect `LaneDefense` mode:
- In LaneDefense: `transform.position += Vector3.right * CurrentMoveSpeed * Time.deltaTime;`
- When `transform.position.x >= wallWorldX`: call `WallBase.Instance.TakeDamage(DamageToBase * baseDamageScale)`, then die (don't trigger GameManager.LoseLife)

### LaneWaveSpawner

Extend `WaveManager.SpawnEnemy` — in LaneDefense mode:
- Ignore the serialized `_spawnPoints` (left-edge single point)
- Instead, pick a random row `y ∈ [0, gridHeight-1]`, spawn at world position `(leftEdgeX, rowWorldY)`
- Enemies from the same `EnemySpawnEntry` spread across different random rows

## Defeat Condition Change

In LaneDefense mode: `GameManager.LoseLife` is NOT called when enemy reaches end. Instead enemy reaches wall and calls `WallBase.TakeDamage`. Lives counter hidden. Wall HP bar shown in HUD.

## GridManager Tile Assignment (LaneDefense)

`SetupGridManager` for LaneDefense maps:
- Wall column tiles = new TileType `Wall` (not buildable, not path)
- Left of wall: some tiles randomly assigned as 'B' (buildable), rest blocked
- Right of wall: some tiles randomly assigned as 'B', rest blocked

On first build, random assignment uses `System.Random(mapName.GetHashCode())` for determinism per map.

## HUD Changes

When `mapType == LaneDefense`:
- Hide "Lives" text
- Show "Wall HP: X/Y" text
- Show wall HP bar (red fill, decreases as damaged)

## Files

| Action | Path |
|--------|------|
| Create | `Unity/Assets/Scripts/Core/MapType.cs` |
| Modify | `Unity/Assets/Scripts/TowerDefense/MapDefinition.cs` |
| Create | `Unity/Assets/Scripts/TowerDefense/WallBase.cs` |
| Modify | `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs` |
| Modify | `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs` |
| Modify | `Unity/Assets/Scripts/Core/GameManager.cs` |
| Modify | `Unity/Assets/Scripts/UI/BattleHUD.cs` |
| Modify | `Unity/Assets/Editor/SceneBootstrapper.cs` |

## Out of Scope

- Wall repair mechanic
- Multiple walls
- Animated wall destruction
- Enemies targeting specific towers (they just walk right)

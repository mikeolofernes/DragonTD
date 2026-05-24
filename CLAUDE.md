# DragonTD — Project Reference

## Project Overview

DragonTD is a cross-platform mobile tower defense RPG built on Unity (game client) and ASP.NET Core (backend API), with a PostgreSQL database. Players explore the world of Aetheria, raise dragons with unique elements and roles, place them as towers to defend against waves of enemies, and collect new dragons through a gacha summon system. A bond system deepens the relationship between player and dragon over time, unlocking stat boosts and story content.

Target platforms: iOS and Android. Unity handles all gameplay and UI; the backend handles accounts, player data persistence, gacha pulls, and leaderboards.

---

## Repo Structure

```
DragonTD/
  Unity/                        # Unity game client (Unity 2022 LTS+)
    Assets/
      Scripts/
        Core/                   # GameManager, ResourceManager, GameState, etc.
        Dragons/
          Data/                 # ScriptableObjects and enums for dragon definitions
        Enemies/                # EnemyBase, enemy ScriptableObjects, waypoints
        Towers/                 # DragonTower placement, targeting, projectiles
        Gacha/                  # SummonPool ScriptableObjects, GachaSystem
        UI/                     # HUD, menus, gacha screen, dragon collection
        Waves/                  # WaveManager, WaveData ScriptableObjects
      Art/
      Audio/
      Prefabs/
      ScriptableObjects/        # Runtime asset instances (dragons, waves, pools)
    ProjectSettings/
  Backend/
    DragonTD.API/               # ASP.NET Core Web API project
    DragonTD.Domain/            # Domain models and interfaces
    DragonTD.Infrastructure/    # EF Core, PostgreSQL, repository implementations
    DragonTD.Tests/             # xUnit test project
```

---

## Coding Conventions

- **Classes, methods, properties**: PascalCase (`GameManager`, `StartBattle`, `CurrentWave`)
- **Private fields**: `_camelCase` (`_startingLives`, `_currentState`)
- **Inspector exposure**: use `[SerializeField]` on private fields; avoid making fields public solely for inspector access
- **Static data**: define in ScriptableObjects (e.g., `DragonData`, `WaveData`, `AbilityData`); never hard-code stat tables in code
- **MonoBehaviours**: keep thin — logic lives in plain C# classes or managers; MonoBehaviours handle Unity lifecycle and component wiring only
- **Singletons**: use the `Instance` pattern with an Awake guard:

  ```csharp
  public static MyManager Instance { get; private set; }
  private void Awake()
  {
      if (Instance != null) { Destroy(gameObject); return; }
      Instance = this;
      DontDestroyOnLoad(gameObject);
  }
  ```

- **Namespaces**: `DragonTD.Core`, `DragonTD.Dragons`, `DragonTD.Enemies`, `DragonTD.Towers`, `DragonTD.Gacha`, `DragonTD.UI`, `DragonTD.Waves`
- **Events**: expose as `public event System.Action<T> OnXxx;` and fire through a protected/private method
- **Backend**: follow standard ASP.NET Core conventions — controllers thin, business logic in services, data access behind repository interfaces

---

## Key Data Flow

```
DragonData (ScriptableObject — static template)
    └─ DragonInstance (plain C# — player-owned copy, has Level / BondLevel / equipment)
            └─ Dragon MonoBehaviour (placed in battle — reads from DragonInstance for stats)
```

- `DragonData` is never mutated at runtime; it is the source of truth for base stats, abilities, rarity, element, and role.
- `DragonInstance` wraps `DragonData` and layers runtime state: level scaling, bond multipliers, total battles.
- The `Dragon` MonoBehaviour (attached to a placed tower prefab) references a `DragonInstance` to determine effective stats during a wave.

---

## Tower Defense Loop

1. **WaveManager** reads `WaveData` ScriptableObjects and spawns enemies on a timer.
2. Enemies are instantiated on the enemy path and advance along waypoints each frame.
3. **DragonTower** (MonoBehaviour on placed dragon prefab) runs a targeting loop, detecting enemies within `Range`.
4. On attack cooldown expiry, `DragonTower` instantiates a projectile prefab aimed at the selected target.
5. Projectiles travel to the target and call `EnemyBase.TakeDamage(float amount)` on impact.
6. When an enemy reaches the end of the path, `GameManager.LoseLife()` is called.
7. When all enemies in a wave are defeated, `WaveManager` notifies `GameManager.OnWaveCleared()`.
8. `GameManager` checks whether more waves remain; if not, triggers Victory.

---

## Gacha System

- **SummonPool** ScriptableObject defines the pool name, list of `DragonData` entries, and per-rarity drop rates.
- Players spend premium currency (Gems) to pull from a pool.
- **Soft pity** begins at pull **50**: each pull past 50 increases the SSS-rate incrementally.
- **Hard pity** guarantees an SSS-rarity dragon at pull **100** and resets the counter.
- Pity counters are stored server-side (backend) and also cached locally for display.
- `GachaSystem` (client) sends pull requests to the backend; the backend resolves results authoritatively.

---

## Bond System

- `DragonInstance.BondLevel` ranges from **1 to 20**.
- Bond XP is earned by:
  - Completing battles with the dragon deployed (+10 XP per battle)
  - Feeding Bond Items (various XP values)
  - Exploration assignments (passive XP over time)
- Each bond level requires `100 * BondLevel` XP to advance (level 1 → 2 costs 100, level 2 → 3 costs 200, etc.).
- **Bond bonuses**: each bond level grants +2% to all stats via `BondStatMultiplier = 1 + (BondLevel - 1) * 0.02`.
- Bond level milestones (5, 10, 15, 20) unlock story vignettes, alternate skins, or ultimate skill upgrades.

---

## Development Phases

| Phase | Name | Status | Goal |
|-------|------|--------|------|
| 1 | Prototype | **Current** | Core TD loop playable, 3 dragon archetypes, 5 waves |
| 2 | Vertical Slice | Planned | 1 full chapter (15 waves), 10 dragons, gacha, basic UI |
| 3 | Alpha | Planned | All systems integrated, backend live, 3 chapters |
| 4 | Beta | Planned | Soft launch, balance pass, leaderboards, push notifications |
| 5 | Launch | Planned | Global release, live-ops pipeline, first event |

---

## How to Run

### Unity Client

1. Install **Unity 2022 LTS** or later via Unity Hub.
2. Open the `Unity/` folder as a project in Unity Hub.
3. Open the `MainMenu` scene and press Play, or build to device via File > Build Settings.

### Backend API

```bash
cd Backend/DragonTD.API
dotnet run
```

The API listens on `https://localhost:5001` by default.

### Database Migrations

```bash
cd Backend/DragonTD.Infrastructure
dotnet ef database update --startup-project ../DragonTD.API
```

---

## Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Secret` | Secret key used to sign JWT tokens (min 32 chars) |

Set these in `Backend/DragonTD.API/appsettings.Development.json` (never commit secrets) or via OS environment variables / a secrets manager in production.

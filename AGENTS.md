# AGENTS.md — Dragon Dominion
> Machine-readable project context for Codex.
> Do not add lore, narrative, or design rationale here. Keep it technical and actionable.

---

## Project Overview

**Game:** Dragon Dominion — Tower Defense RPG
**Engine:** Unity (URP) + C#
**Backend:** .NET 9 / ASP.NET Core
**Platforms:** iOS + Android
**Realtime/Social:** Nakama (open source)
**Database:** PostgreSQL + Redis
**Asset Delivery:** Unity Addressables + Cloudflare CDN

Full design context: `/game-design-docs/GDD.md`

---

## Repo Structure

```
DragonTD/
  Unity/                        ← Unity project root
    Assets/
      Scripts/
        Core/                   ← GameManager, ResourceManager, GameState, GameDirector
        Dragons/                ← Dragon runtime C# scripts + ScriptableObjects
          Data/                 ← DragonRarity, DragonElement, DragonRole enums (legacy)
        TowerDefense/
          Combat/               ← DragonTower, ProjectileBase, AbilityExecutor
          Enemies/              ← EnemyBase, TrollEnemy, UndeadEnemy, WaypointPath
          Waves/                ← WaveManager, WaveData
        Summoning/              ← SummonPool, GachaSystem
        UI/                     ← BattleHUD, DragonPlacementCard, VictoryDefeatPanel
      ScriptableObjects/        ← Runtime asset instances
    ProjectSettings/
  Backend/
    DragonTD.API/               ← ASP.NET Core Web API (.NET 9)
    DragonTD.Domain/            ← Domain models and interfaces
    DragonTD.Infrastructure/    ← EF Core, PostgreSQL, repositories
    DragonTD.Tests/             ← xUnit tests
```

---

## Naming Conventions

### Dragon IDs
Format: `{name_lowercase}_{3digit_number}`
```
voltaris_001
frostfang_002
magmaclaw_003
tempest_glacion_004     ← fusion results get their own ID
```

### Addressable Keys
```
dragons/{dragonId}/hatchling_sprite
dragons/{dragonId}/young_sprite
dragons/{dragonId}/mature_sprite
dragons/{dragonId}/elder_sprite
dragons/{dragonId}/apex_sprite
dragons/{dragonId}/titan_sprite
dragons/{dragonId}/idle_anim
dragons/{dragonId}/attack_anim
dragons/{dragonId}/evolve_anim
dragons/{dragonId}/fusion_vfx
dragons/{dragonId}/skill_active_vfx
dragons/{dragonId}/skill_ultimate_vfx
audio/dragons/{dragonId}/roar
audio/dragons/{dragonId}/bond3_voice
audio/dragons/{dragonId}/bond6_voice
audio/dragons/{dragonId}/bond7_transform
audio/fusion/{fusionResultId}/theme
```

### Skill IDs
Format: `{effect_name}_{3digit_number}`
```
lightning_bolt_001
chain_strike_002
static_field_001
tempest_call_001
```

### Script Naming
- ScriptableObjects: `Dragon{System}` — e.g. `DragonDefinition`, `DragonBondData`
- Runtime scripts: `Dragon{Behavior}` — e.g. `DragonTower`, `DragonAssetLoader`
- Services: `I{Name}Service` interface + `{Name}Service` implementation
- UI: `{Screen}Panel`, `{Component}Card` — e.g. `DragonCollectionPanel`, `DragonPlacementCard`

---

## Coding Conventions

- **Classes, methods, properties**: PascalCase (`GameManager`, `StartBattle`, `CurrentWave`)
- **Private fields**: `_camelCase` (`_startingLives`, `_currentState`)
- **ScriptableObject public fields**: camelCase (`dragonId`, `displayName`, `baseStats`)
- **Inspector exposure**: `[SerializeField]` on private fields; never public fields just for Inspector
- **Static data**: define in ScriptableObjects; never hardcode stat tables in code
- **MonoBehaviours**: keep thin — logic in plain C# classes or managers
- **Singletons**:
  ```csharp
  public static MyManager Instance { get; private set; }
  private void Awake()
  {
      if (Instance != null) { Destroy(gameObject); return; }
      Instance = this;
      DontDestroyOnLoad(gameObject);
  }
  ```
- **Namespaces**: `DragonTD.Core`, `DragonTD.Dragons`, `DragonTD.TowerDefense`, `DragonTD.Summoning`, `DragonTD.UI`
- **Events**: `public event System.Action<T> OnXxx;` fired through a private method

---

## Core Enums

```csharp
public enum DragonClass
{
    Flame, Frost, Storm, Earth, Venom, Celestial, Abyssal, Ancient
}

public enum DragonElement
{
    Fire, Water, Wind, Earth, Lightning, Ice, Shadow, Light
}

public enum DragonRarity
{
    Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythic = 5, Ancient = 6
}

public enum DragonEvolutionStage
{
    Hatchling, Young, Mature, Elder, Apex, Titan
}

public enum FusionType
{
    Stable, Hybrid, Experimental
}

public enum SkillType
{
    Damage, Heal, Buff, Debuff, Summon, Terrain
}

public enum TargetType
{
    Single, AoE, Chain, Self, Allies, Global
}
```

---

## ScriptableObject Schemas

### DragonDefinition
```csharp
[CreateAssetMenu(fileName = "New Dragon", menuName = "Dragon Dominion/Dragon Definition")]
public class DragonDefinition : ScriptableObject
{
    public string dragonId;        // "voltaris_001"
    public string displayName;
    public DragonClass dragonClass;
    public DragonElement element;
    public DragonRarity rarity;
    public DragonBaseStats baseStats;
    public DragonStatGrowth growth;
    public DragonSkillSet skillSet;
    public DragonEvolutionPath evolutionPath;
    public DragonFusionTable fusionTable;
    public DragonBondData bondData;
    public DragonPersonality personality;
    public DragonVisualData visualData;
    public int manaCost = 80;

    // Convenience accessors for TD combat
    public SkillDefinition NormalAttack => skillSet.normalAttack;
    public SkillDefinition ActiveSkill  => skillSet.activeSkill;
}
```

### DragonBaseStats
```csharp
[System.Serializable]
public class DragonBaseStats
{
    public float hp;
    public float attack;
    public float attackSpeed;
    public float armor;
    public float magicResist;
    public float flightSpeed;
    public float range;
    public float mana;
    public float critChance;        // 0.0–1.0
    public float elementalAffinity; // 0.0–1.0
}
```

### DragonSkillSet
```csharp
[System.Serializable]
public class DragonSkillSet
{
    public SkillDefinition normalAttack;
    public SkillDefinition activeSkill;
    public SkillDefinition passiveSkill;
    public SkillDefinition ultimateSkill; // null until Bond 6
}
```

### SkillDefinition
```csharp
[CreateAssetMenu(fileName = "NewSkill", menuName = "Dragon Dominion/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    public string skillId;
    public string displayName;
    [TextArea] public string description;
    public SkillType skillType;
    public TargetType targetType;
    public float baseDamage;
    public float manaCost;
    public float cooldown;
    public float[] levelMultipliers; // length 10, index = skillLevel-1
    public StatusEffect[] statusEffects;
    public string vfxKey;
    public string sfxKey;
    // Tower Defense
    public float range = 4f;
    public bool isAoe;
    public float aoeRadius;
    public float GetDamageMultiplier(int skillLevel) { ... }
}
```

### DragonBondData (7 levels)
```csharp
[System.Serializable]
public class DragonBondData
{
    public BondLevel[] bondLevels;  // length 7
    public float battleBondXP;
    public float feedBondXP;
    public float trainBondXP;
    public float exploreBondXP;
    public string[] voiceLineKeys;  // length 7
}

[System.Serializable]
public class BondLevel
{
    public int level;               // 1-7
    public float xpRequired;
    public float statBoostPercent;  // additive, e.g. 0.05 = +5%
    public string unlockDescription;
    public string skillUnlockId;
    public bool triggersEvolutionAccess;
    public string transformationVfxKey; // non-null at level 7
}
```

Bond stat table reference:
| Bond | XP Req | Stat Boost |
|------|--------|------------|
| 1 | 0 | +0% |
| 2 | 100 | +5% |
| 3 | 300 | +8% |
| 4 | 700 | +12% |
| 5 | 1500 | +18% |
| 6 | 3000 | +25% |
| 7 | 6000 | +35% |

---

## Key Data Flow

```
DragonDefinition (ScriptableObject — static template)
    └─ DragonInstance (plain C# — player-owned copy: Level, BondLevel, EvolutionStage)
            └─ DragonTower MonoBehaviour (placed in battle — reads DragonInstance for stats)
```

- `DragonDefinition` is never mutated at runtime.
- `DragonInstance` wraps `DragonDefinition` with runtime state.
- `DragonTower` references a `DragonInstance` during waves.

---

## DragonRegistry

```csharp
[CreateAssetMenu(fileName = "DragonRegistry", menuName = "Dragon Dominion/Dragon Registry")]
public class DragonRegistry : ScriptableObject
{
    public DragonDefinition[] allDragons;
    // Methods: Initialize(), Get(dragonId), GetByElement, GetByRarity, GetByClass, CanFuse
}
```

- Single global instance loaded at game start.
- **Never reorder existing entries** — breaks save data.
- **Only add entries** to the end.

---

## Tower Defense Loop

1. **WaveManager** reads `WaveData` ScriptableObjects and spawns enemies on a timer.
2. Enemies follow waypoints each frame (`EnemyBase`).
3. **DragonTower** runs a targeting loop within `range`.
4. On attack cooldown, instantiates a projectile aimed at target.
5. `ProjectileBase` calls `EnemyBase.TakeDamage(float)` on impact.
6. Element multiplier applied via `ElementInteraction.GetMultiplier(attacker, defender)`.
7. `GameDirector` monitors intensity, scales difficulty, triggers elite spawns and relief events.
8. Wave clear → `WaveManager` notifies `GameManager.OnWaveCleared()`.

---

## Gacha System

- **SummonPool** ScriptableObject defines banner name, `DragonDefinition[]` entries with weights, and per-rarity rates.
- Players spend Gems (premium currency).
- Rarity rates: Common=40%, Uncommon=30%, Rare=20%, Epic=7%, Legendary=2.5%, Mythic=0.5%
- **Soft pity** starts at pull 50: Epic+ rates tripled.
- **Hard pity** at pull 100: force Mythic, reset counter.
- 10-pull guarantee: at least one Rare or above on the 10th pull.
- Pity stored server-side; cached locally for display.

---

## Common Tasks

### Add a New Dragon
1. Create `Assets/ScriptableObjects/Dragons/{DragonName}.asset` (type: `DragonDefinition`)
2. Set `dragonId` using format `{name_lowercase}_00{n}`
3. Create skill assets in `Assets/ScriptableObjects/Dragons/`
4. Fill `evolutionPath` — all 6 stages required
5. Fill `bondData` — all 7 BondLevel entries required
6. Add to `DragonRegistry.asset > allDragons[]`
7. Run `DragonTD/Export Dragons to JSON` from Editor menu

### Add a New Skill
1. Create `SkillDefinition` asset
2. Set `skillId` as `{effect_name}_00{n}`
3. Fill `levelMultipliers[]` — must be length 10
4. Assign to dragon's `skillSet` in DragonDefinition

### Stat Balancing Reference

| Rarity | HP (Lv1) | ATK (Lv1) | rarityMultiplier |
|--------|----------|-----------|-----------------|
| Common | 400–600 | 80–120 | 1.0 |
| Uncommon | 600–900 | 120–180 | 1.2 |
| Rare | 900–1200 | 180–260 | 1.5 |
| Epic | 1100–1500 | 280–380 | 1.8 |
| Legendary | 1500–2000 | 380–520 | 2.2 |
| Mythic | 2000–2800 | 520–700 | 2.5 |
| Ancient | 2800–4000 | 700–1000 | 3.0 |

---

## Architecture Rules

- **Never hardcode a dragonId string** outside ScriptableObject assets and editor tools
- **Never load dragon assets synchronously** — always use `DragonAssetLoader` async methods
- **Never add dragon stats to code** — all stats live in ScriptableObjects
- **DragonRegistry is the only dragon lookup** — no other class maintains a dragon list
- **New dragon = new asset file** — never add dragons to existing files or code arrays
- **AnimationCurves are designer-owned** — leave as Unity defaults if unsure
- **Addressable keys are immutable once shipped** — changing breaks asset loading

---

## Backend API Conventions (.NET 9)

- Route prefix: `/api/v1/`
- Auth: JWT Bearer token on all routes except `/api/v1/auth/`
- Response envelope:
  ```json
  { "success": true, "data": {}, "error": null }
  ```
- JSON field naming: `snake_case`
- PostgreSQL table prefix: `dd_` (e.g. `dd_player_dragons`, `dd_bond_progress`)
- Dragon validation: validate `dragonId` against exported registry before processing

---

## Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (min 32 chars) |

Set via OS environment variables or secrets manager. **Never commit to source control.**

---

## How to Run

### Unity Client
1. Install Unity 2022 LTS+ via Unity Hub.
2. Open `Unity/` folder as a project.
3. Open `MainMenu` scene and press Play.

### Backend API
```bash
cd Backend/DragonTD.API
dotnet run
```
API listens on `https://localhost:5001`.

### Database Migrations
```bash
cd Backend/DragonTD.Infrastructure
dotnet ef database update --startup-project ../DragonTD.API
```

---

## Development Phases

| Phase | Name | Status | Goal |
|-------|------|--------|------|
| 1 | Prototype | **Current** | Core TD loop playable, 3 dragon archetypes, 5 waves |
| 2 | Vertical Slice | Planned | 1 full chapter (15 waves), 10 dragons, gacha, basic UI |
| 3 | Alpha | Planned | All systems integrated, backend live, 3 chapters |
| 4 | Beta | Planned | Soft launch, balance pass, leaderboards |
| 5 | Launch | Planned | Global release, live-ops pipeline, first event |

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, invoke the `skill` tool with `skill: "graphify"` before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

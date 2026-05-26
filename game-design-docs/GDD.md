# Dragon Dominion — Game Design Document

> **Version:** Phase 1 (Prototype)
> **Engine:** Unity (URP) · **Backend:** .NET 9 / ASP.NET Core · **Platforms:** iOS + Android

---

## Table of Contents

1. [Game Overview](#1-game-overview)
2. [Core Gameplay Loop](#2-core-gameplay-loop)
3. [Tower Defense System](#3-tower-defense-system)
4. [Dragon System](#4-dragon-system)
5. [Element System](#5-element-system)
6. [Enemy Roster](#6-enemy-roster)
7. [Wave System](#7-wave-system)
8. [Dynamic Difficulty Director](#8-dynamic-difficulty-director)
9. [Dragon Fusion](#9-dragon-fusion)
10. [Summoning & Gacha](#10-summoning--gacha)
11. [Economy & Currencies](#11-economy--currencies)
12. [Bond System](#12-bond-system)
13. [Evolution System](#13-evolution-system)
14. [Phase 1 Dragon Roster](#14-phase-1-dragon-roster)
15. [Development Phases](#15-development-phases)

---

## 1. Game Overview

**Dragon Dominion** is a tower defense RPG for mobile (iOS + Android). Players collect, bond with, and evolve dragons, then deploy them as towers to defend against waves of enemies. The game blends strategic tower placement with RPG depth — each dragon has a unique element, skill set, bond progression, and evolution path.

The meta-game revolves around collecting a roster of dragons through a gacha summoning system, deepening bonds with them over time, and fusing pairs to create powerful hybrid dragons.

### Design Pillars

- **Collect** — Build a roster of dragons across 8 elements and 7 rarity tiers.
- **Bond** — Form relationships with individual dragons that grow through battle and care, unlocking stats and skills.
- **Command** — Place dragons strategically on the battlefield, using their elemental strengths and active skills to defeat waves.
- **Evolve** — Advance dragons through 6 evolution stages, dramatically increasing their power and visuals.
- **Fuse** — Combine compatible dragons to produce unique Legendary or Mythic hybrids.

---

## 2. Core Gameplay Loop

```
Pre-battle (Setup Phase)
    │  Select dragons from inventory
    │  Place dragons on grid tiles
    │  Spend Mana to summon each dragon
    ▼
Wave Phase
    │  Enemies spawn and march toward base
    │  Dragons attack automatically; player triggers Active Skills manually
    │  Gold and Mana drop from enemy kills and wave rewards
    ▼
Between Waves (BetweenWaves Phase)
    │  Reposition or add dragons
    │  Spend Gold to upgrade or place new dragons
    ▼
Wave Clear / Defeat
    │  Victory: earn rewards, gain Bond XP for every deployed dragon
    │  Defeat: partial rewards, enemy feedback for rethinking placement
    ▼
Meta (Collection & Progression)
    │  Spend Gems to summon new dragons
    │  Evolve or fuse dragons
    │  Upgrade bond levels, unlock ultimate skills
    └─ Return to Pre-battle
```

### Game States

| State | Description |
|-------|-------------|
| `Setup` | Player places dragons before the first wave |
| `Wave` | Active wave in progress |
| `BetweenWaves` | Breathing room between waves |
| `Paused` | Game frozen; time scale = 0 |
| `Victory` | All waves cleared |
| `Defeat` | Lives reached 0 |

---

## 3. Tower Defense System

### Map & Grid

- The battlefield is a tile grid managed by `GridManager`.
- Tiles are either **path** (enemies walk on them) or **placement** (dragons sit on them).
- The player drags a `DragonPlacementCard` onto a valid tile to deploy a dragon.

### Dragon Towers

Each placed dragon becomes a `DragonTower` MonoBehaviour that:

1. Runs a targeting loop every frame, scanning within `range` for the closest living enemy.
2. When the attack cooldown expires, spawns a `ProjectileBase` aimed at the target.
3. The projectile calls `EnemyBase.TakeDamage(damage)` on impact, applying the element multiplier from `ElementInteraction.GetMultiplier(attacker, defender)`.
4. Mana accumulates per hit; when full, the Active Skill becomes castable.

### Mana Economy (Battle)

- Battle mana is a shared pool starting at **100** each battle.
- Spending mana to **place** a dragon costs the dragon's `manaCost` value (80–150 depending on rarity).
- Mana is **earned** by clearing waves and via Director relief events.
- This creates a placement-pacing tension: can't flood the board on wave 1.

### Lives

- The player starts with **20 lives**.
- Each enemy that reaches the base deals `DamageToBase` lives (typically 1).
- Reaching 0 lives triggers `GameState.Defeat`.

---

## 4. Dragon System

### Data Model

```
DragonDefinition  (ScriptableObject — read-only template)
    └─ DragonInstance  (plain C# — player's copy: Level, BondLevel, EvolutionStage)
            └─ DragonTower  (MonoBehaviour — battle runtime, reads DragonInstance stats)
```

`DragonDefinition` is never mutated at runtime. All player-specific state lives in `DragonInstance`.

### Stat Computation

```
effectiveStat = baseStat × levelMultiplier × bondStatMultiplier
levelMultiplier   = 1 + (level - 1) × 0.08        // +8% per level
bondStatMultiplier = 1 + sum(statBoostPercent for all reached bond levels)
```

Range and attack speed are **not** multiplied by level or bond; they scale only via evolution.

### Skill Levels

A single `SkillLevel` (1–10) applies to all of the dragon's active skills. The `SkillDefinition.levelMultipliers[]` array (length 10) stores the damage multiplier at each level.

---

## 5. Element System

Eight elements interact via a strength/weakness table. A 1.5× multiplier is applied when an attacker hits a vulnerable defender; a 0.75× penalty applies when attacking a resistant defender.

### Element Interaction Matrix

| Attacker ↓ / Defender → | Fire | Water | Wind | Earth | Lightning | Ice | Shadow | Light |
|--------------------------|------|-------|------|-------|-----------|-----|--------|-------|
| **Fire** | 1× | 0.75× | 0.75× | 1× | 1× | **1.5×** | 1× | 1× |
| **Water** | **1.5×** | 1× | 1× | **1.5×** | 0.75× | 1× | 1× | 1× |
| **Wind** | 1× | 1× | 1× | **1.5×** | **1.5×** | 1× | 1× | 1× |
| **Earth** | 0.75× | 1× | 1× | 1× | **1.5×** | 1× | 1× | 1× |
| **Lightning** | 1× | **1.5×** | 0.75× | 1× | 1× | 1× | 1× | 1× |
| **Ice** | 0.75× | 1× | **1.5×** | 1× | 1× | 1× | 1× | 1× |
| **Shadow** | 1× | 1× | 1× | 1× | 1× | 1× | 1× | **1.5×** |
| **Light** | 1× | 1× | 1× | 1× | 1× | 1× | **1.5×** | 1× |

> Not all enemies have an element. `EnemyData.HasElement` gates whether the multiplier is applied.

---

## 6. Enemy Roster

### Factions

| Faction | Description |
|---------|-------------|
| `Orc` | Balanced melee foot soldiers; medium HP, medium speed |
| `Troll` | Tanky slow bruisers; high HP, high armor, low speed |
| `Undead` | Varied; can include fast skeletons or slow zombies |
| `CorruptedDragon` | Late-game elite units; dragon elements, high stats |

### Enemy Stats (ScriptableObject: `EnemyData`)

| Field | Description |
|-------|-------------|
| `MaxHp` | Base hit points |
| `MoveSpeed` | Units per second along the waypoint path |
| `Armor` | Flat damage reduction; `effectiveDamage = max(1, damage − armor)` |
| `GoldValue` | Gold added to player pool on death |
| `DamageToBase` | Lives lost when enemy reaches the end |
| `Element` / `HasElement` | Elemental type for interaction multiplier |

### Enemy Spawning Difficulty Scaling

The `GameDirector` applies a `difficultyMultiplier` to every enemy's HP and speed on spawn:

```
scaledHp    = baseHp × difficultyMultiplier
scaledSpeed = baseSpeed × lerp(1, difficultyMultiplier, 0.5)
```

Multiplier range: **0.75× (relief) → 2.0× (max pressure)**.

---

## 7. Wave System

### WaveData ScriptableObject

Each wave is a `WaveData` asset containing:

- `EnemyGroups[]` — list of `EnemySpawnEntry` (prefab + count + spawn interval)
- `TimeBetweenGroups` — pause between groups within the wave
- `GoldReward` — gold added to player on wave clear
- `ManaReward` — mana added to player on wave clear

### Wave Lifecycle

1. `GameManager.StartNextWave()` increments `CurrentWave` and sets state to `Wave`.
2. `WaveManager` receives the state change and begins the spawn coroutine for `_waves[waveIndex]`.
3. Enemies spawn at `_spawnPoints[0]` and are initialized with the waypoint path.
4. When `ActiveEnemyCount` reaches 0, `WaveManager.OnEnemyDied()` fires `OnWaveComplete` and grants rewards.
5. `GameManager.OnWaveCleared()` transitions to `Victory` (if last wave) or `BetweenWaves`.

### Phase 1 Target: 5 Waves

The prototype delivers 5 waves with increasing enemy counts and the introduction of Troll enemies mid-chapter.

---

## 8. Dynamic Difficulty Director

The `GameDirector` runs an **intensity meter** (0–1 float) and reacts to player performance in real time to keep the session feeling tense but fair.

### Intensity Meter

| Event | Effect |
|-------|--------|
| Enemy is alive | +0.08 per second |
| No enemies alive | −0.12 per second |
| Player loses a life | +0.15 spike |
| Wave cleared | −0.25 drop |

### Difficulty Scaling

| Trigger | Effect |
|---------|--------|
| 2 consecutive perfect waves (no lives lost) | `difficultyMultiplier` +0.1 (cap 2.0) |
| 3+ lives lost in a single wave | `difficultyMultiplier` −0.15 (floor 0.75) |

### Director Events

| Event | Trigger Condition |
|-------|-------------------|
| **Elite Enemy Spawn** | Intensity ≥ 0.7 AND random roll < 0.4 per second |
| **Bonus Resource Drop** | Sustained intensity ≥ 0.85 for 30 s, OR intensity low AND 2+ lives lost |
| **Difficulty Increased** | 2 perfect-wave streak hit |
| **Difficulty Decreased** | Relief condition met |

Elite enemies receive a **1.5× stat multiplier** on top of the current difficulty multiplier. Relief events grant **+40 mana + 60 gold**.

Minimum cooldown between any two Director events: **20 seconds**.

---

## 9. Dragon Fusion

Two compatible parent dragons are consumed to produce a single, more powerful result dragon. Fusion results have their own `dragonId` (e.g., `tempest_glacion_004`) and unique visuals.

### Fusion Rules

- Compatibility is defined in each dragon's `DragonFusionTable.fusionEntries[]`.
- Both parents are removed from the player's inventory.
- The result dragon starts at **Hatchling** stage, level 1, bond 1.
- Fusion type (Stable / Hybrid / Experimental) affects the outcome's element and base stats.

### Phase 1 Fusion

| Parent A | Parent B | Result | Fusion Type |
|----------|----------|--------|-------------|
| Voltaris (Lightning/Storm) | Frostfang (Ice/Frost) | Tempest Glacion (Lightning/Storm, Legendary) | Hybrid |

---

## 10. Summoning & Gacha

### Banner (SummonPool)

Each banner is a `SummonPool` ScriptableObject containing:

- Banner name and featured dragons
- `RarityRates[]` — per-rarity pull probabilities
- `HasRateUp` + `RateUpDragon` — optional 50% rate-up for a featured dragon

### Rarity Rates (Standard Banner)

| Rarity | Base Rate | Soft-Pity Rate (pull 50+) |
|--------|-----------|---------------------------|
| Common | 40% | 40% |
| Uncommon | 30% | 30% |
| Rare | 20% | 20% |
| Epic | 7% | 21% |
| Legendary | 2.5% | 7.5% |
| Mythic | 0.5% | 1.5% |

### Pity System

| Mechanic | Trigger | Effect |
|----------|---------|--------|
| **Soft pity** | Pull 50+ without Epic or above | Epic+ rates tripled |
| **Hard pity** | Pull 100 without Epic or above | Forces Mythic; counter resets |
| **10-pull guarantee** | 10-pull with no Rare+ in first 9 | 10th pull guaranteed Rare or above |

Pity counters are stored **server-side** and cached locally for display. They are **per-banner** and reset when the relevant pity fires.

### Currency

| Action | Cost |
|--------|------|
| Single pull | 160 Gems |
| 10-pull | 1,600 Gems (no discount; pity protection is the value) |

---

## 11. Economy & Currencies

### Currency Types

| Currency | Source | Use |
|----------|--------|-----|
| **Mana** (battle) | Wave rewards, Director relief, start of battle (100) | Place dragons during battle |
| **Gold** (battle) | Enemy kills (`GoldValue`), wave rewards | In-battle upgrades (Phase 2+) |
| **Gems** (premium) | IAP, login rewards, achievements | Summon pulls, special offers |

### Battle Economy Flow

```
Battle starts → 100 Mana
Enemy dies    → +GoldValue gold
Wave cleared  → +GoldReward gold, +ManaReward mana
Director relief → +40 mana, +60 gold
Dragon placement ← costs ManaCost (75–150 mana)
```

---

## 12. Bond System

Bonds deepen through battle (`battleBondXP`), feeding, training, and exploration. Each dragon has 7 bond levels with increasing XP thresholds and stat boosts.

### Bond Level Table

| Level | XP Required | Stat Boost (additive) | Notable Unlock |
|-------|-------------|----------------------|----------------|
| 1 | 0 | +0% | — |
| 2 | 100 | +5% | — |
| 3 | 300 | +8% | Voice line |
| 4 | 700 | +12% | — |
| 5 | 1500 | +18% | — |
| 6 | 3000 | +25% | Ultimate Skill unlocked |
| 7 | 6000 | +35% | Transformation VFX |

### XP Sources

| Action | XP |
|--------|----|
| Completing a battle | `battleBondXP` (per dragon, per `DragonBondData`) |
| Feeding | `feedBondXP` |
| Training | `trainBondXP` |
| Exploration | `exploreBondXP` |

Bond XP is applied via `DragonInstance.RecordBattle()` for deployed dragons at wave end. The `AddBondXp()` method handles multi-level pop in a single call.

---

## 13. Evolution System

Each dragon has 6 evolution stages. Advancing a stage requires the dragon to meet a minimum level threshold (to be defined per-dragon in `DragonEvolutionPath`) and consumes evolution materials.

### Stages

| Stage | Visual Description |
|-------|--------------------|
| Hatchling | Small, young form |
| Young | Growing; proportions shift |
| Mature | Adult combat form |
| Elder | Battle-worn; scarred or decorated |
| Apex | Peak power; dramatic visual upgrade |
| Titan | Maximum form; enormous, elemental effects constant |

Bond level 5 gates access to the final evolution stages (`triggersEvolutionAccess = true`). Evolution does **not** increase base stats directly — it unlocks the next tier of stat growth curves.

---

## 14. Phase 1 Dragon Roster

Seven dragons ship with the Phase 1 prototype. Stats below are Level 1, Bond 1 values.

### Dragon Cards

#### Voltaris — Lightning Storm · Epic
> Dark purple and gold dragon, lightning crackling through wings.

| Stat | Value |
|------|-------|
| HP | 900 |
| Attack | 220 |
| Armor | 60 |
| Range | 5.0 |
| Attack Speed | 1.67 attacks/s |
| Mana Cost | 90 |

**Normal Attack:** Single-target lightning bolt (0.8× multiplier, 0.6 s cooldown)
**Active Skill:** Chain Lightning — 3.0× single-target, 10 s cooldown
**Fusion:** + Frostfang → Tempest Glacion

---

#### Frostfang — Ice Frost · Epic
> Blue ice-crystal dragon, exhaling frost breath.

| Stat | Value |
|------|-------|
| HP | 1100 |
| Attack | 195 |
| Armor | 90 |
| Range | 4.5 |
| Attack Speed | 0.83 attacks/s |
| Mana Cost | 85 |

**Normal Attack:** Frost projectile (1.0×, 1.2 s cooldown)
**Active Skill:** Blizzard — 2.0× AoE (radius 3), 12 s cooldown
**Fusion:** + Voltaris → Tempest Glacion

---

#### Magmaclaw — Fire Flame · Epic
> Red/orange dragon with lava-crack scales, breathing a cone of fire.

| Stat | Value |
|------|-------|
| HP | 1100 |
| Attack | 260 |
| Armor | 70 |
| Range | 3.5 |
| Attack Speed | 1.0 attacks/s |
| Mana Cost | 80 |

**Normal Attack:** Fire projectile (1.2×, 1.0 s cooldown)
**Active Skill:** Magma Burst — 2.8× AoE (radius 2.5), 8 s cooldown

---

#### Tempest Glacion — Lightning Storm · Legendary *(Fusion Result)*
> Blue/gold dragon — left wing ice crystals, right wing lightning.

| Stat | Value |
|------|-------|
| HP | 1500 |
| Attack | 380 |
| Armor | 120 |
| Range | 5.5 |
| Attack Speed | 1.5 attacks/s |
| Mana Cost | 150 |

**Normal Attack:** Hybrid bolt (1.0×, 0.67 s cooldown)
**Active Skill:** Glacial Storm — 4.0× AoE (radius 4), 15 s cooldown

---

#### Stonehide — Earth · Rare
> Stocky mossy stone dragon with golden eyes. Defensive specialist.

| Stat | Value |
|------|-------|
| HP | 2200 |
| Attack | 130 |
| Armor | 200 |
| Range | 2.5 |
| Attack Speed | 0.5 attacks/s |
| Mana Cost | 75 |

**Normal Attack:** Boulder slam (1.5×, 2.0 s cooldown)
**Active Skill:** Fortify — support buff (1.0×), 20 s cooldown

---

#### Celestara — Light Celestial · Legendary
> White/gold feathered dragon with a golden halo and glowing orbs. Support/area control.

| Stat | Value |
|------|-------|
| HP | 1600 |
| Attack | 160 |
| Armor | 100 |
| Range | 6.0 |
| Attack Speed | 0.77 attacks/s |
| Mana Cost | 120 |

**Normal Attack:** Light ray (0.8×, 1.3 s cooldown)
**Active Skill:** Divine Aura — 1.5× AoE (radius 5), 18 s cooldown

---

#### Shadowfang — Shadow Abyssal · Epic
> Black/dark purple dragon with void flames and glowing violet eyes. High single-target burst.

| Stat | Value |
|------|-------|
| HP | 1050 |
| Attack | 240 |
| Armor | 80 |
| Range | 4.0 |
| Attack Speed | 1.25 attacks/s |
| Mana Cost | 95 |

**Normal Attack:** Void claw (1.1×, 0.8 s cooldown)
**Active Skill:** Void Strike — 3.2× single-target, 11 s cooldown

---

### Phase 1 Roster Summary

| Dragon | Element | Rarity | Role |
|--------|---------|--------|------|
| Voltaris | Lightning | Epic | DPS / Chain |
| Frostfang | Ice | Epic | AoE / Slow |
| Magmaclaw | Fire | Epic | Burst AoE |
| Tempest Glacion | Lightning | Legendary | Hybrid DPS |
| Stonehide | Earth | Rare | Tank / Anchor |
| Celestara | Light | Legendary | Support / AoE |
| Shadowfang | Shadow | Epic | Burst Single |

---

## 15. Development Phases

| Phase | Name | Status | Goal |
|-------|------|--------|------|
| 1 | **Prototype** | **Current** | Core TD loop playable · 3 dragon archetypes · 5 waves · 1 fusion |
| 2 | Vertical Slice | Planned | 1 full chapter (15 waves) · 10 dragons · gacha live · basic UI polish |
| 3 | Alpha | Planned | All systems integrated · backend live · 3 chapters · 30+ dragons |
| 4 | Beta | Planned | Soft launch · balance pass · leaderboards · first seasonal event |
| 5 | Launch | Planned | Global release · live-ops pipeline · first limited banner |

### Phase 1 Success Criteria

- [ ] All 5 waves completable with Phase 1 dragon roster
- [ ] Dragon placement, targeting, and skill activation working end-to-end
- [ ] Voltaris + Frostfang fusion produces Tempest Glacion
- [ ] GameDirector adjusting difficulty across a full 5-wave run
- [ ] Gacha single-pull and 10-pull functional with correct pity tracking
- [ ] Bond XP accumulating after battle and correctly advancing bond levels

# Dragon Depth Pass, Balance Pass, Railway Deployment — Design

Date: 2026-05-30

---

## System 1: Dragon Depth Pass

### Bond Stat Multipliers

`DragonInstance` exposes `Attack`, `AttackSpeed`, `Range` already. Add `GetBondStatMultiplier(int bondLevel) -> float` to `DragonProgression` (or inline in `DragonInstance`) returning an additive stat boost applied on top of base stats.

| Bond | Boost |
|------|-------|
| 1 | +0% |
| 2 | +5% |
| 3 | +8% |
| 4 | +12% |
| 5 | +18% |
| 6 | +25% + ultimate unlock |
| 7 | +35% |

Implementation: `DragonInstance.Attack` property multiplies by `(1 + bondBoost)`. Same for `AttackSpeed` and `Range`.

### Evolution Stage Stat Multipliers

Applied as a multiplier to the base stats from `DragonDefinition.baseStats`.

| Stage | Multiplier |
|-------|-----------|
| Hatchling | 1.00× |
| Young | 1.15× |
| Mature | 1.30× |
| Elder | 1.50× |
| Apex | 1.75× |
| Titan | 2.10× |

`DragonInstance.Attack` = `baseStats.attack × rarityMultiplier × evolutionMultiplier × (1 + bondBoost)`.

### Ultimate Skills (Bond 6 unlock, all 10 dragons)

Each ultimate is a `SkillDefinition` asset. `DragonTower` auto-casts the ultimate (separate from the active skill) when Bond >= 6 and cooldown elapsed. Ultimates are wave-only like active skills.

| Dragon | Ultimate Name | Effect | CD | Mult | AoE |
|--------|--------------|--------|----|------|-----|
| voltaris_001 | Thunderstorm | Chain lightning hits ALL enemies in range | 30s | 5.0× | false |
| frostfang_002 | Absolute Zero | Freeze all in-range enemies 3s | 35s | 2.0× | true, r=6 |
| magmaclaw_003 | Eruption | Massive AoE + 6s burn DoT | 28s | 4.0× | true, r=4 |
| tempest_glacion_004 | Glacial Tempest | Freeze + chain lightning combo | 40s | 6.0× | true, r=5 |
| stonehide_005 | Earthquake | Slow ALL on-screen 60% for 5s | 45s | 1.5× | true, r=12 |
| celestara_006 | Solar Flare | +30% range/speed to nearby towers 8s | 50s | 1.0× | true, r=5 |
| shadowfang_007 | Void Collapse | Pull nearby enemies in + 3× damage | 32s | 3.0× | true, r=4 |
| emberveil_008 | Celestial Inferno | Rain fire on 4 random enemies | 30s | 5.5× | false |
| tideclaw_009 | Maelstrom | AoE water explosion + 40% slow 4s | 28s | 3.5× | true, r=4 |
| zephyrwing_010 | Cyclone | 4× damage to flying, 50% slow ground | 25s | 4.0× | true, r=5 |

### Implementation approach

- Add `DragonInstance.UltimateSkill` property reading from `BondData.bondLevels[5].skillUnlockId` when BondLevel >= 6
- `DragonTower` gets `_lastUltimateCastTime` + checks in `Update()` same as active skill
- Create 10 `SkillDefinition` assets in `Assets/ScriptableObjects/Skills/Ultimates/`
- Wire into `DragonDefinition.bondData` via SceneBootstrapper's dragon definition builder

---

## System 2: Balance Pass

### Design target

- Chapter 1: Winnable with any 4 random dragons, no upgrades needed through wave 5
- Chapter 2: Requires at least 5 dragons with 1-2 upgrades by wave 8
- Chapter 3: Requires good role coverage + Bond 2+ by wave 10

### PrototypeBalance.cs changes

```
StartingMana: 300 → 320       (slightly easier start)
StartingGold: 130 → 150       (afford 1 upgrade sooner)
UpgradeBaseCost: 35 → 30      (upgrades feel accessible)
```

### Dragon DPS buff

Base attack multiplier +15% across the board via `rarityMultiplier` table in `PrototypeBalance`:

| Rarity | Current | New |
|--------|---------|-----|
| Common | 1.0 | 1.15 |
| Uncommon | 1.2 | 1.38 |
| Rare | 1.5 | 1.72 |
| Epic | 1.8 | 2.07 |
| Legendary | 2.2 | 2.53 |
| Mythic | 2.5 | 2.87 |
| Ancient | 3.0 | 3.45 |

### Wave difficulty adjustment

`WaveManager` applies `StageDifficultyMultiplier` to enemy HP and speed at spawn. Current values are correct in `StageCatalog`. The issue is the Chapter 1 early waves use low base HP enemies that die too slowly because projectile damage is low.

Fix: increase `ProjectileBaseDamage` multiplier in `PrototypeBalance` (or absorb into rarity multipliers above). The rarity buff above covers this — no wave asset changes needed.

### Reward scaling

Gold/mana rewards in Chapter 1 waves 01-05 bump slightly:

| Wave | Gold (current) | Gold (new) | Mana (current) | Mana (new) |
|------|---------------|-----------|----------------|-----------|
| 01 | 120 | 140 | 90 | 105 |
| 02 | 165 | 190 | 115 | 135 |
| 03 | 300 | 320 | 190 | 210 |
| 04 | 420 | 440 | 240 | 260 |
| 05 | 600 | 620 | 320 | 340 |

Chapters 2 and 3 rewards already scale correctly via `rewardMultiplier` in `StageCatalog`. No changes needed there.

---

## System 3: Railway Deployment

### Files

- `Backend/Dockerfile` — multi-stage .NET 9 build + runtime image
- `Backend/railway.toml` — Railway build/start config, healthcheck
- `Backend/DragonTD.API/appsettings.Production.json` — reads from env vars

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish DragonTD.API/DragonTD.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "DragonTD.API.dll"]
```

### railway.toml

```toml
[build]
builder = "DOCKERFILE"
dockerfilePath = "Dockerfile"

[deploy]
startCommand = "dotnet DragonTD.API.dll"
healthcheckPath = "/health"
healthcheckTimeout = 30
```

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": ""
  }
}
```

Both read from Railway environment variables `ConnectionStrings__DefaultConnection` and `Jwt__Secret`.

### HealthCheck endpoint

Add a `/health` GET endpoint to `Program.cs` returning 200 OK — Railway uses it to confirm deployment success.

### Railway setup steps (user does these, one-time)

1. railway.app → New Project → Deploy from GitHub
2. Select the repo, set root directory to `Backend/`
3. Add PostgreSQL service → Railway auto-sets `DATABASE_URL`
4. Set env var `ConnectionStrings__DefaultConnection` = `${{Postgres.DATABASE_URL}}`
5. Set env var `Jwt__Secret` = a 32+ char secret
6. Deploy — Railway runs `dotnet ef database update` is NOT automatic; add a release command or run migrations via the Railway shell once after first deploy

### Migration strategy

Add to `Program.cs` startup (production only):
```csharp
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```
This auto-migrates on every deploy — safe since EF Core migrations are idempotent.

---

## Self-review

**Placeholder scan:** No TBDs. All 10 ultimates specified with cooldowns and multipliers. Balance numbers are concrete. Dockerfile is complete.

**Internal consistency:** Bond boost uses additive formula consistent with existing `DragonInstance` pattern. Evolution multipliers stack correctly. Railway env var names match `appsettings.json` convention (`__` as section separator).

**Scope:** Three independent systems, each self-contained. Can be implemented in any order. Dragon depth and balance are Unity-side; deployment is backend-side.

**Ambiguity:** `Solar Flare` (Celestara) buffs nearby towers — this needs a mechanism. Implemented as: `DragonTower` checks for nearby allied towers in an AoE pulse, temporarily sets `_damageMultiplier` and `_rangeBonus` on them. If too complex, simplify to self-buff only.

# Dragon Depth, Balance Pass, Railway Deployment — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add evolution stage stat scaling and Bond-6 ultimate skills to all 10 dragons, tune game balance for better feel, and deploy the .NET 9 backend to Railway.

**Architecture:** Dragon depth is additive to existing `DragonInstance` computed properties — evolution multiplier and ultimate skill property are new, no existing behavior breaks. Balance tweaks are value changes in `PrototypeBalanceConfig` + `SceneBootstrapper`. Railway deployment is 3 new files + 2 code additions with no schema changes.

**Tech Stack:** Unity 6 C#, ScriptableObjects, .NET 9 / ASP.NET Core, EF Core, Railway (PaaS), Docker.

---

## File Structure

| Action | Path |
|--------|------|
| Modify | `Unity/Assets/Scripts/Dragons/DragonInstance.cs` — add `EvolutionMultiplier`, `UltimateSkill`, `GlobalDamageBalanceMultiplier` |
| Modify | `Unity/Assets/Scripts/TowerDefense/PrototypeBalanceConfig.cs` — add `globalDamageBalance` field |
| Modify | `Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs` — expose `GlobalDamageBalance` |
| Modify | `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs` — auto-cast ultimate |
| Modify | `Unity/Assets/Editor/SceneBootstrapper.cs` — create ultimate skill assets, update balance defaults, bump wave rewards |
| Create | `Backend/Dockerfile` |
| Create | `Backend/railway.toml` |
| Modify | `Backend/DragonTD.API/appsettings.Production.json` |
| Modify | `Backend/DragonTD.API/Program.cs` — health endpoint + auto-migration |

---

## Task 1: Evolution Stage Stat Multiplier

**Files:**
- Modify: `Unity/Assets/Scripts/Dragons/DragonInstance.cs`

- [ ] **Step 1: Add EvolutionMultiplier property to DragonInstance**

In `DragonInstance.cs`, add this property after `LevelMultiplier`:

```csharp
        public static float GetEvolutionMultiplier(DragonEvolutionStage stage) => stage switch
        {
            DragonEvolutionStage.Hatchling => 1.00f,
            DragonEvolutionStage.Young     => 1.15f,
            DragonEvolutionStage.Mature    => 1.30f,
            DragonEvolutionStage.Elder     => 1.50f,
            DragonEvolutionStage.Apex      => 1.75f,
            DragonEvolutionStage.Titan     => 2.10f,
            _                              => 1.00f
        };

        private float EvolutionMultiplier => GetEvolutionMultiplier(EvolutionStage);
```

- [ ] **Step 2: Apply EvolutionMultiplier to Attack, Hp, Defense**

Update the three stat properties:

```csharp
        public float Hp      => Definition.baseStats.hp     * LevelMultiplier * BondStatMultiplier * EvolutionMultiplier;
        public float Attack  => Definition.baseStats.attack  * LevelMultiplier * BondStatMultiplier * EvolutionMultiplier * AccountDamageMultiplier;
        public float Defense => Definition.baseStats.armor   * LevelMultiplier * BondStatMultiplier * EvolutionMultiplier;
```

Leave `Range` and `AttackSpeed` unchanged (evolution doesn't increase range or speed).

- [ ] **Step 3: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/Dragons/DragonInstance.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: evolution stage stat multipliers on DragonInstance (Hatchling 1.0x → Titan 2.1x)"
```

---

## Task 2: Global Balance Buff (+15% damage)

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/PrototypeBalanceConfig.cs`
- Modify: `Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs`
- Modify: `Unity/Assets/Scripts/Dragons/DragonInstance.cs`

- [ ] **Step 1: Add globalDamageBalance to PrototypeBalanceConfig**

In `PrototypeBalanceConfig.cs`, add under the `[Header("Tower Progression")]` section:

```csharp
        [Header("Global Balance")]
        public float globalDamageBalance = 1.15f;
        public int startingMana = 320;
        public int startingGold = 150;
        public int upgradeBaseCost = 30;
```

**Note:** This replaces the existing `startingMana = 300`, `startingGold = 130`, `upgradeBaseCost = 35` defaults. Change only those three values' defaults — keep all other fields unchanged.

- [ ] **Step 2: Expose in PrototypeBalance**

In `PrototypeBalance.cs`, add after `UpgradeBaseCost`:

```csharp
        public static float GlobalDamageBalance => Config != null ? Config.globalDamageBalance : 1.15f;
```

Also update the existing fallback defaults inline:
```csharp
        public static int StartingMana => Config != null ? Config.startingMana : 320;
        public static int StartingGold => Config != null ? Config.startingGold : 150;
        public static int UpgradeBaseCost => Config != null ? Config.upgradeBaseCost : 30;
```

- [ ] **Step 3: Apply GlobalDamageBalance in DragonInstance.Attack**

Update Attack property to include the global multiplier:

```csharp
        public float Attack  => Definition.baseStats.attack  * LevelMultiplier * BondStatMultiplier * EvolutionMultiplier * PrototypeBalance.GlobalDamageBalance * AccountDamageMultiplier;
```

- [ ] **Step 4: Update PrototypeBalanceConfig asset defaults in SceneBootstrapper**

In `Unity/Assets/Editor/SceneBootstrapper.cs`, find `CreatePrototypeBalanceConfig`. Change it to always update the key values (not just create-if-missing):

```csharp
        static PrototypeBalanceConfig CreatePrototypeBalanceConfig()
        {
            const string path = "Assets/Resources/PrototypeBalanceConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<PrototypeBalanceConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<PrototypeBalanceConfig>();
                AssetDatabase.CreateAsset(cfg, path);
            }
            cfg.startingMana       = 320;
            cfg.startingGold       = 150;
            cfg.upgradeBaseCost    = 30;
            cfg.globalDamageBalance = 1.15f;
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            return cfg;
        }
```

- [ ] **Step 5: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/PrototypeBalanceConfig.cs Unity/Assets/Scripts/TowerDefense/PrototypeBalance.cs Unity/Assets/Scripts/Dragons/DragonInstance.cs Unity/Assets/Editor/SceneBootstrapper.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: +15% global damage balance, bump starting mana/gold, reduce upgrade cost"
```

---

## Task 3: Wave 01–05 Reward Tweaks

**Files:**
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`

- [ ] **Step 1: Update Wave01–Wave05 reward values**

Find the five `CreateWave("Wave01"…"Wave05"…)` calls in `Build()` and update gold/mana rewards:

```csharp
            CreateWave("Wave01", 140, 105,   // was 120, 90
                new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 4, SpawnInterval = 1.25f },
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 5, SpawnInterval = 0.82f });
            CreateWave("Wave02", 190, 135,   // was 165, 115
                new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 5, SpawnInterval = 1.0f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 2, SpawnInterval = 1.3f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 3, SpawnInterval = 1.1f });
            CreateWave("Wave03", 320, 210,   // was 300, 190
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 5, SpawnInterval = 0.64f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 5, SpawnInterval = 0.9f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 5, SpawnInterval = 0.82f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 3, SpawnInterval = 1.08f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 2, SpawnInterval = 0.7f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 2, SpawnInterval = 0.7f });
            CreateWave("Wave04", 440, 260,   // was 420, 240
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 6, SpawnInterval = 0.72f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 5, SpawnInterval = 0.82f },
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 8, SpawnInterval = 0.52f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 4, SpawnInterval = 0.92f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 3, SpawnInterval = 0.62f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 4, SpawnInterval = 0.58f });
            CreateWave("Wave05", 620, 340,   // was 600, 320
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 10, SpawnInterval = 0.42f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 6, SpawnInterval = 0.68f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 7, SpawnInterval = 0.66f },
```

Keep the rest of Wave05's enemy entries exactly as they are — only change the gold/mana values.

- [ ] **Step 2: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Editor/SceneBootstrapper.cs
git -C "D:/DragonTD/DragonTD" commit -m "balance: bump Ch1 Wave01-05 gold/mana rewards for better early pacing"
```

---

## Task 4: UltimateSkill Property on DragonInstance

**Files:**
- Modify: `Unity/Assets/Scripts/Dragons/DragonInstance.cs`

- [ ] **Step 1: Add UltimateSkill property**

Add after `SkillLevel` field and before `Hp` property:

```csharp
        // Returns the ultimate skill only when Bond 6+ is reached.
        public SkillDefinition UltimateSkill =>
            BondLevel >= 6 ? Definition?.skillSet?.ultimateSkill : null;
```

- [ ] **Step 2: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/Dragons/DragonInstance.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: UltimateSkill property on DragonInstance, unlocks at Bond 6"
```

---

## Task 5: DragonTower Ultimate Auto-Cast

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs`

- [ ] **Step 1: Add ultimate cast fields**

After `_nextInheritedPassiveTime`, add:

```csharp
        private float _ultimateCooldown;
        private float _lastUltimateCastTime = float.NegativeInfinity;
```

- [ ] **Step 2: Initialize ultimate cooldown in Setup**

In `Setup(DragonInstance instance, GridTile placedTile, int manaCost)`, after `_activeSkillCooldown` is set, add:

```csharp
            _ultimateCooldown = instance.UltimateSkill != null ? instance.UltimateSkill.cooldown : 999f;
```

- [ ] **Step 3: Auto-cast ultimate in Update**

In `Update()`, after the existing `_autoCastActiveSkill` block, add:

```csharp
            if (_dragonInstance.UltimateSkill != null &&
                GameManager.Instance?.State == GameState.Wave &&
                Time.time - _lastUltimateCastTime >= _ultimateCooldown)
            {
                EnemyBase ultimateTarget = FindNearestEnemy();
                if (ultimateTarget != null)
                    TryCastUltimate(ultimateTarget);
            }
```

- [ ] **Step 4: Add TryCastUltimate method**

Add after `TryCastActiveSkill`:

```csharp
        public bool TryCastUltimate(EnemyBase target)
        {
            SkillDefinition ultimate = _dragonInstance?.UltimateSkill;
            if (ultimate == null) return false;
            if (target == null || target.IsDead) return false;
            if (GameManager.Instance?.State != GameState.Wave) return false;

            AbilityExecutor.ExecuteActiveSkill(ultimate, _dragonInstance, target, transform.position, _damageMultiplier * LevelDamageMultiplier, _statusMagnitudeMultiplier);
            BattleStatsTracker.Instance?.RecordSkillCast();
            _lastUltimateCastTime = Time.time;

            DamageIndicator.SpawnText(transform.position + Vector3.up * 1.55f, "ULTIMATE!", PrototypeBalance.WeakFeedbackColor);
            return true;
        }
```

- [ ] **Step 5: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: DragonTower auto-casts ultimate skill when Bond >= 6"
```

---

## Task 6: Create 10 Ultimate SkillDefinition Assets in SceneBootstrapper

**Files:**
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`

- [ ] **Step 1: Add CreateUltimateSkill helper method**

Add near `CreateActiveSkill` and `CreateNormalAttack` in SceneBootstrapper:

```csharp
        static SkillDefinition CreateUltimateSkill(Phase1DragonData.Def dragon)
        {
            string id = dragon.Id + "_ultimate";
            string path = SODir + "/Skills/" + id + ".asset";
            var sk = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (sk == null) { sk = ScriptableObject.CreateInstance<SkillDefinition>(); AssetDatabase.CreateAsset(sk, path); }

            (string name, float cd, float mult, bool aoe, float radius, string desc) = dragon.Id switch
            {
                "voltaris_001"       => ("Thunderstorm",      30f, 5.0f, false, 0f,  "Chain lightning hits ALL enemies in range"),
                "frostfang_002"      => ("Absolute Zero",     35f, 2.0f, true,  6f,  "Freeze all in-range enemies for 3 seconds"),
                "magmaclaw_003"      => ("Eruption",          28f, 4.0f, true,  4f,  "Massive AoE blast + 6s burn on all hit enemies"),
                "tempest_glacion_004"=> ("Glacial Tempest",   40f, 6.0f, true,  5f,  "Freeze + chain lightning — the ultimate fusion"),
                "stonehide_005"      => ("Earthquake",        45f, 1.5f, true,  12f, "Slows ALL on-screen enemies 60% for 5 seconds"),
                "celestara_006"      => ("Solar Flare",       50f, 1.0f, true,  5f,  "Heals and buffs nearby allied towers for 8 seconds"),
                "shadowfang_007"     => ("Void Collapse",     32f, 3.0f, true,  4f,  "Pulls nearby enemies in and deals triple damage"),
                "emberveil_008"      => ("Celestial Inferno", 30f, 5.5f, false, 0f,  "Rains celestial fire on 4 random enemies"),
                "tideclaw_009"       => ("Maelstrom",         28f, 3.5f, true,  4f,  "AoE water explosion + 40% slow for 4 seconds"),
                "zephyrwing_010"     => ("Cyclone",           25f, 4.0f, true,  5f,  "4x damage to flying enemies, slows ground 50%"),
                _                   => ("Ultimate",           30f, 3.0f, false, 0f,  "Powerful ultimate ability")
            };

            sk.skillId     = id;
            sk.displayName = name;
            sk.description = desc;
            sk.skillType   = SkillType.Damage;
            sk.targetType  = aoe ? TargetType.AoE : TargetType.Chain;
            sk.cooldown    = cd;
            sk.isAoe       = aoe;
            sk.aoeRadius   = radius;
            sk.range       = dragon.SkillRadius > 0 ? dragon.SkillRadius : dragon.AtkCd > 0 ? 5f : 4f;
            sk.levelMultipliers = new float[]{ mult, mult*1.05f, mult*1.10f, mult*1.15f, mult*1.20f,
                                               mult*1.26f, mult*1.32f, mult*1.38f, mult*1.45f, mult*1.55f };
            EditorUtility.SetDirty(sk);
            return sk;
        }
```

- [ ] **Step 2: Call CreateUltimateSkill and assign to DragonDefinition**

In `Build()`, inside the `foreach (var dragon in Phase1DragonData.All)` loop, find where `CreateDragonDef(dragon)` is called. After it, the DragonDefinition gets its skills wired. Find `CreateDragonDef` in SceneBootstrapper and locate where `so.FindProperty("skillSet")` is set. After assigning `normalAttack` and `activeSkill`, add:

```csharp
            var ultimateSkill = CreateUltimateSkill(dragon);
            skillSet.FindPropertyRelative("ultimateSkill").objectReferenceValue = ultimateSkill;
```

**Note:** Read `CreateDragonDef` carefully first to find the exact `SerializedObject` variable name and the `skillSet` property path. The structure is: `var skillSet = so.FindProperty("skillSet"); skillSet.FindPropertyRelative("normalAttack")...`. Add the ultimate assignment in the same block.

- [ ] **Step 3: Verify compile**

Open Unity editor, wait for recompile, check Console for errors.

- [ ] **Step 4: Rebuild scene**

Run `Dragon Dominion > ★ Build Battle Scene` in Unity editor to generate all 10 ultimate SkillDefinition assets and wire them.

- [ ] **Step 5: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Editor/SceneBootstrapper.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: create 10 ultimate skill assets in SceneBootstrapper, wire into DragonDefinitions"
```

---

## Task 7: Railway — Dockerfile + railway.toml

**Files:**
- Create: `Backend/Dockerfile`
- Create: `Backend/railway.toml`

- [ ] **Step 1: Create Dockerfile**

Create `Backend/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish DragonTD.API/DragonTD.API.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "DragonTD.API.dll"]
```

- [ ] **Step 2: Create railway.toml**

Create `Backend/railway.toml`:

```toml
[build]
builder = "DOCKERFILE"
dockerfilePath = "Dockerfile"

[deploy]
healthcheckPath = "/health"
healthcheckTimeout = 60
restartPolicyType = "ON_FAILURE"
restartPolicyMaxRetries = 3
```

- [ ] **Step 3: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Backend/Dockerfile Backend/railway.toml
git -C "D:/DragonTD/DragonTD" commit -m "chore: add Dockerfile and railway.toml for Railway deployment"
```

---

## Task 8: Health Endpoint + Auto-Migration

**Files:**
- Modify: `Backend/DragonTD.API/Program.cs`
- Create: `Backend/DragonTD.API/appsettings.Production.json`

- [ ] **Step 1: Add health endpoint and auto-migration to Program.cs**

In `Program.cs`, after `app.MapControllers();` and before `app.Run();`, add:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Auto-migrate on startup in Production — EF migrations are idempotent
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

- [ ] **Step 2: Create appsettings.Production.json**

Create `Backend/DragonTD.API/appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "DragonTD.API",
    "Audience": "DragonTD.Client"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Both `DefaultConnection` and `Jwt:Secret` are empty here — Railway injects them as environment variables (`ConnectionStrings__DefaultConnection` and `Jwt__Secret`).

- [ ] **Step 3: Verify backend builds in Docker locally (optional but recommended)**

```powershell
cd D:\DragonTD\DragonTD\Backend
docker build -t dragon-dominion-api .
```

Expected: `Successfully built <hash>` with no errors.

- [ ] **Step 4: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Backend/DragonTD.API/Program.cs Backend/DragonTD.API/appsettings.Production.json
git -C "D:/DragonTD/DragonTD" commit -m "feat: add /health endpoint and production auto-migration; add appsettings.Production.json"
```

---

## Task 9: Railway Project Setup (user action)

These steps are performed by the user in the Railway dashboard — no code to write.

- [ ] **Step 1: Create Railway project**

1. Go to railway.app → New Project → **Deploy from GitHub repo**
2. Select the `DragonTD` repository
3. Set **Root Directory** to `Backend/`
4. Railway auto-detects the Dockerfile

- [ ] **Step 2: Add PostgreSQL service**

In the Railway project → **+ New** → Database → **PostgreSQL**. Railway creates a Postgres instance and exposes `${{Postgres.DATABASE_URL}}`.

- [ ] **Step 3: Set environment variables**

In the API service → Variables tab, add:

| Variable | Value |
|----------|-------|
| `ConnectionStrings__DefaultConnection` | `${{Postgres.DATABASE_URL}}` |
| `Jwt__Secret` | Any 32+ character random string (e.g. generate at `generate-secret.vercel.app`) |

- [ ] **Step 4: Deploy**

Click **Deploy** (or push to the branch). Railway builds the Dockerfile, starts the container. First startup runs `db.Database.Migrate()` automatically.

- [ ] **Step 5: Verify**

```powershell
# Replace <railway-domain> with the generated domain shown in Railway dashboard
Invoke-RestMethod -Uri "https://<railway-domain>/health"
```

Expected: `{ "status": "healthy", "timestamp": "2026-..." }`

---

## Task 10: Update Handoff Doc

**Files:**
- Modify: `docs/2026-05-27-dragon-dominion-prototype-handoff.md`

- [ ] **Step 1: Append section before Main Files**

Add before `## Main Files To Read First`:

```markdown
## Dragon Depth, Balance, Railway — 2026-05-30

**Dragon Depth:**
- Evolution stage stat multipliers: Hatchling 1.0× → Titan 2.10×. Applied in `DragonInstance.Hp/Attack/Defense`.
- Global damage balance +15% via `PrototypeBalance.GlobalDamageBalance = 1.15f`.
- `DragonInstance.UltimateSkill` returns the ultimate `SkillDefinition` only when `BondLevel >= 6`.
- `DragonTower` auto-casts the ultimate in waves (separate cooldown from active skill), shows "ULTIMATE!" text.
- 10 ultimate SkillDefinition assets generated by SceneBootstrapper under `Assets/ScriptableObjects/Skills/`.

**Balance:**
- Starting resources: Mana 300→320, Gold 130→150.
- Upgrade base cost: 35→30.
- Chapter 1 Wave 01-05 gold/mana rewards bumped +10–17%.
- `PrototypeBalanceConfig` now always updated on Build Battle Scene.

**Railway Deployment:**
- `Backend/Dockerfile` + `Backend/railway.toml` — deploys via Git push.
- `/health` endpoint returns `{ status, timestamp }`.
- Production startup auto-runs EF migrations.
- Railway env vars: `ConnectionStrings__DefaultConnection = ${{Postgres.DATABASE_URL}}`, `Jwt__Secret`.
- Pending: user must create Railway project, add PostgreSQL service, set env vars (see Task 9).
```

- [ ] **Step 2: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add docs/2026-05-27-dragon-dominion-prototype-handoff.md
git -C "D:/DragonTD/DragonTD" commit -m "docs: update handoff for dragon depth, balance pass, Railway deployment"
git -C "D:/DragonTD/DragonTD" fetch . main:claude/dragon-tower-defense-rpg-okxJw
```

---

## Self-Review

**Spec coverage:**
- Evolution multipliers → Task 1. ✓
- +15% damage buff → Tasks 2+3. ✓
- Starting resources bump → Task 2. ✓
- Wave rewards → Task 3. ✓
- All 10 ultimates with cooldowns/multipliers → Task 6. ✓
- UltimateSkill Bond-6 gate → Task 4. ✓
- DragonTower auto-cast → Task 5. ✓
- Dockerfile + railway.toml → Task 7. ✓
- Health endpoint + auto-migration → Task 8. ✓
- appsettings.Production.json → Task 8. ✓

**Placeholder scan:** All steps have concrete code. Task 9 is user-action steps (no code to write, by design). Task 6 Step 2 says "read CreateDragonDef carefully first" — this is necessary instruction, not a placeholder.

**Type consistency:**
- `UltimateSkill` property defined Task 4 → consumed in Task 5 (`_dragonInstance.UltimateSkill`) and Task 6 (SceneBootstrapper wires it). ✓
- `PrototypeBalance.GlobalDamageBalance` defined Task 2 → applied Task 2 Step 3. ✓
- `_ultimateCooldown` field added Task 5 Step 1, initialized Task 5 Step 2, used Task 5 Step 3. ✓
- `CreateUltimateSkill` defined Task 6 Step 1, called Task 6 Step 2. ✓

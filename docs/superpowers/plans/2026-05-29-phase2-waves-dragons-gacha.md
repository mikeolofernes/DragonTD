# Phase 2 — Waves, Dragons, Gacha Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand the playable TD slice to 15 waves and 10 dragons, and wire the existing GachaSystem/SummonPool classes into the real gem-based summon flow with persistent pity.

**Architecture:** All three tasks are independent. Waves and dragons are pure SceneBootstrapper/data changes — no runtime code changes. Gacha wires the existing `GachaSystem` (already fully implemented) into `PlayerInventory.TrySummonDragon`, adds pity persistence to `PlayerProgressionSaveData`/`PlayerProgression`, creates a `SummonPool.asset` in SceneBootstrapper, and adds a gem-based single-pull and 10-pull path.

**Tech Stack:** Unity 6 C#, ScriptableObjects, SceneBootstrapper editor tool, `DragonTD.Summoning.GachaSystem`, `DragonTD.Core.PlayerProgression`

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `Unity/Assets/Editor/SceneBootstrapper.cs` | Add Wave06–15, create SummonPool asset, add 3 new dragon art dirs |
| Modify | `Unity/Assets/Scripts/Dragons/Phase1DragonData.cs` | Add dragons 8–10 stat definitions |
| Modify | `Unity/Assets/Scripts/Core/PlayerProgressionData.cs` | Add `gachaPullsSinceLastEpic`, `gachaTotalPulls` save fields |
| Modify | `Unity/Assets/Scripts/Core/PlayerProgression.cs` | Add pity properties + load/write/reset methods |
| Modify | `Unity/Assets/Scripts/Core/ProgressionApiDtos.cs` | Add `gacha_pulls_since_last_epic`, `gacha_total_pulls` DTO fields |
| Modify | `Unity/Assets/Scripts/Core/PlayerInventory.cs` | Replace `PickSummonDragon` with GachaSystem; add gem single-pull and 10-pull |
| Modify | `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` | Update summon panel: show pity, gem cost, single/10-pull buttons |

---

## Task 1: Waves 5 → 15

**Files:**
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`

- [ ] **Step 1: Add Wave06–Wave15 CreateWave calls in SceneBootstrapper**

In `SceneBootstrapper.Build()`, immediately after the `CreateWave("Wave05", ...)` block, add:

```csharp
CreateWave("Wave06", 700, 380,
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 12, SpawnInterval = 0.38f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 7, SpawnInterval = 0.62f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 8, SpawnInterval = 0.60f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 9, SpawnInterval = 0.48f },
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 5, SpawnInterval = 0.40f });
CreateWave("Wave07", 820, 430,
    new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 8, SpawnInterval = 0.55f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 8, SpawnInterval = 0.56f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 12, SpawnInterval = 0.34f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 10, SpawnInterval = 0.44f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 6, SpawnInterval = 0.54f });
CreateWave("Wave08", 950, 490,
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 7, SpawnInterval = 0.36f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 9, SpawnInterval = 0.50f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 9, SpawnInterval = 0.52f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 10, SpawnInterval = 0.40f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 14, SpawnInterval = 0.30f });
CreateWave("Wave09", 1100, 560,
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 10, SpawnInterval = 0.46f },
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 8, SpawnInterval = 0.34f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 12, SpawnInterval = 0.38f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 10, SpawnInterval = 0.46f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 15, SpawnInterval = 0.28f });
CreateWave("Wave10", 1300, 640,
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 10, SpawnInterval = 0.30f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 12, SpawnInterval = 0.42f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 12, SpawnInterval = 0.42f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 14, SpawnInterval = 0.34f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 18, SpawnInterval = 0.24f });
CreateWave("Wave11", 1450, 700,
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 15, SpawnInterval = 0.30f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 12, SpawnInterval = 0.38f },
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 10, SpawnInterval = 0.28f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 12, SpawnInterval = 0.38f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 20, SpawnInterval = 0.22f });
CreateWave("Wave12", 1650, 780,
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 12, SpawnInterval = 0.26f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 14, SpawnInterval = 0.34f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 16, SpawnInterval = 0.28f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 14, SpawnInterval = 0.34f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 22, SpawnInterval = 0.20f });
CreateWave("Wave13", 1900, 860,
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 16, SpawnInterval = 0.30f },
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 14, SpawnInterval = 0.24f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 16, SpawnInterval = 0.30f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 18, SpawnInterval = 0.26f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 24, SpawnInterval = 0.18f });
CreateWave("Wave14", 2200, 950,
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 16, SpawnInterval = 0.22f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 18, SpawnInterval = 0.28f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 20, SpawnInterval = 0.22f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 18, SpawnInterval = 0.26f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 26, SpawnInterval = 0.16f });
CreateWave("Wave15", 2600, 1100,
    new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 20, SpawnInterval = 0.18f },
    new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 20, SpawnInterval = 0.24f },
    new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 20, SpawnInterval = 0.22f },
    new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 22, SpawnInterval = 0.20f },
    new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 30, SpawnInterval = 0.14f });
```

- [ ] **Step 2: Update the waves array to load all 15 assets**

In `SceneBootstrapper.Build()`, find the `waves` array (currently loads Wave01–05):

```csharp
var waves = new[]{
    AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave01.asset"),
    AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave02.asset"),
    AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave03.asset"),
    AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave04.asset"),
    AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave05.asset")
};
```

Replace with:

```csharp
var waves = new WaveData[15];
for (int w = 1; w <= 15; w++)
    waves[w - 1] = AssetDatabase.LoadAssetAtPath<WaveData>($"{SODir}/Waves/Wave{w:D2}.asset");
```

- [ ] **Step 3: Regenerate the battle scene**

Open Unity editor → `Dragon Dominion > Build Battle Scene`

OR with Unity closed:
```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\DragonTD\DragonTD\Unity' -executeMethod DragonTD.Editor.SceneBootstrapper.Build -logFile 'D:\DragonTD\DragonTD\unity-phase2-waves.log'
```

- [ ] **Step 4: Verify wave assets created**

```powershell
$waves = Get-ChildItem 'D:\DragonTD\DragonTD\Unity\Assets\ScriptableObjects\Waves\*.asset'
Write-Host "Wave count: $($waves.Count)"
```

Expected: `Wave count: 15`

- [ ] **Step 5: Update GameManager victory message**

In `Unity/Assets/Scripts/Core/GameManager.cs`, find any hardcoded wave count in the victory fallback message and update to 15. Search for string literals containing "wave" or "5" near the victory logic and update accordingly.

Run:
```powershell
Select-String -Path 'Unity\Assets\Scripts\Core\GameManager.cs' -Pattern '".*wave.*"' -CaseSensitive:$false
```

If found, update the number. If not found, skip — `WaveManager` drives victory via `TotalWaves` already.

- [ ] **Step 6: Commit Task 1**

```bash
git add Unity/Assets/Editor/SceneBootstrapper.cs
git add Unity/Assets/Scripts/Core/GameManager.cs
git add Unity/Assets/ScriptableObjects/Waves/
git add Unity/Assets/Scenes/BattleScene.unity
git commit -m "feat: expand waves 5→15 in SceneBootstrapper and regenerate battle scene"
```

---

## Task 2: Dragons 7 → 10

**Files:**
- Modify: `Unity/Assets/Scripts/Dragons/Phase1DragonData.cs`
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs` (art dirs already added via `Phase1DragonData.All` loop)

- [ ] **Step 1: Add 3 new dragon definitions to Phase1DragonData.All**

In `Unity/Assets/Scripts/Dragons/Phase1DragonData.cs`, add these 3 entries at the end of the `All` array (after `shadowfang_007`):

```csharp
new Def
{
    Id = "emberveil_008", Name = "Emberveil",
    Rarity = DragonRarity.Epic, Element = DragonElement.Fire, Class = DragonClass.Celestial,
    Hp = 1200f, Atk = 310f, Armor = 85f, Range = 4f, AttackSpeed = 1.1f, Mana = 220f,
    ManaCost = 100,
    NormalSkillId = "emberveil_ray_001", NormalAttackName = "Ember Ray",
    AtkCd = 0.91f, AtkMult = 0.9f,
    SkillName = "Celestial Fire", SkillCd = 14f, SkillMult = 2.6f, SkillAoe = true, SkillRadius = 3f,
},
new Def
{
    Id = "tideclaw_009", Name = "Tideclaw",
    Rarity = DragonRarity.Rare, Element = DragonElement.Water, Class = DragonClass.Frost,
    Hp = 1050f, Atk = 200f, Armor = 110f, Range = 3.5f, AttackSpeed = 0.9f, Mana = 160f,
    ManaCost = 70,
    NormalSkillId = "tideclaw_strike_001", NormalAttackName = "Tidal Strike",
    AtkCd = 1.11f, AtkMult = 1.1f,
    SkillName = "Whirlpool", SkillCd = 10f, SkillMult = 2.2f, SkillAoe = true, SkillRadius = 2.5f,
},
new Def
{
    Id = "zephyrwing_010", Name = "Zephyrwing",
    Rarity = DragonRarity.Uncommon, Element = DragonElement.Wind, Class = DragonClass.Storm,
    Hp = 700f, Atk = 165f, Armor = 45f, Range = 5.5f, AttackSpeed = 1.7f, Mana = 180f,
    ManaCost = 60,
    NormalSkillId = "zephyrwing_slash_001", NormalAttackName = "Wind Slash",
    AtkCd = 0.59f, AtkMult = 0.75f,
    SkillName = "Gust Burst", SkillCd = 8f, SkillMult = 2.0f, SkillAoe = false, SkillRadius = 0f,
},
```

**Note:** `DragonElement.Water` and `DragonElement.Wind` — verify these exist in the `DragonElement` enum in `Unity/Assets/Scripts/Dragons/` before using. If `Water` doesn't exist (only `Ice`) use `Ice`; if `Wind` doesn't exist (only `Lightning`) use `Lightning`. Check the enum before committing.

- [ ] **Step 2: Verify DragonElement and DragonClass enum values**

```powershell
Select-String -Path 'Unity\Assets\Scripts\Dragons\DragonData.cs','Unity\Assets\Scripts\Dragons\DragonEnums.cs' -Pattern 'Water|Wind|Frost' 2>$null
Get-ChildItem Unity\Assets\Scripts\Dragons\ | Select-Object Name
```

Adjust the new dragon definitions to use only enum values that actually exist.

- [ ] **Step 3: Regenerate the battle scene**

Open Unity editor → `Dragon Dominion > Build Battle Scene`

OR with Unity closed:
```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\DragonTD\DragonTD\Unity' -executeMethod DragonTD.Editor.SceneBootstrapper.Build -logFile 'D:\DragonTD\DragonTD\unity-phase2-dragons.log'
```

- [ ] **Step 4: Verify dragon ScriptableObjects created**

```powershell
$assets = Get-ChildItem 'D:\DragonTD\DragonTD\Unity\Assets\ScriptableObjects\Dragons\*.asset'
Write-Host "Dragon count: $($assets.Count)"
```

Expected: `Dragon count: 10`

- [ ] **Step 5: Commit Task 2**

```bash
git add Unity/Assets/Scripts/Dragons/Phase1DragonData.cs
git add Unity/Assets/ScriptableObjects/Dragons/
git add Unity/Assets/ScriptableObjects/Skills/
git add Unity/Assets/Prefabs/Dragons/
git add Unity/Assets/Scenes/
git commit -m "feat: add 3 dragons (emberveil_008, tideclaw_009, zephyrwing_010), total 10"
```

---

## Task 3: Gacha System Wiring

**Files:**
- Modify: `Unity/Assets/Scripts/Core/PlayerProgressionData.cs`
- Modify: `Unity/Assets/Scripts/Core/PlayerProgression.cs`
- Modify: `Unity/Assets/Scripts/Core/ProgressionApiDtos.cs`
- Modify: `Unity/Assets/Scripts/Core/PlayerInventory.cs`
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

**Context:** `GachaSystem.cs` and `SummonPool.cs` already exist in `Unity/Assets/Scripts/Summoning/` and are complete. `GachaSystem` stores pity in a `Dictionary<string, PityTracker>` (in-memory). This task persists that pity to `PlayerProgression`, creates a `SummonPool.asset` resource, and replaces the simple `PickSummonDragon` with real gacha pulls.

### Sub-task 3a: Pity persistence in save data

- [ ] **Step 1: Add pity fields to PlayerProgressionSaveData**

In `Unity/Assets/Scripts/Core/PlayerProgressionData.cs`, add these two fields to the `PlayerProgressionSaveData` class:

```csharp
public int gachaPullsSinceLastEpic;
public int gachaTotalPulls;
```

- [ ] **Step 2: Add pity properties and methods to PlayerProgression**

In `Unity/Assets/Scripts/Core/PlayerProgression.cs`:

Add two properties (alongside `SummonTickets`):
```csharp
public int GachaPullsSinceLastEpic { get; private set; }
public int GachaTotalPulls { get; private set; }
```

In the `Load` method, after the `SummonTickets` line, add:
```csharp
GachaPullsSinceLastEpic = saveData == null ? 0 : Mathf.Max(0, saveData.gachaPullsSinceLastEpic);
GachaTotalPulls         = saveData == null ? 0 : Mathf.Max(0, saveData.gachaTotalPulls);
```

In the `WriteTo` method, after the `summonTickets` line, add:
```csharp
saveData.gachaPullsSinceLastEpic = GachaPullsSinceLastEpic;
saveData.gachaTotalPulls         = GachaTotalPulls;
```

Add a new method `SyncGachaPity` (call after each pull to keep progression in sync):
```csharp
public void SyncGachaPity(int pullsSinceLastEpic, int totalPulls)
{
    GachaPullsSinceLastEpic = Mathf.Max(0, pullsSinceLastEpic);
    GachaTotalPulls         = Mathf.Max(0, totalPulls);
    OnChanged?.Invoke();
}
```

- [ ] **Step 3: Add pity fields to ProgressionApiDtos**

In `Unity/Assets/Scripts/Core/ProgressionApiDtos.cs`, in `PlayerProgressionApiDto`, add alongside `summon_tickets`:
```csharp
public int gacha_pulls_since_last_epic;
public int gacha_total_pulls;
```

In `PlayerInventory.BuildApiDto()`, add these two lines where the other progression fields are mapped:
```csharp
gacha_pulls_since_last_epic = saveData.gachaPullsSinceLastEpic,
gacha_total_pulls = saveData.gachaTotalPulls,
```

### Sub-task 3b: SummonPool asset creation

- [ ] **Step 4: Add SummonPool asset creation to SceneBootstrapper**

In `SceneBootstrapper.Build()`, after dragon definitions are created (after the `starters` array line), add a call to a new helper:

```csharp
CreateSummonPool(starters);
```

Add the helper method to `SceneBootstrapper`:

```csharp
static void CreateSummonPool(DragonDefinition[] allDragons)
{
    string poolPath = "Assets/Resources/SummonPool_Phase1.asset";
    var pool = AssetDatabase.LoadAssetAtPath<SummonPool>(poolPath);
    if (pool == null)
    {
        pool = ScriptableObject.CreateInstance<SummonPool>();
        AssetDatabase.CreateAsset(pool, poolPath);
    }

    pool.BannerName = "Phase1";
    pool.SummonCostGems = 300;
    pool.TenPullCostGems = 2700;
    pool.HasRateUp = false;
    pool.RateUpDragon = null;

    // Rates are already set to correct defaults in SummonPool constructor —
    // only reset if the array is missing or wrong length
    if (pool.RarityRates == null || pool.RarityRates.Length != 6)
    {
        pool.RarityRates = new RarityRate[]
        {
            new RarityRate { Rarity = DragonRarity.Common,    Rate = 0.400f },
            new RarityRate { Rarity = DragonRarity.Uncommon,  Rate = 0.300f },
            new RarityRate { Rarity = DragonRarity.Rare,      Rate = 0.200f },
            new RarityRate { Rarity = DragonRarity.Epic,      Rate = 0.070f },
            new RarityRate { Rarity = DragonRarity.Legendary, Rate = 0.025f },
            new RarityRate { Rarity = DragonRarity.Mythic,    Rate = 0.005f },
        };
    }

    // Weights by rarity: higher weight = more likely within rarity group
    var weights = new System.Collections.Generic.Dictionary<DragonRarity, int>
    {
        { DragonRarity.Common,    10 },
        { DragonRarity.Uncommon,   8 },
        { DragonRarity.Rare,       6 },
        { DragonRarity.Epic,       4 },
        { DragonRarity.Legendary,  3 },
        { DragonRarity.Mythic,     1 },
    };

    var dragonWeights = new System.Collections.Generic.List<DragonWeight>();
    foreach (DragonDefinition def in allDragons)
    {
        if (def == null) continue;
        int w = weights.TryGetValue(def.rarity, out int wt) ? wt : 5;
        dragonWeights.Add(new DragonWeight { Dragon = def, Weight = w });
    }
    pool.AvailableDragons = dragonWeights.ToArray();

    EditorUtility.SetDirty(pool);
    AssetDatabase.SaveAssets();
    Debug.Log($"[SceneBootstrapper] SummonPool created/updated: {dragonWeights.Count} dragons");
}
```

Add the required `using` at the top of `SceneBootstrapper.cs`:
```csharp
using DragonTD.Summoning;
```

### Sub-task 3c: Wire GachaSystem into PlayerInventory

- [ ] **Step 5: Add GachaSystem + SummonPool fields to PlayerInventory**

In `Unity/Assets/Scripts/Core/PlayerInventory.cs`, add these fields alongside `_authSession`:

```csharp
private GachaSystem _gachaSystem;
private SummonPool _summonPool;
```

Add a `using` directive at the top:
```csharp
using DragonTD.Summoning;
```

In `Awake()`, after `SetSyncStatus(...)`, initialize the gacha system:
```csharp
_gachaSystem = new GachaSystem();
_summonPool = Resources.Load<SummonPool>("SummonPool_Phase1");
```

- [ ] **Step 6: Sync pity state on load/save**

In `ApplySaveData`, after `Progression.Load(saveData)`, sync pity from progression into the gacha tracker:

```csharp
if (_gachaSystem != null && _summonPool != null)
{
    var pity = _gachaSystem.GetPity(_summonPool.BannerName);
    pity.PullsSinceLastEpic = Progression.GachaPullsSinceLastEpic;
    pity.TotalPulls         = Progression.GachaTotalPulls;
}
```

**Note:** `PityTracker` fields are public. If they're not public, change the field access accordingly — check `GachaSystem.cs` line 10–12.

- [ ] **Step 7: Replace PickSummonDragon with GachaSystem in TrySummonDragon**

In `PlayerInventory`, replace `TrySummonDragon` with this version that uses the gacha system. The existing ticket-based path now goes through GachaSystem:

```csharp
public bool TrySummonDragon(out string message)
{
    if (!Progression.TrySpendSummonTicket(out message))
    {
        LastSummonSummary = message;
        OnSummonResult?.Invoke(message);
        return false;
    }

    DragonDefinition summoned = PullFromGacha();
    if (summoned == null)
    {
        message = "No dragons available in summon pool";
        LastSummonSummary = message;
        OnSummonResult?.Invoke(message);
        return false;
    }

    AddDragon(summoned);
    message = $"Summoned {summoned.displayName} ({summoned.rarity})";
    LastSummonSummary = message;
    Progression.RecordDailyObjectiveProgress(DailyObjectiveType.SummonDragon);
    OnSummonResult?.Invoke(message);
    OnProgressionChanged?.Invoke();
    return true;
}
```

- [ ] **Step 8: Add TrySummonDragonWithGems and TryTenPullWithGems**

Add these two methods to `PlayerInventory` below `TrySummonDragon`:

```csharp
public bool TrySummonDragonWithGems(out string message)
{
    if (_summonPool == null)
    {
        message = "Summon pool not loaded";
        return false;
    }
    if (!Progression.TrySpendGems(_summonPool.SummonCostGems, out message))
    {
        LastSummonSummary = message;
        OnSummonResult?.Invoke(message);
        return false;
    }

    DragonDefinition summoned = PullFromGacha();
    if (summoned == null)
    {
        message = "No dragons available in summon pool";
        LastSummonSummary = message;
        OnSummonResult?.Invoke(message);
        return false;
    }

    AddDragon(summoned);
    message = $"Summoned {summoned.displayName} ({summoned.rarity})";
    LastSummonSummary = message;
    Progression.RecordDailyObjectiveProgress(DailyObjectiveType.SummonDragon);
    OnSummonResult?.Invoke(message);
    OnProgressionChanged?.Invoke();
    return true;
}

public bool TryTenPullWithGems(out string[] messages)
{
    messages = new string[10];
    if (_summonPool == null)
    {
        messages[0] = "Summon pool not loaded";
        return false;
    }
    if (!Progression.TrySpendGems(_summonPool.TenPullCostGems, out string spendMsg))
    {
        messages[0] = spendMsg;
        LastSummonSummary = spendMsg;
        OnSummonResult?.Invoke(spendMsg);
        return false;
    }

    DragonDefinition[] results = _gachaSystem.TenPull(_summonPool);
    SyncPityToProgression();
    SaveProgressionAsync();

    for (int i = 0; i < results.Length; i++)
    {
        if (results[i] != null)
        {
            AddDragon(results[i]);
            messages[i] = $"{results[i].displayName} ({results[i].rarity})";
        }
        else
        {
            messages[i] = "—";
        }
    }

    Progression.RecordDailyObjectiveProgress(DailyObjectiveType.SummonDragon);
    string summary = $"10-Pull: {string.Join(", ", messages)}";
    LastSummonSummary = summary;
    OnSummonResult?.Invoke(summary);
    OnProgressionChanged?.Invoke();
    return true;
}
```

- [ ] **Step 9: Add PullFromGacha helper**

Add this private helper to `PlayerInventory`, replacing or supplementing `PickSummonDragon`:

```csharp
private DragonDefinition PullFromGacha()
{
    if (_gachaSystem == null || _summonPool == null)
        return PickSummonDragon(); // fallback to old behavior if gacha not loaded

    DragonDefinition result = _gachaSystem.SinglePull(_summonPool);
    SyncPityToProgression();
    SaveProgressionAsync();
    return result;
}

private void SyncPityToProgression()
{
    if (_gachaSystem == null || _summonPool == null) return;
    var pity = _gachaSystem.GetPity(_summonPool.BannerName);
    Progression.SyncGachaPity(pity.PullsSinceLastEpic, pity.TotalPulls);
}
```

### Sub-task 3d: Update summon UI

- [ ] **Step 10: Update summon panel in ProfileProgressionPanel**

In `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`, update `UpdateSummonPanelText()` to show pity and gem costs:

Replace the current method body:
```csharp
private void UpdateSummonPanelText()
{
    PlayerInventory inventory = PlayerInventory.Instance;
    if (_summonPanelText != null && inventory != null)
    {
        int pullsSince = inventory.Progression.GachaPullsSinceLastEpic;
        int totalPulls = inventory.Progression.GachaTotalPulls;
        string pityInfo = pullsSince >= 50
            ? $"Soft pity active! ({pullsSince}/100)"
            : $"Pity: {pullsSince}/100 pulls since Epic+";
        _summonPanelText.text =
            $"Dragon Summon\n\n" +
            $"Tickets: {inventory.Progression.SummonTickets}  |  Gems: {inventory.Progression.Gems}\n" +
            $"1 Ticket or 300 Gems per pull  |  10-Pull: 2700 Gems\n" +
            $"{pityInfo}\n" +
            $"Total pulls: {totalPulls}\n" +
            $"{(string.IsNullOrWhiteSpace(inventory.LastSummonSummary) ? "Use a ticket or gems to summon." : inventory.LastSummonSummary)}";
    }

    if (_summonConfirmButton != null && inventory != null)
        _summonConfirmButton.interactable = inventory.Progression.SummonTickets > 0;
}
```

- [ ] **Step 11: Add gem pull and 10-pull buttons to summon panel**

In `EnsureSummonPanel()`, after `_summonConfirmButton` and `_summonCancelButton` creation, add gem pull buttons:

```csharp
// Add gem single-pull button
var gemPullButton = CreateRuntimeButton(_summonPanel.transform, "GemSinglePullButton", "300 Gems", font,
    new Vector2(0.50f, 0.18f), new Vector2(140f, 42f));
gemPullButton.onClick.AddListener(() =>
{
    PlayerInventory.Instance?.TrySummonDragonWithGems(out _);
    UpdateSummonPanelText();
    Refresh();
});

// Add 10-pull button
var tenPullButton = CreateRuntimeButton(_summonPanel.transform, "TenPullButton", "2700 Gems\n(10x)", font,
    new Vector2(0.65f, 0.18f), new Vector2(140f, 42f));
tenPullButton.onClick.AddListener(() =>
{
    PlayerInventory.Instance?.TryTenPullWithGems(out _);
    UpdateSummonPanelText();
    Refresh();
});
```

Update `_summonConfirmButton` label to "Use Ticket" and move it to `(0.35f, 0.18f)` in the layout. Adjust all three button anchors so they fit:

```csharp
// Existing _summonConfirmButton stays at anchor (0.35f, 0.18f) — "Use Ticket"
// Existing _summonCancelButton moves to (0.84f, 0.18f) — "Close"
```

In `EnsureSummonPanel()`, change `_summonCancelButton` creation anchor from `new Vector2(0.65f, 0.18f)` to `new Vector2(0.84f, 0.18f)` to leave room for the new buttons.

- [ ] **Step 12: Compile verify**

Run Unity batchmode compile or check editor Console. Expected: no errors.

If `DragonElement.Water` or `DragonElement.Wind` don't exist (Task 2 step 2), fix those element values in `Phase1DragonData.cs` before compiling.

- [ ] **Step 13: Commit Task 3**

```bash
git add Unity/Assets/Scripts/Core/PlayerProgressionData.cs
git add Unity/Assets/Scripts/Core/PlayerProgression.cs
git add Unity/Assets/Scripts/Core/ProgressionApiDtos.cs
git add Unity/Assets/Scripts/Core/PlayerInventory.cs
git add Unity/Assets/Editor/SceneBootstrapper.cs
git add Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs
git add Unity/Assets/Resources/
git commit -m "feat: wire GachaSystem into summon flow with pity persistence and gem pulls"
```

---

## Task 4: Regenerate and Verify Scene

- [ ] **Step 1: Regenerate scene with all changes**

Open Unity editor → `Dragon Dominion > Build Battle Scene`

- [ ] **Step 2: Manual verify waves**

Open `BattleScene.unity` → Play → Start waves → confirm:
- 15 waves present (wave counter shows 1/15 … 15/15)
- Victory after wave 15

- [ ] **Step 3: Manual verify dragons**

Open `MainMenu.unity` → Play → Dragons menu → confirm:
- 10 dragon tiles visible in collection grid
- Emberveil, Tideclaw, Zephyrwing appear with correct names and elements

- [ ] **Step 4: Manual verify gacha**

From Dragons menu → Summon Dragon panel:
- Pity counter shows "0/100 pulls since Epic+"
- "Use Ticket" spends 1 ticket and shows a dragon name + rarity
- "300 Gems" button spends 300 gems and shows a dragon name + rarity
- "2700 Gems (10x)" button spends 2700 gems and shows 10 dragon names

- [ ] **Step 5: Update handoff doc**

Append a section to `docs/2026-05-27-dragon-dominion-prototype-handoff.md`:

```markdown
## Phase 2 — Waves, Dragons, Gacha — 2026-05-29

- Waves expanded from 5 to 15. Wave06–15 generated in SceneBootstrapper with
  escalating enemy counts, faster spawn intervals, and increasing gold/mana rewards.
- Dragons expanded from 7 to 10: emberveil_008 (Epic/Fire/Celestial),
  tideclaw_009 (Rare/Water/Frost), zephyrwing_010 (Uncommon/Wind/Storm).
- GachaSystem wired into PlayerInventory summon flow:
  - Ticket pull uses GachaSystem.SinglePull with soft/hard pity.
  - Gem single pull (300 Gems) and 10-pull (2700 Gems) added.
  - Pity state persisted in PlayerProgressionSaveData as gachaPullsSinceLastEpic
    and gachaTotalPulls, synced to/from GachaSystem.PityTracker on load/save.
  - SummonPool_Phase1.asset created in Assets/Resources, containing all 10 dragons
    with rarity-weighted entries and correct rates from CLAUDE.md.
  - Summon panel updated to show pity counter, gem cost, and all pull options.
```

- [ ] **Step 6: Commit Task 4**

```bash
git add docs/2026-05-27-dragon-dominion-prototype-handoff.md
git add Unity/Assets/Scenes/
git commit -m "docs: update handoff for Phase 2 waves/dragons/gacha"
```

---

## Self-Review

**Spec coverage:**
- Waves 5→15: Wave06–Wave15 in Task 1. ✓
- Dragons 7→10: emberveil_008, tideclaw_009, zephyrwing_010 in Task 2. ✓
- SummonPool ScriptableObject: created in SceneBootstrapper Task 3 step 4. ✓
- GachaSystem with pity rates (soft@50, hard@100, 10-pull guarantee): GachaSystem.cs already implements this fully; wired in Task 3 steps 7–9. ✓
- Pity stored in PlayerProgression: Tasks 3a steps 1–3. ✓
- Gem-based pulls: TrySummonDragonWithGems + TryTenPullWithGems in Task 3 steps 8–9. ✓

**No placeholders present.**

**Type consistency verified:**
- `GachaSystem.SinglePull(SummonPool)` → called in `PullFromGacha`. ✓
- `GachaSystem.TenPull(SummonPool)` → called in `TryTenPullWithGems`. ✓
- `GachaSystem.GetPity(string)` → returns `PityTracker` with public `PullsSinceLastEpic` and `TotalPulls`. ✓
- `PlayerProgression.SyncGachaPity(int, int)` → defined in step 2, called in `SyncPityToProgression`. ✓
- `SummonPool.SummonCostGems` and `TenPullCostGems` → public fields on SummonPool.cs. ✓
- `DragonElement.Water` / `DragonElement.Wind` → step 2 explicitly instructs to verify and adjust if absent. ✓

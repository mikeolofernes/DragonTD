# LaneDefense Map Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Plants-vs-Zombies-style LaneDefense map mode where enemies walk straight right and damage a wall, alongside the existing PathFollowing mode.

**Architecture:** Additive — new `MapType` enum gates behaviour in existing systems. `WallBase` MonoBehaviour handles HP and fires a defeat event. `EnemyBase` detects LaneDefense mode and walks straight right instead of following waypoints. `WaveManager` spawns enemies at random rows on the left edge instead of a single spawn point.

**Tech Stack:** Unity 6 C#, MonoBehaviour, existing GridManager/GameManager/BattleHUD patterns.

---

## File Structure

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

---

## Task 1: MapType Enum + MapDefinition Extensions

**Files:**
- Create: `Unity/Assets/Scripts/Core/MapType.cs`
- Modify: `Unity/Assets/Scripts/TowerDefense/MapDefinition.cs`

- [ ] **Step 1: Create MapType enum**

Create `Unity/Assets/Scripts/Core/MapType.cs`:

```csharp
namespace DragonTD.Core
{
    public enum MapType
    {
        PathFollowing,  // existing: enemies follow waypoints
        LaneDefense     // new: enemies walk straight right, wall has HP
    }
}
```

- [ ] **Step 2: Add LaneDefense fields to MapDefinition**

In `Unity/Assets/Scripts/TowerDefense/MapDefinition.cs`, add after `[Header("Background (optional...)")]` and before the `[Header("Grid")]` block:

```csharp
        [Header("Lane Defense Settings (LaneDefense mode only)")]
        public DragonTD.Core.MapType mapType = DragonTD.Core.MapType.PathFollowing;
        public int wallColumn = 8;    // column index where the wall sits
        public float wallHp   = 1000f;
```

- [ ] **Step 3: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/Core/MapType.cs Unity/Assets/Scripts/TowerDefense/MapDefinition.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: MapType enum (PathFollowing/LaneDefense) and wall HP fields on MapDefinition"
```

---

## Task 2: WallBase MonoBehaviour

**Files:**
- Create: `Unity/Assets/Scripts/TowerDefense/WallBase.cs`

- [ ] **Step 1: Create WallBase.cs**

Create `Unity/Assets/Scripts/TowerDefense/WallBase.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace DragonTD.TowerDefense
{
    // Placed at the wall column in LaneDefense maps. Enemies deal damage here.
    // Fires OnWallDestroyed when HP reaches 0 — GameManager subscribes to trigger defeat.
    public class WallBase : MonoBehaviour
    {
        public static WallBase Instance { get; private set; }

        [SerializeField] private float _maxHp = 1000f;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public float MaxHp      => _maxHp;
        public float CurrentHp  { get; private set; }
        public float HpPercent  => MaxHp > 0f ? CurrentHp / MaxHp : 0f;
        public bool  IsDestroyed { get; private set; }

        public event System.Action OnWallDestroyed;
        public event System.Action<float> OnHpChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            CurrentHp = _maxHp;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(float maxHp)
        {
            _maxHp    = maxHp;
            CurrentHp = maxHp;
            IsDestroyed = false;
            UpdateVisual();
        }

        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - damage);
            OnHpChanged?.Invoke(HpPercent);
            UpdateVisual();
            if (CurrentHp <= 0f)
            {
                IsDestroyed = true;
                OnWallDestroyed?.Invoke();
            }
        }

        private void UpdateVisual()
        {
            if (_spriteRenderer == null) return;
            // Tint wall from orange (full HP) to red (low HP)
            float t = HpPercent;
            _spriteRenderer.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f, 1f),
                                                new Color(0.9f, 0.55f, 0.1f, 1f), t);
        }

        // Runtime factory — called by GameManager when entering a LaneDefense scene
        public static WallBase Create(float wallWorldX, float gridHeight, float cellSize, float wallHp)
        {
            var go  = new GameObject("WallBase");
            // Wall spans the full grid height, positioned at the wall column world X
            go.transform.position = new Vector3(wallWorldX, (gridHeight * cellSize) * 0.5f - cellSize * 0.5f, 0f);

            // Visual: white sprite stretched to cover one column × full grid height
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;

            var tex = new Texture2D(4, 4);
            for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            go.transform.localScale = new Vector3(1f, gridHeight, 1f);

            var wall = go.AddComponent<WallBase>();
            wall._spriteRenderer = sr;
            wall.Configure(wallHp);
            return wall;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/WallBase.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: WallBase MonoBehaviour with HP, damage, destroy event, runtime factory"
```

---

## Task 3: EnemyBase LaneDefense Movement

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs`

Context: `EnemyBase.MoveTowardsWaypoint()` is `protected virtual` and drives movement. `ReachBase()` is called when the enemy reaches the end. In LaneDefense mode enemies walk right until they hit the wall column world X.

- [ ] **Step 1: Add lane mode detection fields to EnemyBase**

In `EnemyBase.cs`, after `private bool _reachedBase;` add:

```csharp
        private bool  _isLaneMode;
        private float _wallWorldX;
```

- [ ] **Step 2: Set lane mode in Initialize**

`Initialize(Transform[] waypoints)` is the existing init method. Add an overload for LaneDefense:

```csharp
        // Call this overload in LaneDefense maps instead of Initialize(Transform[])
        public void InitializeLane(float wallWorldX)
        {
            _isLaneMode = true;
            _wallWorldX = wallWorldX;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            if (_healthBar != null) _healthBar.Initialize(this);
            _waypoints = new Transform[0];
            _maxHp = _data.MaxHp;
            _moveSpeed = _data.MoveSpeed;
            _currentHp = _maxHp;
            _waypointIndex = 0;
            _shieldCracked = Trait != EnemyTrait.Shielded;
            _baseScale = transform.localScale;
            ApplyTraitVisuals();
            EnsureTraitLabel();
            OnHpChanged?.Invoke(HpPercent);
        }
```

- [ ] **Step 3: Override MoveTowardsWaypoint for lane mode**

Find `protected virtual void MoveTowardsWaypoint()`. Replace with:

```csharp
        protected virtual void MoveTowardsWaypoint()
        {
            if (_isLaneMode)
            {
                // Lane mode: walk straight right until we hit the wall
                transform.position += Vector3.right * CurrentMoveSpeed * Time.deltaTime;
                if (transform.position.x >= _wallWorldX)
                    HitWall();
                return;
            }

            if (_waypoints == null || _waypointIndex >= _waypoints.Length) return;

            Transform target = _waypoints[_waypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target.position, CurrentMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                _waypointIndex++;
                if (_waypointIndex >= _waypoints.Length)
                    ReachBase();
            }
        }
```

- [ ] **Step 4: Add HitWall method**

Add after `ReachBase()`:

```csharp
        private void HitWall()
        {
            if (_reachedBase) return;
            _reachedBase = true;
            if (WallBase.Instance != null)
                WallBase.Instance.TakeDamage(_data.DamageToBase * 50f); // 50 HP per DamageToBase unit
            BattleStatsTracker.Instance?.RecordEnemyLeaked();
            Die();
        }
```

- [ ] **Step 5: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: EnemyBase lane mode - walk right, hit WallBase on reach"
```

---

## Task 4: WaveManager Lane Spawn

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`

Context: `SpawnWave` uses `_spawnPoints[0].position` and calls `enemy.Initialize(_waypoints)`. In LaneDefense mode we spawn at random rows and call `enemy.InitializeLane(wallWorldX)`.

- [ ] **Step 1: Add lane mode fields to WaveManager**

Add after `private bool _isWaveActive;`:

```csharp
        private bool  _isLaneMode;
        private float _laneWallWorldX;
        private float _laneLeftEdgeX;
        private int   _laneGridHeight;
        private float _laneCellSize;

        // Called by GameManager when entering LaneDefense scene
        public void ConfigureLaneMode(float wallWorldX, float leftEdgeX, int gridHeight, float cellSize)
        {
            _isLaneMode      = true;
            _laneWallWorldX  = wallWorldX;
            _laneLeftEdgeX   = leftEdgeX;
            _laneGridHeight  = gridHeight;
            _laneCellSize    = cellSize;
        }
```

- [ ] **Step 2: Modify SpawnWave to use lane spawning**

In `SpawnWave`, find the enemy instantiation block:

```csharp
                        GameObject enemyGO = Instantiate(group.EnemyPrefab, _spawnPoints[0].position, Quaternion.identity);
                        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
                        if (enemy != null)
                        {
                            enemy.Initialize(_waypoints);
                            enemy.ApplyDifficultyMultiplier(GameManager.Instance?.StageDifficultyMultiplier ?? 1f);
                            GameDirector.Instance?.OnEnemySpawned(enemy);
                        }
```

Replace with:

```csharp
                        Vector3 spawnPos;
                        if (_isLaneMode)
                        {
                            int row = UnityEngine.Random.Range(0, _laneGridHeight);
                            float worldY = _laneLeftEdgeX + row * _laneCellSize; // reuse leftEdgeX as originY
                            spawnPos = new Vector3(_laneLeftEdgeX - 1f, worldY, 0f);
                        }
                        else
                        {
                            spawnPos = _spawnPoints[0].position;
                        }

                        GameObject enemyGO = Instantiate(group.EnemyPrefab, spawnPos, Quaternion.identity);
                        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
                        if (enemy != null)
                        {
                            if (_isLaneMode)
                                enemy.InitializeLane(_laneWallWorldX);
                            else
                                enemy.Initialize(_waypoints);
                            enemy.ApplyDifficultyMultiplier(GameManager.Instance?.StageDifficultyMultiplier ?? 1f);
                            GameDirector.Instance?.OnEnemySpawned(enemy);
                        }
```

**Note:** `_laneLeftEdgeX` is reused for the origin Y base — specifically `originY = -3.5f` (the grid origin Y from SceneBootstrapper). Pass the correct Y origin when calling `ConfigureLaneMode`. Update the call site in Task 5.

- [ ] **Step 3: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: WaveManager lane spawn mode — random row on left edge, InitializeLane"
```

---

## Task 5: GameManager LaneDefense Wiring

**Files:**
- Modify: `Unity/Assets/Scripts/Core/GameManager.cs`

Context: `LoseLife` is the current defeat path. `StartBattle` resets the battle. In LaneDefense mode the wall handles defeat via `OnWallDestroyed`.

- [ ] **Step 1: Add lane mode fields + setup to GameManager**

After `private GameState _stateBeforePause;` add:

```csharp
        private bool _isLaneMode;

        // Called by SceneBootstrapper/ChapterContent wiring when scene loads with a LaneDefense map
        public void SetupLaneDefense(MapDefinition map)
        {
            _isLaneMode = true;
            // Grid constants matching SceneBootstrapper: 12 cols, 8 rows, origin (-5.5, -3.5)
            const float originX = -5.5f;
            const float originY = -3.5f;
            const int   rows    = 8;
            float wallWorldX    = originX + map.wallColumn;
            float leftEdgeX     = originX - 1f;   // one unit left of col 0

            WallBase wall = WallBase.Create(wallWorldX, rows, 1f, map.wallHp);
            wall.OnWallDestroyed += HandleWallDestroyed;

            if (WaveManager.Instance != null)
                WaveManager.Instance.ConfigureLaneMode(wallWorldX, originY, rows, 1f);
        }

        private void HandleWallDestroyed()
        {
            if (State == GameState.Defeat) return;
            GrantBattleRewards(false);
            SetState(GameState.Defeat);
            ShowBattleMessage("The wall has fallen!");
        }
```

- [ ] **Step 2: Call SetupLaneDefense from StartBattle**

In `StartBattle()`, after `SetState(GameState.Planning);`, add:

```csharp
            // LaneDefense setup — runs if the active chapter has a LaneDefense map
            var activeMap = ChapterContent.Active?.map;
            if (activeMap != null && activeMap.mapType == DragonTD.Core.MapType.LaneDefense)
                SetupLaneDefense(activeMap);
            else
                _isLaneMode = false;
```

- [ ] **Step 3: Hide Lives in LaneDefense mode**

`LoseLife` should no-op in LaneDefense mode (wall handles defeat):

Find `public void LoseLife(int amount = 1)` and wrap its body:

```csharp
        public void LoseLife(int amount = 1)
        {
            if (_isLaneMode) return; // wall handles defeat in LaneDefense
            Lives -= amount;
            if (Lives <= 0) Lives = 0;
            OnLivesChanged?.Invoke(Lives);
            if (Lives <= 0 && State != GameState.Defeat)
            {
                GrantBattleRewards(false);
                SetState(GameState.Defeat);
                ShowBattleMessage("Defeat - the base fell");
            }
            else if (Lives <= 5)
            {
```

**Note:** Find the full existing `LoseLife` body in `GameManager.cs`. Add `if (_isLaneMode) return;` as the very first line inside the method.

- [ ] **Step 4: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/Core/GameManager.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: GameManager LaneDefense wiring — wall setup, wall-destroyed defeat, LoseLife no-op"
```

---

## Task 6: BattleHUD Wall HP Display

**Files:**
- Modify: `Unity/Assets/Scripts/UI/BattleHUD.cs`

Context: `BattleHUD` has `[SerializeField] private Text _livesText;` updated via `UpdateLives()`. In LaneDefense mode, hide lives and show wall HP instead.

- [ ] **Step 1: Add wall HP text field and subscribe**

In `BattleHUD.cs`, after `[SerializeField] private Text _livesText;` add:

```csharp
        private Text _wallHpText;
```

In `OnEnable()`, after existing subscriptions (e.g. after `GameManager.Instance.OnLivesChanged += UpdateLives;`), add:

```csharp
            if (WallBase.Instance != null)
                WallBase.Instance.OnHpChanged += UpdateWallHp;
```

In `OnDisable()`, add:

```csharp
            if (WallBase.Instance != null)
                WallBase.Instance.OnHpChanged -= UpdateWallHp;
```

- [ ] **Step 2: Add UpdateWallHp and EnsureWallHpText**

Add these methods:

```csharp
        private void EnsureWallHpText()
        {
            if (_wallHpText != null) return;
            if (_livesText == null) return;
            var go = new GameObject("WallHpText");
            go.transform.SetParent(_livesText.transform.parent, false);
            var rt = go.AddComponent<RectTransform>();
            RectTransform src = _livesText.GetComponent<RectTransform>();
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.anchoredPosition = src.anchoredPosition;
            rt.sizeDelta = src.sizeDelta;
            _wallHpText = go.AddComponent<Text>();
            _wallHpText.font = _livesText.font;
            _wallHpText.fontSize = _livesText.fontSize;
            _wallHpText.color = new Color(1f, 0.55f, 0.1f, 1f); // orange
            _wallHpText.alignment = _livesText.alignment;
        }

        private void UpdateWallHp(float hpPercent)
        {
            EnsureWallHpText();
            if (_wallHpText == null || WallBase.Instance == null) return;
            int cur = Mathf.CeilToInt(WallBase.Instance.CurrentHp);
            int max = Mathf.CeilToInt(WallBase.Instance.MaxHp);
            _wallHpText.text = $"Wall: {cur}/{max}";
            _wallHpText.gameObject.SetActive(true);
            if (_livesText != null) _livesText.gameObject.SetActive(false);
        }
```

- [ ] **Step 3: Reset on non-LaneDefense scenes**

In the existing `Refresh()` or `UpdateLives()` method, ensure lives text is shown and wall text hidden when `WallBase.Instance == null`:

Find `UpdateLives()`. Add at the top:

```csharp
        private void UpdateLives()
        {
            bool laneMode = WallBase.Instance != null;
            if (_livesText != null) _livesText.gameObject.SetActive(!laneMode);
            if (_wallHpText != null) _wallHpText.gameObject.SetActive(laneMode);
            if (!laneMode && _livesText != null)
                _livesText.text = $"Lives: {GameManager.Instance?.Lives ?? 0}";
        }
```

- [ ] **Step 4: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/UI/BattleHUD.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: BattleHUD shows wall HP bar in LaneDefense mode, hides lives"
```

---

## Task 7: SceneBootstrapper Chapter 4 LaneDefense Scaffold

**Files:**
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`

- [ ] **Step 1: Add EnsureChapter4LaneMap method**

Add after `EnsureChapter3Map()`:

```csharp
        static MapDefinition EnsureChapter4LaneMap()
        {
            string path = MapSODir + "/Chapter4LaneMap.asset";
            var def = AssetDatabase.LoadAssetAtPath<MapDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<MapDefinition>();
                def.mapName = "Chapter 4 — Lane Defense";
                // 12 cols, 8 rows. Wall at col 8. B tiles across combat + base zones.
                // Row 0 = top. Cols 0-7 = combat zone. Col 8 = wall. Cols 9-11 = base zone.
                def.grid =
                    "BBBBBBBBBWBBB\n" +   // W marks conceptual wall column (treated as B for placement, wall is a GO)
                    "BBBBBBBBBBBBB\n" +
                    "BBBBBBBBBBBBB\n" +
                    "BBBBBBBBBBBBB\n" +
                    "BBBBBBBBBBBBB\n" +
                    "BBBBBBBBBBBBB\n" +
                    "BBBBBBBBBBBBB\n" +
                    "BBBBBBBBBBBBB";
                def.mapType    = DragonTD.Core.MapType.LaneDefense;
                def.wallColumn = 8;
                def.wallHp     = 1000f;
                AssetDatabase.CreateAsset(def, path);
                AssetDatabase.SaveAssets();
            }
            return def;
        }
```

**Note:** The grid uses all 'B' tiles — in LaneDefense mode, all non-wall columns are buildable. The wall itself is created at runtime by `GameManager.SetupLaneDefense`. The 13-char row is a marker reminder — fix to 12 chars matching `MapDefinition.Cols`:

```
def.grid =
    "BBBBBBBBBBBB\n" +
    "BBBBBBBBBBBB\n" +
    "BBBBBBBBBBBB\n" +
    "BBBBBBBBBBBB\n" +
    "BBBBBBBBBBBB\n" +
    "BBBBBBBBBBBB\n" +
    "BBBBBBBBBBBB\n" +
    "BBBBBBBBBBBB";
```

- [ ] **Step 2: Add Chapter 4 stages to StageCatalog**

In `Unity/Assets/Scripts/Core/StageCatalog.cs`, after the 4 Chapter 3 stages (add trailing comma to ch3 stage 4), add:

```csharp
            new StageDefinition("chapter_4_stage_1", "4-1", "Ashwall", "Lane rush — light", 2.0f, 1.8f, "Epic/Legendary", 9, 2, 1.0f, 20, 2, 8, 4, DragonRoleTag.Damage),
            new StageDefinition("chapter_4_stage_2", "4-2", "Ironhold", "Lane rush — heavy", 2.4f, 2.0f, "Epic/Legendary", 10, 3, 1.1f, 20, 1, 8, 4, DragonRoleTag.AntiShield, DragonRoleTag.Damage),
            new StageDefinition("chapter_4_stage_3", "4-3", "Wallbreak", "Mixed lane assault", 2.8f, 2.2f, "Legendary", 11, 3, 1.2f, 20, 0, 8, 4, DragonRoleTag.Aoe, DragonRoleTag.Damage),
            new StageDefinition("chapter_4_stage_4", "4-4", "Last Stand", "Full lane siege", 3.2f, 2.5f, "Legendary/Mythic", 12, 4, 1.3f, 20, 0, 8, 4, DragonRoleTag.Damage, DragonRoleTag.Slow, DragonRoleTag.Aoe)
```

- [ ] **Step 3: Generate Chapter4 content + wire into ChapterContents**

In `EnsureChapterContents`, add `ch4Map` parameter and entry:

```csharp
        static void EnsureChapterContents(MapDefinition ch1Map, MapDefinition ch2Map, MapDefinition ch3Map, MapDefinition ch4Map)
        {
            CreateChapterContent("Chapter1Content", 1, ch1Map, "Wave", 15);
            CreateChapterContent("Chapter2Content", 2, ch2Map, "Chapter2_Wave", 15);
            CreateChapterContent("Chapter3Content", 3, ch3Map, "Chapter3_Wave", 15);
            CreateChapterContent("Chapter4Content", 4, ch4Map, "Chapter4_Wave", 15);
        }
```

In `Build()`, add:
```csharp
            var ch4Map = EnsureChapter4LaneMap();
```

And update the call: `EnsureChapterContents(mapDef, ch2Map, ch3Map, ch4Map);`

Also generate 15 Chapter4 waves (use existing enemy prefabs, Chapter 3 enemy types work fine for LaneDefense):

```csharp
            CreateWave("Chapter4_Wave01", 180, 120, new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 6, SpawnInterval = 1.2f });
            CreateWave("Chapter4_Wave02", 220, 145, new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 8, SpawnInterval = 0.8f });
            CreateWave("Chapter4_Wave03", 270, 170,
                new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 6, SpawnInterval = 1.0f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 3, SpawnInterval = 0.9f });
            CreateWave("Chapter4_Wave04", 330, 200,
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 10, SpawnInterval = 0.6f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 2, SpawnInterval = 1.0f });
            CreateWave("Chapter4_Wave05", 400, 230,
                new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 8, SpawnInterval = 0.9f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 4, SpawnInterval = 0.8f });
            CreateWave("Chapter4_Wave06", 480, 265,
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 12, SpawnInterval = 0.5f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 4, SpawnInterval = 0.8f });
            CreateWave("Chapter4_Wave07", 570, 300,
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 5, SpawnInterval = 0.7f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 5, SpawnInterval = 0.7f });
            CreateWave("Chapter4_Wave08", 670, 340,
                new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 10, SpawnInterval = 0.7f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 5, SpawnInterval = 0.6f },
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 8, SpawnInterval = 0.5f });
            CreateWave("Chapter4_Wave09", 780, 385,
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 6, SpawnInterval = 0.6f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 6, SpawnInterval = 0.7f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 6, SpawnInterval = 0.5f });
            CreateWave("Chapter4_Wave10", 900, 440,
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 16, SpawnInterval = 0.38f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 8, SpawnInterval = 0.55f });
            CreateWave("Chapter4_Wave11", 1040, 500,
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 8, SpawnInterval = 0.5f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 8, SpawnInterval = 0.45f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 8, SpawnInterval = 0.5f });
            CreateWave("Chapter4_Wave12", 1200, 570,
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 10, SpawnInterval = 0.45f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 8, SpawnInterval = 0.42f },
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 14, SpawnInterval = 0.32f });
            CreateWave("Chapter4_Wave13", 1400, 650,
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 12, SpawnInterval = 0.38f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 10, SpawnInterval = 0.35f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 10, SpawnInterval = 0.38f });
            CreateWave("Chapter4_Wave14", 1650, 750,
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 20, SpawnInterval = 0.28f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 12, SpawnInterval = 0.35f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 12, SpawnInterval = 0.32f });
            CreateWave("Chapter4_Wave15", 2000, 880,
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 14, SpawnInterval = 0.28f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 14, SpawnInterval = 0.30f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 16, SpawnInterval = 0.25f },
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 22, SpawnInterval = 0.20f });
```

Also update `CreateManagerRoot` to wire 4 chapters into `GameManager._chapters` (find the existing block that loads ch1/ch2/ch3 and add ch4).

- [ ] **Step 4: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Editor/SceneBootstrapper.cs Unity/Assets/Scripts/Core/StageCatalog.cs
git -C "D:/DragonTD/DragonTD" commit -m "feat: Chapter 4 LaneDefense map scaffold — map asset, 15 waves, 4 stages, ChapterContent"
git -C "D:/DragonTD/DragonTD" fetch . main:claude/dragon-tower-defense-rpg-okxJw
```

---

## Task 8: Fix WaveManager lane spawn Y coordinate

**Context:** In Task 4, `_laneLeftEdgeX` was incorrectly reused for the Y origin. This task corrects the spawn position calculation.

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`
- Modify: `Unity/Assets/Scripts/Core/GameManager.cs` (update SetupLaneDefense call)

- [ ] **Step 1: Add separate Y origin field to WaveManager**

In `WaveManager.cs`, change `ConfigureLaneMode` to accept a Y origin separately:

```csharp
        private float _laneOriginY;

        public void ConfigureLaneMode(float wallWorldX, float leftEdgeX, float originY, int gridHeight, float cellSize)
        {
            _isLaneMode      = true;
            _laneWallWorldX  = wallWorldX;
            _laneLeftEdgeX   = leftEdgeX;
            _laneOriginY     = originY;
            _laneGridHeight  = gridHeight;
            _laneCellSize    = cellSize;
        }
```

- [ ] **Step 2: Fix spawn position in SpawnWave**

Update the lane spawn position:

```csharp
                        if (_isLaneMode)
                        {
                            int row = UnityEngine.Random.Range(0, _laneGridHeight);
                            float worldY = _laneOriginY + row * _laneCellSize;
                            spawnPos = new Vector3(_laneLeftEdgeX, worldY, 0f);
                        }
```

- [ ] **Step 3: Update GameManager.SetupLaneDefense call**

In `GameManager.SetupLaneDefense`, update the `ConfigureLaneMode` call:

```csharp
            if (WaveManager.Instance != null)
                WaveManager.Instance.ConfigureLaneMode(wallWorldX, leftEdgeX, originY, rows, 1f);
```

Where `originY = -3.5f` (grid origin Y, matching SceneBootstrapper's `new Vector3(-5.5f, -3.5f, 0f)`).

- [ ] **Step 4: Commit**

```bash
git -C "D:/DragonTD/DragonTD" add Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs Unity/Assets/Scripts/Core/GameManager.cs
git -C "D:/DragonTD/DragonTD" commit -m "fix: WaveManager lane spawn uses correct Y origin from grid"
git -C "D:/DragonTD/DragonTD" fetch . main:claude/dragon-tower-defense-rpg-okxJw
```

---

## Self-Review

**Spec coverage:**
- MapType enum ✓ Task 1
- MapDefinition wallColumn/wallHp ✓ Task 1
- WallBase HP/damage/defeat event ✓ Task 2
- WallBase.Create runtime factory ✓ Task 2
- EnemyBase lane movement ✓ Task 3
- EnemyBase.InitializeLane ✓ Task 3
- HitWall → WallBase.TakeDamage ✓ Task 3
- WaveManager lane spawn (random row) ✓ Tasks 4+8
- GameManager SetupLaneDefense ✓ Task 5
- GameManager LoseLife no-op in lane mode ✓ Task 5
- BattleHUD wall HP display ✓ Task 6
- Chapter 4 scaffold ✓ Task 7

**No placeholders.**

**Type consistency:**
- `WallBase.Instance` defined Task 2, used Tasks 3, 5, 6. ✓
- `ConfigureLaneMode(wallWorldX, leftEdgeX, originY, gridHeight, cellSize)` 5-param signature defined Task 8, matches call in Task 5. ✓ (Task 4 uses 4-param — Task 8 corrects this.)
- `MapType.LaneDefense` defined Task 1, used Tasks 5, 7. ✓
- `InitializeLane(float wallWorldX)` defined Task 3, called in Task 4. ✓

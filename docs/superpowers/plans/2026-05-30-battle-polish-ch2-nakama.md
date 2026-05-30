# Battle Polish, Chapter 2, Pity Sync, Nakama — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the 10-pull result screen, verify existing tower visuals, persist gacha pity to backend, add Nakama device auth + leaderboard submission, and build a runtime-loadable Chapter 2 ice-tundra (map + 3 enemies + 15 waves + stages).

**Architecture:** Unity client features are additive. Chapter 2 introduces runtime chapter loading: a `ChapterContent` ScriptableObject bundles a `MapDefinition` + `WaveData[]`, and `GameManager` selects the active chapter from the chosen stage before `GridManager`/`WaveManager` build. Backend pity is already a JSON-blob round-trip — covered by a test. Nakama is isolated behind two service classes.

**Tech Stack:** Unity 6 C#, ScriptableObjects, .NET 9 / xUnit, Nakama Unity SDK.

---

## Verified Pre-Existing (do NOT rebuild)

- **Range rings:** `TowerSelectionManager.DrawRangePreview()` already draws an element-colored range ring on tower select and hides it on `ClearSelection`. (Task 1 = verify only.)
- **Level badges:** `DragonTower.EnsureLevelBadge()`/`UpdateLevelBadge()` already render `Lv{n}` / `FUSED` / `HYBRID`. (Task 2 = verify only.)
- **Backend progression** is stored as a raw JSON blob (`PlayerProgressionState.SaveJson`); the Unity DTO already carries `gacha_pulls_since_last_epic` / `gacha_total_pulls`. (Task 6 = round-trip test only, no migration.)

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Modify | `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs` | Set portrait sprite in `Setup` |
| Modify | `Unity/Assets/Scripts/Core/PlayerInventory.cs` | `LastTenPullResults`; Nakama login |
| Create | `Unity/Assets/Scripts/UI/SummonResultPanel.cs` | 10-pull modal |
| Modify | `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` | Show result panel after 10-pull |
| Create | `Backend/DragonTD.Tests/ProgressionPityTests.cs` | Pity round-trip test |
| Modify | `Unity/Packages/manifest.json` | Nakama SDK package |
| Create | `Unity/Assets/Scripts/Core/NakamaConfig.cs` | Host/port/key config SO |
| Create | `Unity/Assets/Scripts/Core/NakamaAuthService.cs` | Device auth |
| Create | `Unity/Assets/Scripts/Core/NakamaLeaderboardService.cs` | Submit/list scores |
| Modify | `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyData.cs` | `ShieldRegenDelay` field |
| Modify | `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs` | Shield-regen behavior |
| Create | `Unity/Assets/Scripts/TowerDefense/ChapterContent.cs` | Map + waveset bundle SO |
| Modify | `Unity/Assets/Scripts/Core/StageCatalog.cs` | Chapter 2 stages + `chapter` field |
| Modify | `Unity/Assets/Scripts/Core/GameManager.cs` | Select active chapter content |
| Modify | `Unity/Assets/Scripts/TowerDefense/Grid/GridManager.cs` | Build from a runtime MapDefinition |
| Modify | `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs` | Accept runtime waveset |
| Modify | `Unity/Assets/Editor/SceneBootstrapper.cs` | Generate Ch2 enemies/map/waves/content |

---

## Task 1: Verify Range Rings (no code unless broken)

**Files:** none expected.

- [ ] **Step 1: Inspect existing behavior**

Read `Unity/Assets/Scripts/TowerDefense/Combat/TowerSelectionManager.cs`. Confirm `SelectTower` → `DrawRangePreview()` enables `_rangePreview` and `ClearSelection` disables it, colored by `tower.ProjectileColor`.

- [ ] **Step 2: Manual confirm**

In Unity, Play `BattleScene`, place a dragon, click it. Expected: a colored ring appears at the tower's `AttackRange`; clicking empty space / Escape hides it.

- [ ] **Step 3: Only if broken**

If no ring appears, verify `OnMouseDown` → `TowerSelectionManager.Ensure().HandleTowerClicked(this)` fires (the tile collider must be disabled when occupied — already done). No commit if nothing changed.

---

## Task 2: Verify Level Badges (no code unless broken)

**Files:** none expected.

- [ ] **Step 1: Inspect**

Read `DragonTower.EnsureLevelBadge`/`UpdateLevelBadge`. Confirm badge text updates in `ApplyLevelVisuals`, called from `Setup` and `TryUpgrade`.

- [ ] **Step 2: Manual confirm**

Play, place a dragon (shows `Lv1`), upgrade it (shows `Lv2`, `Lv3`), fuse (shows `FUSED`/`HYBRID`). No commit if nothing changed.

---

## Task 3: Tower Portrait Sprite in Setup

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs`

The prefab gets the portrait at scene-build time, but runtime `Setup` never refreshes the `SpriteRenderer`. Set it so summoned/placed dragons always show their portrait.

- [ ] **Step 1: Add sprite assignment in `Setup`**

In `DragonTower.Setup(DragonInstance instance, GridTile placedTile, int manaCost)`, immediately after the line `_projectileColor = instance.Definition.visualData.primaryColor;` add:

```csharp
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Sprite portrait = instance.Definition.visualData != null ? instance.Definition.visualData.portrait : null;
                if (portrait != null)
                {
                    spriteRenderer.sprite = portrait;
                    spriteRenderer.color  = Color.white;
                }
                else
                {
                    spriteRenderer.color = _projectileColor;
                }
            }
```

- [ ] **Step 2: Compile-verify**

Open Unity (or batchmode) — confirm no compile errors in Console.

- [ ] **Step 3: Commit**

```bash
git add Unity/Assets/Scripts/TowerDefense/Combat/DragonTower.cs
git commit -m "feat: set dragon portrait sprite on tower Setup, color fallback when missing"
```

---

## Task 4: 10-Pull Result Screen

**Files:**
- Modify: `Unity/Assets/Scripts/Core/PlayerInventory.cs`
- Create: `Unity/Assets/Scripts/UI/SummonResultPanel.cs`
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

**Context:** `PlayerInventory.TryTenPullWithGems(out string[] messages)` already exists, adds 10 dragons via `AddDragon`, and sets `LastSummonSummary`. It does not expose the structured `DragonDefinition` results. Add that, then a panel that displays them.

- [ ] **Step 1: Add `LastTenPullResults` to PlayerInventory**

In `Unity/Assets/Scripts/Core/PlayerInventory.cs`, add a property near `LastSummonSummary`:

```csharp
        public System.Collections.Generic.List<DragonDefinition> LastTenPullResults { get; private set; } = new System.Collections.Generic.List<DragonDefinition>();
```

- [ ] **Step 2: Populate it inside `TryTenPullWithGems`**

In `TryTenPullWithGems`, find the loop that iterates `results` and calls `AddDragon`. Immediately before that loop, add:

```csharp
            LastTenPullResults.Clear();
```

Inside the loop, in the branch where `results[i] != null` (right after `AddDragon(results[i]);`), add:

```csharp
            LastTenPullResults.Add(results[i]);
```

- [ ] **Step 3: Create `SummonResultPanel.cs`**

Create `Unity/Assets/Scripts/UI/SummonResultPanel.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;
using DragonTD.Dragons;

namespace DragonTD.UI
{
    // Modal showing the 10 dragons from a 10-pull. Built at runtime under the given parent canvas.
    public class SummonResultPanel : MonoBehaviour
    {
        private GameObject _root;
        private Transform _cellContainer;
        private Font _font;

        public void Initialize(Transform parent, Font font)
        {
            _font = font;
            _root = new GameObject("SummonResultPanel");
            _root.transform.SetParent(parent, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.18f);
            rt.anchorMax = new Vector2(0.85f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.12f, 0.98f);

            var title = CreateText(_root.transform, "Title", "10-Pull Results",
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), 26, new Color(1f, 0.92f, 0.6f, 1f));
            title.alignment = TextAnchor.MiddleCenter;

            var containerGO = new GameObject("Cells");
            containerGO.transform.SetParent(_root.transform, false);
            var crt = containerGO.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.04f, 0.16f);
            crt.anchorMax = new Vector2(0.96f, 0.86f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            _cellContainer = containerGO.transform;

            var okButton = CreateButton(_root.transform, "OkButton", "OK",
                new Vector2(0.4f, 0.03f), new Vector2(0.6f, 0.13f));
            okButton.onClick.AddListener(() => _root.SetActive(false));

            _root.SetActive(false);
        }

        public void Show(List<DragonDefinition> results)
        {
            if (_root == null) return;
            foreach (Transform child in _cellContainer)
                Destroy(child.gameObject);

            int count = results != null ? results.Count : 0;
            for (int i = 0; i < 10; i++)
            {
                DragonDefinition def = i < count ? results[i] : null;
                CreateCell(i, def);
            }
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
        }

        private void CreateCell(int index, DragonDefinition def)
        {
            int col = index % 5;
            int row = index / 5;
            var cellGO = new GameObject($"Cell{index}");
            cellGO.transform.SetParent(_cellContainer, false);
            var rt = cellGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(col / 5f + 0.01f, 1f - (row + 1) / 2f + 0.02f);
            rt.anchorMax = new Vector2((col + 1) / 5f - 0.01f, 1f - row / 2f - 0.02f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = cellGO.AddComponent<Image>();
            img.color = def != null ? RarityColor(def.rarity) : new Color(0.1f, 0.1f, 0.14f, 1f);

            string label = def != null ? $"{def.displayName}\n{def.rarity}" : "—";
            var txt = CreateText(cellGO.transform, "Label", label,
                Vector2.zero, Vector2.one, 13, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private static Color RarityColor(DragonRarity rarity) => rarity switch
        {
            DragonRarity.Common    => new Color(0.42f, 0.45f, 0.5f, 0.95f),
            DragonRarity.Uncommon  => new Color(0.2f, 0.6f, 0.32f, 0.95f),
            DragonRarity.Rare      => new Color(0.18f, 0.45f, 0.85f, 0.95f),
            DragonRarity.Epic      => new Color(0.55f, 0.28f, 0.78f, 0.95f),
            DragonRarity.Legendary => new Color(0.9f, 0.65f, 0.12f, 0.95f),
            DragonRarity.Mythic    => new Color(0.85f, 0.2f, 0.25f, 0.95f),
            _                      => new Color(0.3f, 0.3f, 0.34f, 0.95f)
        };

        private Text CreateText(Transform parent, string name, string content,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = content;
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.55f, 0.3f, 1f);
            var btn = go.AddComponent<Button>();
            CreateText(go.transform, "Label", label, Vector2.zero, Vector2.one, 20, Color.white);
            return btn;
        }
    }
}
```

- [ ] **Step 4: Wire the panel into ProfileProgressionPanel**

In `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`, add a field near the other panel fields:

```csharp
        private SummonResultPanel _tenPullResultPanel;
```

Find the ten-pull button handler (the lambda calling `TryTenPullWithGems`). Replace its body with:

```csharp
            tenPullButton.onClick.AddListener(() =>
            {
                PlayerInventory inv = PlayerInventory.Instance;
                if (inv != null && inv.TryTenPullWithGems(out _))
                {
                    EnsureTenPullResultPanel();
                    _tenPullResultPanel.Show(inv.LastTenPullResults);
                }
                UpdateSummonPanelText();
                Refresh();
            });
```

Add the helper method:

```csharp
        private void EnsureTenPullResultPanel()
        {
            if (_tenPullResultPanel != null) return;
            Font font = _dragonDetailText != null ? _dragonDetailText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("TenPullResultPanelHost");
            go.transform.SetParent(transform, false);
            _tenPullResultPanel = go.AddComponent<SummonResultPanel>();
            _tenPullResultPanel.Initialize(transform, font);
        }
```

**Note:** the ten-pull button is created inside `EnsureSummonPanel`. The variable name there is `tenPullButton`. If the handler is registered elsewhere or under a different local name, search for `TryTenPullWithGems` in the file and replace that single call site with the block above.

- [ ] **Step 5: Compile-verify**

Open Unity — confirm Console shows no errors.

- [ ] **Step 6: Commit**

```bash
git add Unity/Assets/Scripts/Core/PlayerInventory.cs
git add Unity/Assets/Scripts/UI/SummonResultPanel.cs
git add Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs
git commit -m "feat: 10-pull result screen showing all 10 summoned dragons"
```

---

## Task 5: Backend Gacha Pity Round-Trip Test

**Files:**
- Create: `Backend/DragonTD.Tests/ProgressionPityTests.cs`

Progression is a raw JSON blob, so pity already persists. Lock it with a test.

- [ ] **Step 1: Write the test**

Create `Backend/DragonTD.Tests/ProgressionPityTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DragonTD.Tests;

public class ProgressionPityTests
{
    [Fact]
    public async Task GachaPityFieldsRoundTripThroughProgression()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("pity-device");

        var payload = new
        {
            version = 1,
            gacha_pulls_since_last_epic = 47,
            gacha_total_pulls = 213
        };
        var put = await client.PutAsJsonAsync("/api/v1/progression", payload);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync("/api/v1/progression");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(47,  body.RootElement.GetProperty("gacha_pulls_since_last_epic").GetInt32());
        Assert.Equal(213, body.RootElement.GetProperty("gacha_total_pulls").GetInt32());
    }
}
```

- [ ] **Step 2: Run the test**

```powershell
cd Backend
dotnet test DragonTD.sln -v minimal --filter "ProgressionPityTests"
```

Expected: 1 passed. (If the `AuthorizeAsync` helper signature differs, match the pattern used in `ProgressionControllerTests.cs`.)

- [ ] **Step 3: Run full backend suite**

```powershell
dotnet test DragonTD.sln -v minimal
```

Expected: all green.

- [ ] **Step 4: Commit**

```bash
git add Backend/DragonTD.Tests/ProgressionPityTests.cs
git commit -m "test: verify gacha pity fields round-trip through progression sync"
```

---

## Task 6: Nakama SDK + Config

**Files:**
- Modify: `Unity/Packages/manifest.json`
- Create: `Unity/Assets/Scripts/Core/NakamaConfig.cs`

- [ ] **Step 1: Add the Nakama package**

In `Unity/Packages/manifest.json`, add to the `dependencies` object:

```json
    "com.heroiclabs.nakama-unity": "https://github.com/heroiclabs/nakama-unity.git?path=/Packages/Nakama#v3.14.0",
```

- [ ] **Step 2: Let Unity resolve the package**

Open Unity. Wait for the Package Manager to fetch Nakama. Confirm `Nakama` namespace resolves (Console has no "namespace Nakama not found" errors). If resolution fails, report BLOCKED with the Console error.

- [ ] **Step 3: Create `NakamaConfig.cs`**

Create `Unity/Assets/Scripts/Core/NakamaConfig.cs`:

```csharp
using UnityEngine;

namespace DragonTD.Core
{
    // Dev defaults point at a local Nakama server. Do NOT commit production keys.
    [CreateAssetMenu(fileName = "NakamaConfig", menuName = "Dragon Dominion/Nakama Config")]
    public class NakamaConfig : ScriptableObject
    {
        public string scheme = "http";
        public string host = "127.0.0.1";
        public int port = 7350;
        public string serverKey = "defaultkey";
        public string battleLeaderboardId = "battle_score";
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Unity/Packages/manifest.json Unity/Assets/Scripts/Core/NakamaConfig.cs
git commit -m "chore: add Nakama Unity SDK and dev config ScriptableObject"
```

---

## Task 7: Nakama Auth + Leaderboard Services

**Files:**
- Create: `Unity/Assets/Scripts/Core/NakamaAuthService.cs`
- Create: `Unity/Assets/Scripts/Core/NakamaLeaderboardService.cs`
- Modify: `Unity/Assets/Scripts/Core/PlayerInventory.cs`

- [ ] **Step 1: Create `NakamaAuthService.cs`**

Create `Unity/Assets/Scripts/Core/NakamaAuthService.cs`:

```csharp
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace DragonTD.Core
{
    // Wraps Nakama device authentication.
    public class NakamaAuthService
    {
        private readonly NakamaConfig _config;
        public IClient Client { get; private set; }
        public ISession Session { get; private set; }
        public bool IsAuthenticated => Session != null && !Session.IsExpired;

        public NakamaAuthService(NakamaConfig config)
        {
            _config = config;
            Client = new Client(_config.scheme, _config.host, _config.port, _config.serverKey, UnityWebRequestAdapter.Instance);
        }

        public async Task<bool> AuthenticateDeviceAsync(string deviceId)
        {
            try
            {
                Session = await Client.AuthenticateDeviceAsync(deviceId);
                return IsAuthenticated;
            }
            catch (ApiResponseException ex)
            {
                Debug.LogWarning($"[Nakama] Auth failed: {ex.Message}");
                return false;
            }
        }
    }
}
```

- [ ] **Step 2: Create `NakamaLeaderboardService.cs`**

Create `Unity/Assets/Scripts/Core/NakamaLeaderboardService.cs`:

```csharp
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace DragonTD.Core
{
    // Submits and lists leaderboard scores via Nakama.
    public class NakamaLeaderboardService
    {
        private readonly IClient _client;
        private readonly ISession _session;

        public NakamaLeaderboardService(IClient client, ISession session)
        {
            _client = client;
            _session = session;
        }

        public async Task<bool> SubmitScoreAsync(string leaderboardId, long score)
        {
            if (_client == null || _session == null) return false;
            try
            {
                await _client.WriteLeaderboardRecordAsync(_session, leaderboardId, score);
                return true;
            }
            catch (ApiResponseException ex)
            {
                Debug.LogWarning($"[Nakama] Score submit failed: {ex.Message}");
                return false;
            }
        }

        public async Task<IApiLeaderboardRecordList> ListTopAsync(string leaderboardId, int limit)
        {
            if (_client == null || _session == null) return null;
            try
            {
                return await _client.ListLeaderboardRecordsAsync(_session, leaderboardId, ownerIds: null, expiry: null, limit, cursor: null);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogWarning($"[Nakama] List scores failed: {ex.Message}");
                return null;
            }
        }
    }
}
```

- [ ] **Step 3: Add Nakama login + score submit to PlayerInventory**

In `Unity/Assets/Scripts/Core/PlayerInventory.cs`, add fields near `_authSession`:

```csharp
        private NakamaAuthService _nakamaAuth;
        private NakamaLeaderboardService _nakamaLeaderboard;
        private NakamaConfig _nakamaConfig;
```

In `Awake()`, after the existing service init, add:

```csharp
            _nakamaConfig = Resources.Load<NakamaConfig>("NakamaConfig");
```

Add these public methods:

```csharp
        public async Task<bool> LoginWithNakamaAsync(string deviceId)
        {
            if (_nakamaConfig == null)
            {
                SetSyncStatus("Nakama config missing");
                return false;
            }
            _nakamaAuth = new NakamaAuthService(_nakamaConfig);
            SetSyncStatus("Nakama sign-in...");
            bool ok = await _nakamaAuth.AuthenticateDeviceAsync(deviceId);
            if (!ok)
            {
                SetSyncStatus("Nakama sign-in failed");
                return false;
            }
            _nakamaLeaderboard = new NakamaLeaderboardService(_nakamaAuth.Client, _nakamaAuth.Session);
            SetSyncStatus("Nakama connected");
            return true;
        }

        public async void SubmitBattleScoreToNakama(int score)
        {
            if (_nakamaLeaderboard == null || _nakamaConfig == null) return;
            await _nakamaLeaderboard.SubmitScoreAsync(_nakamaConfig.battleLeaderboardId, score);
        }
```

Ensure the file has `using System.Threading.Tasks;` (it already does — `SaveProgressionAsync` uses `Task`).

- [ ] **Step 4: Submit score on victory**

In `PlayerInventory.GrantBattleCompletionRewards(...)`, in the victory path (where `victory == true`), after the reward result is built (near `OnBattleRewardResultGranted?.Invoke(result);`), add:

```csharp
            if (victory)
                SubmitBattleScoreToNakama(result.wavesCleared * 100 + result.starsEarned * 50);
```

Place this once in the main (has-dragons) victory branch. If `result.starsEarned` isn't set yet at that point, use `result.wavesCleared * 100`.

- [ ] **Step 5: Compile-verify**

Open Unity. Confirm no compile errors. If Nakama types don't resolve, Task 6 Step 2 didn't complete — fix package resolution first.

- [ ] **Step 6: Commit**

```bash
git add Unity/Assets/Scripts/Core/NakamaAuthService.cs
git add Unity/Assets/Scripts/Core/NakamaLeaderboardService.cs
git add Unity/Assets/Scripts/Core/PlayerInventory.cs
git commit -m "feat: Nakama device auth and battle-score leaderboard submission"
```

---

## Task 8: Shield-Regen Enemy Field

**Files:**
- Modify: `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyData.cs`
- Modify: `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs`

`GlacialShield` (Chapter 2) regenerates its shield after it breaks. Add a data field + behavior.

- [ ] **Step 1: Add the field to EnemyData**

In `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyData.cs`, add after `RegenPerSecond`:

```csharp
        public float ShieldRegenDelay; // seconds after shield break before it restores; 0 = never
```

- [ ] **Step 2: Read EnemyBase shield handling**

Read `Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs`. Locate where the `Shielded` trait is initialized and where the shield breaks (search for `Shielded` and `shield`). Identify the field that tracks whether the shield is currently up (e.g. `_shieldActive` / `_hasShield`).

- [ ] **Step 3: Add regen behavior**

Using the actual field names found in Step 2, add a regen timer. After the shield breaks, record `Time.time`; in `Update`, if `_data.ShieldRegenDelay > 0` and the shield is down and `Time.time - _shieldBrokenTime >= _data.ShieldRegenDelay`, restore the shield (re-enable the shield flag and its visual). Match the existing field names and visual-toggle calls exactly — do not invent new visual methods; reuse whatever the break path uses, inverted.

If the shield system is more complex than a single bool, implement the minimal restore that mirrors the existing break, and report `DONE_WITH_CONCERNS` describing what you reused.

- [ ] **Step 4: Compile-verify + commit**

```bash
git add Unity/Assets/Scripts/TowerDefense/Enemies/EnemyData.cs
git add Unity/Assets/Scripts/TowerDefense/Enemies/EnemyBase.cs
git commit -m "feat: ShieldRegenDelay enemy field with shield-restore behavior"
```

---

## Task 9: ChapterContent + Runtime Chapter Loading

**Files:**
- Create: `Unity/Assets/Scripts/TowerDefense/ChapterContent.cs`
- Modify: `Unity/Assets/Scripts/TowerDefense/Grid/GridManager.cs`
- Modify: `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`

The current scene builds one hardcoded map + waveset. To support multiple chapters in one scene, introduce a `ChapterContent` bundle and let managers accept it at runtime.

- [ ] **Step 1: Create `ChapterContent.cs`**

Create `Unity/Assets/Scripts/TowerDefense/ChapterContent.cs`:

```csharp
using UnityEngine;

namespace DragonTD.TowerDefense
{
    // Bundles everything a chapter needs: its map layout and its ordered wave set.
    [CreateAssetMenu(fileName = "ChapterContent", menuName = "Dragon Dominion/Chapter Content")]
    public class ChapterContent : ScriptableObject
    {
        public int chapterNumber = 1;
        public MapDefinition map;
        public WaveData[] waves;
    }
}
```

- [ ] **Step 2: Add a static "active chapter" selector**

Add a static holder so the managers can read the chosen chapter before they build (set by `GameManager` in Task 10). In `ChapterContent.cs`, add a static field:

```csharp
        // Set before scene managers initialize; null = use scene-assigned defaults.
        public static ChapterContent Active;
```

- [ ] **Step 3: GridManager uses active chapter map if present**

In `Unity/Assets/Scripts/TowerDefense/Grid/GridManager.cs`, add a serialized fallback map field near the top:

```csharp
        [SerializeField] private MapDefinition _mapDefinition;
```

In `BuildGrid()` (or `Start` before `BuildGrid`), choose the map:

```csharp
            MapDefinition activeMap = ChapterContent.Active != null && ChapterContent.Active.map != null
                ? ChapterContent.Active.map
                : _mapDefinition;
```

If `activeMap != null`, derive path/bonus tiles from it (reuse `MapDefinition.GetPathTiles()` / `GetBonusType`) instead of the serialized `_pathTiles` arrays. If `activeMap == null`, keep current serialized behavior. Keep this change minimal — only override the tile-source when an active map exists.

**Note:** SceneBootstrapper already populates `_pathTiles` etc. from the Chapter 1 `MapDefinition` at build time, so when `ChapterContent.Active == null` the existing serialized arrays still drive Chapter 1. The runtime override only matters when a non-default chapter is selected.

- [ ] **Step 4: WaveManager uses active chapter waves if present**

In `Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs`, find where `_waves` is read (the serialized `WaveData[] _waves`). Add, in `Awake`/`Start` before any wave logic:

```csharp
            if (ChapterContent.Active != null && ChapterContent.Active.waves != null && ChapterContent.Active.waves.Length > 0)
                _waves = ChapterContent.Active.waves;
```

`_waves` is private serialized — assign directly inside WaveManager (this code lives in WaveManager, so it has access). Confirm `TotalWaves` derives from `_waves.Length`.

- [ ] **Step 5: Compile-verify + commit**

```bash
git add Unity/Assets/Scripts/TowerDefense/ChapterContent.cs
git add Unity/Assets/Scripts/TowerDefense/Grid/GridManager.cs
git add Unity/Assets/Scripts/TowerDefense/Waves/WaveManager.cs
git commit -m "feat: ChapterContent bundle + runtime chapter map/waveset override"
```

---

## Task 10: Stage Catalog Chapter 2 + GameManager wiring

**Files:**
- Modify: `Unity/Assets/Scripts/Core/StageCatalog.cs`
- Modify: `Unity/Assets/Scripts/Core/GameManager.cs`

- [ ] **Step 1: Add a `chapter` field to StageDefinition**

In `Unity/Assets/Scripts/Core/StageCatalog.cs`, add a public field and constructor param. Add field after `stageNumber`:

```csharp
        public int chapter = 1;
```

Add `int chapter` as the LAST constructor parameter (after `recommendedRoles` is `params`, so add `chapter` BEFORE the `params` arg). Change the constructor signature to:

```csharp
        public StageDefinition(string stageId, string stageNumber, string displayName, string enemyTheme, float difficultyMultiplier, float rewardMultiplier, string chestPreview, int recommendedLevel, int extraEnemiesPerGroup, float spawnRateMultiplier, int tenLifeStarRequirement, int maxLeaksForStar, int maxDragonsForStar, int chapter, params DragonRoleTag[] recommendedRoles)
```

Assign `this.chapter = chapter;` in the body. Update the 4 existing Chapter 1 entries to pass `1` in the new `chapter` position (before the role tags). Example for stage 1:

```csharp
            new StageDefinition("chapter_1_stage_1", "1-1", "Dominion Road", "Goblin Scout Line", 1f, 1f, "Common/Rare", 1, 0, 1f, 10, 2, 3, 1, DragonRoleTag.Damage),
```

- [ ] **Step 2: Add Chapter 2 stages**

Append to the `Stages` array (after stage 1-4):

```csharp
            new StageDefinition("chapter_2_stage_1", "2-1", "Frozen Pass", "Ice Shard rush", 1.7f, 1.6f, "Rare/Epic", 5, 1, 1.1f, 12, 1, 5, 2, DragonRoleTag.Slow, DragonRoleTag.Damage),
            new StageDefinition("chapter_2_stage_2", "2-2", "Glacier Hold", "Frost Brutes", 1.95f, 1.75f, "Rare/Epic/Legendary", 6, 2, 1.16f, 12, 1, 5, 2, DragonRoleTag.AntiShield, DragonRoleTag.Aoe),
            new StageDefinition("chapter_2_stage_3", "2-3", "Shardspire", "Shielded glacials", 2.2f, 1.9f, "Epic/Legendary", 7, 2, 1.22f, 14, 0, 6, 2, DragonRoleTag.AntiShield, DragonRoleTag.Damage),
            new StageDefinition("chapter_2_stage_4", "2-4", "Winter Throne", "Full ice assault", 2.5f, 2.1f, "Epic/Legendary/Mythic", 8, 3, 1.3f, 16, 0, 6, 2, DragonRoleTag.AntiFlying, DragonRoleTag.Slow, DragonRoleTag.Aoe)
```

- [ ] **Step 3: GameManager selects active chapter content**

In `Unity/Assets/Scripts/Core/GameManager.cs`, add a serialized array of chapter content and select by the chosen stage's chapter. Add field:

```csharp
        [SerializeField] private DragonTD.TowerDefense.ChapterContent[] _chapters;
```

In `StartBattle()` (or wherever `CurrentStageId` is resolved before the grid/waves build — early in battle setup), add:

```csharp
            var stage = StageCatalog.Get(CurrentStageId);
            DragonTD.TowerDefense.ChapterContent.Active = null;
            if (_chapters != null)
            {
                foreach (var c in _chapters)
                {
                    if (c != null && c.chapterNumber == stage.chapter) { DragonTD.TowerDefense.ChapterContent.Active = c; break; }
                }
            }
```

**Important ordering:** This must run before `GridManager`/`WaveManager` build. If those build in their own `Start()` and `GameManager.StartBattle()` is called later (e.g. from a button), the managers will already have built Chapter 1. In that case, set `ChapterContent.Active` from the MainMenu stage-select flow (before `BattleScene` loads) instead — store the selected chapter in a `static` that survives the scene load, and set `ChapterContent.Active` in `GameManager.Awake()`. Use whichever runs first; verify by logging `ChapterContent.Active?.chapterNumber` in `GridManager.BuildGrid`.

- [ ] **Step 4: Compile-verify + commit**

```bash
git add Unity/Assets/Scripts/Core/StageCatalog.cs
git add Unity/Assets/Scripts/Core/GameManager.cs
git commit -m "feat: Chapter 2 stages + GameManager active-chapter selection"
```

---

## Task 11: SceneBootstrapper — Chapter 2 Enemies, Map, Waves, Content

**Files:**
- Modify: `Unity/Assets/Editor/SceneBootstrapper.cs`

- [ ] **Step 1: Create 3 ice enemy data + prefabs**

In `SceneBootstrapper.Build()`, alongside the existing enemy creation, add three new `EnemyData` + prefabs. Follow the exact pattern of the existing `CreateRunnerData()` / `CreateEnemyPrefab(...)` calls. Add helper data builders:

```csharp
        static EnemyData CreateIceShardData()
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.EnemyName = "Ice Shard"; d.MaxHp = 240f; d.MoveSpeed = 2.6f; d.Armor = 10f;
            d.GoldValue = 14; d.DamageToBase = 1; d.Faction = EnemyFaction.Undead;
            d.Element = DragonElement.Ice; d.HasElement = true; d.Trait = EnemyTrait.Runner;
            string path = SODir + "/Enemies/IceShard.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(path) == null) AssetDatabase.CreateAsset(d, path);
            return AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        }

        static EnemyData CreateFrostBruteData()
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.EnemyName = "Frost Brute"; d.MaxHp = 1600f; d.MoveSpeed = 1.0f; d.Armor = 120f;
            d.GoldValue = 32; d.DamageToBase = 2; d.Faction = EnemyFaction.Troll;
            d.Element = DragonElement.Ice; d.HasElement = true; d.Trait = EnemyTrait.Brute;
            string path = SODir + "/Enemies/FrostBrute.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(path) == null) AssetDatabase.CreateAsset(d, path);
            return AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        }

        static EnemyData CreateGlacialShieldData()
        {
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.EnemyName = "Glacial Shield"; d.MaxHp = 900f; d.MoveSpeed = 1.5f; d.Armor = 60f;
            d.GoldValue = 26; d.DamageToBase = 1; d.Faction = EnemyFaction.Undead;
            d.Element = DragonElement.Ice; d.HasElement = true; d.Trait = EnemyTrait.Shielded;
            d.ShieldRegenDelay = 4f;
            string path = SODir + "/Enemies/GlacialShield.asset";
            if (AssetDatabase.LoadAssetAtPath<EnemyData>(path) == null) AssetDatabase.CreateAsset(d, path);
            return AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        }
```

Call them and create prefabs in `Build()` (after existing enemy prefab creation):

```csharp
            var iceShardData = CreateIceShardData();
            var frostBruteData = CreateFrostBruteData();
            var glacialShieldData = CreateGlacialShieldData();
            CreateEnemyPrefab("IceShard", iceShardData, new Color(0.7f, 0.9f, 1f), 0.6f);
            CreateEnemyPrefab("FrostBrute", frostBruteData, new Color(0.55f, 0.75f, 1f), 1.0f);
            CreateEnemyPrefab("GlacialShield", glacialShieldData, new Color(0.35f, 0.85f, 1f), 0.82f);
```

- [ ] **Step 2: Create Chapter 2 map asset**

Add a helper that creates `Assets/ScriptableObjects/Maps/Chapter2Map.asset` with a distinct path layout, mirroring `EnsureMapDefinition()`:

```csharp
        static MapDefinition EnsureChapter2Map()
        {
            string path = MapSODir + "/Chapter2Map.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MapDefinition>(path);
            if (existing != null) return existing;
            var def = ScriptableObject.CreateInstance<MapDefinition>();
            def.mapName = "Chapter 2";
            def.grid =
                "BBBBBBBBBBBB\n" +
                "PPPPPPPBBBBB\n" +
                "BBBBBBPBBBBB\n" +
                "BBBBBBPPPPPB\n" +
                "BBBBBBBBBBPB\n" +
                "BBBBBBBBBBPB\n" +
                "BBBBBBBBBBPP\n" +
                "BBBBBBBBBBBB";
            AssetDatabase.CreateAsset(def, path);
            AssetDatabase.SaveAssets();
            return def;
        }
```

- [ ] **Step 3: Generate 15 Chapter 2 waves**

After the existing `CreateWave("Wave15", ...)` calls, add `Chapter2_Wave01`–`Chapter2_Wave15` using the 3 ice enemy prefabs. Load the prefabs first:

```csharp
            var iceShardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/IceShard.prefab");
            var frostBrutePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/FrostBrute.prefab");
            var glacialShieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/GlacialShield.prefab");
```

Then 15 `CreateWave("Chapter2_Wave01", ...)` … `"Chapter2_Wave15"` calls with escalating counts (start ~8 ice shards, build to mixes of all three; rewards scaled ~1.3× the Chapter 1 values). Write all 15 explicitly — no loops, no placeholders. Use the same `EnemySpawnEntry` shape as the existing Chapter 1 waves.

- [ ] **Step 4: Create the two ChapterContent assets**

Add a helper that wires each chapter's map + waves into a `ChapterContent` asset:

```csharp
        static void EnsureChapterContents(MapDefinition ch1Map, MapDefinition ch2Map)
        {
            CreateChapterContent("Chapter1Content", 1, ch1Map, "Wave", 15);
            CreateChapterContent("Chapter2Content", 2, ch2Map, "Chapter2_Wave", 15);
        }

        static ChapterContent CreateChapterContent(string assetName, int number, MapDefinition map, string wavePrefix, int waveCount)
        {
            string dir = "Assets/ScriptableObjects/Chapters";
            EnsureDir(dir);
            string path = dir + "/" + assetName + ".asset";
            var content = AssetDatabase.LoadAssetAtPath<ChapterContent>(path);
            if (content == null)
            {
                content = ScriptableObject.CreateInstance<ChapterContent>();
                AssetDatabase.CreateAsset(content, path);
            }
            content.chapterNumber = number;
            content.map = map;
            var waves = new WaveData[waveCount];
            for (int w = 1; w <= waveCount; w++)
                waves[w - 1] = AssetDatabase.LoadAssetAtPath<WaveData>($"{SODir}/Waves/{wavePrefix}{w:D2}.asset");
            content.waves = waves;
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            return content;
        }
```

Call `EnsureChapterContents(mapDef, EnsureChapter2Map())` in `Build()` after Chapter 1 + Chapter 2 waves are created. Add `using DragonTD.TowerDefense;` is already present.

- [ ] **Step 5: Wire chapters into GameManager on the generated scene**

Where `CreateManagerRoot` configures `GameManager` (the `SerializedObject` for the GameManager component in the generated `BattleScene`), assign the `_chapters` array:

```csharp
            var ch1 = AssetDatabase.LoadAssetAtPath<ChapterContent>("Assets/ScriptableObjects/Chapters/Chapter1Content.asset");
            var ch2 = AssetDatabase.LoadAssetAtPath<ChapterContent>("Assets/ScriptableObjects/Chapters/Chapter2Content.asset");
            var chaptersProp = gmSerialized.FindProperty("_chapters");
            chaptersProp.arraySize = 2;
            chaptersProp.GetArrayElementAtIndex(0).objectReferenceValue = ch1;
            chaptersProp.GetArrayElementAtIndex(1).objectReferenceValue = ch2;
```

Use the actual `SerializedObject` variable name from `CreateManagerRoot` (read that method first; it wires `GameManager` already). If GameManager isn't currently configured via SerializedObject there, add a small block that does `new SerializedObject(gm)` → set `_chapters` → `ApplyModifiedProperties()`.

- [ ] **Step 6: Regenerate + verify**

Run `Dragon Dominion > ★ Build Battle Scene` in the Unity editor. Confirm Console shows the new assets created and no errors. Verify these files exist:

```powershell
Test-Path Unity\Assets\ScriptableObjects\Maps\Chapter2Map.asset
Test-Path Unity\Assets\ScriptableObjects\Chapters\Chapter2Content.asset
(Get-ChildItem Unity\Assets\ScriptableObjects\Waves\Chapter2_Wave*.asset).Count   # expect 15
(Get-ChildItem Unity\Assets\ScriptableObjects\Enemies\IceShard.asset).Count        # expect 1
```

- [ ] **Step 7: Commit**

```bash
git add Unity/Assets/Editor/SceneBootstrapper.cs
git add Unity/Assets/ScriptableObjects/Enemies/
git add Unity/Assets/ScriptableObjects/Maps/Chapter2Map.asset*
git add Unity/Assets/ScriptableObjects/Waves/Chapter2_Wave*
git add Unity/Assets/ScriptableObjects/Chapters/
git add Unity/Assets/Prefabs/Enemies/
git add Unity/Assets/Scenes/BattleScene.unity
git commit -m "feat: Chapter 2 ice enemies, map, 15 waves, and chapter content bundles"
```

---

## Task 12: Update Handoff Doc

**Files:**
- Modify: `docs/2026-05-27-dragon-dominion-prototype-handoff.md`

- [ ] **Step 1: Append a section** before "## Main Files To Read First":

```markdown
## Battle Polish, Chapter 2, Nakama — 2026-05-30

- Tower portraits now set on runtime Setup (was build-time only).
- 10-pull shows a result screen (SummonResultPanel) listing all 10 dragons by rarity.
- Range rings and level badges confirmed pre-existing (TowerSelectionManager / DragonTower).
- Gacha pity persists to backend via the existing JSON-blob progression sync (verified by ProgressionPityTests).
- Nakama device auth (NakamaAuthService) + battle-score leaderboard submission (NakamaLeaderboardService); requires a running Nakama server and Resources/NakamaConfig.asset.
- Chapter 2 (Ice Tundra): 3 new enemies (IceShard, FrostBrute, GlacialShield with shield regen), Chapter2Map, 15 Chapter2 waves, ChapterContent bundles. GameManager selects the active chapter from the chosen stage; GridManager/WaveManager build from ChapterContent.Active.
- Pending art: ice/snow tile sprites for Chapter2Map, 3 dragon portraits.
```

- [ ] **Step 2: Commit**

```bash
git add docs/2026-05-27-dragon-dominion-prototype-handoff.md
git commit -m "docs: handoff entry for battle polish, Chapter 2, Nakama"
```

---

## Self-Review

**Spec coverage:**
- Range rings → Task 1 (verify, pre-existing). ✓
- LV badge → Task 2 (verify, pre-existing). ✓
- 10-pull screen → Task 4. ✓
- Tower portrait → Task 3. ✓
- Chapter 2 (map + 3 enemies + 15 waves + stages) → Tasks 8–11. ✓
- Backend pity → Task 5 (test; JSON-blob already persists). ✓
- Nakama auth + leaderboard → Tasks 6–7. ✓

**Placeholder scan:** No TBD/TODO. Task 8 Step 3 and Task 10 Step 3 intentionally instruct reading actual field/method names first (shield internals, GameManager wiring) because those internals weren't fully read; the plan specifies exact behavior and fallback reporting rather than guessed symbols.

**Type consistency:**
- `LastTenPullResults` (List<DragonDefinition>) defined Task 4 Step 1, consumed Step 4. ✓
- `ChapterContent.Active` defined Task 9 Step 2, set Task 10 Step 3, read Task 9 Steps 3–4. ✓
- `EnemyData.ShieldRegenDelay` defined Task 8 Step 1, set Task 11 Step 1, used Task 8 Step 3. ✓
- `StageDefinition.chapter` defined Task 10 Step 1, read Task 10 Step 3. ✓
- `NakamaConfig.battleLeaderboardId` defined Task 6 Step 3, used Task 7 Step 3. ✓

**Scope flag:** Chapter 2 (Tasks 8–11) is the heaviest segment and touches grid/wave runtime loading. If Task 9's runtime override proves to conflict with SceneBootstrapper's serialized arrays during execution, treat Task 9 + 11 as the integration risk and verify the `ChapterContent.Active` ordering (Task 10 Step 3 note) before committing.

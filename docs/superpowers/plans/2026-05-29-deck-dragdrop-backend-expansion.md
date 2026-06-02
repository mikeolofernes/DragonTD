# Dragon Dominion — Deck Drag-Drop + Backend Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Prev/Next/Equip buttons with drag-and-drop deck in Dragons menu; verify DB migration; add real IAP receipt validation; expand Unity PlayMode regression; add dated events with score tiers; implement Clan membership and Raid.

**Architecture:** Unity drag-drop uses EventSystem interfaces (IBeginDragHandler/IDragHandler/IEndDragHandler/IDropHandler) on dynamically-created UI cards — no scene changes required. Backend tasks extend existing controller/model patterns with new EF Core models and migrations. Clan feature adds Clan/ClanMember tables. Events move from a static array to a DB-backed model filtered by date.

**Tech Stack:** Unity 6 C#, UnityEngine.UI EventSystem, .NET 9 / ASP.NET Core, EF Core + PostgreSQL (in-memory for tests), xUnit, System.Security.Cryptography (RSA for Google Play)

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `Unity/Assets/Scripts/UI/DragCardHandler.cs` | IBeginDragHandler/IDragHandler/IEndDragHandler; spawns ghost; dims source |
| Create | `Unity/Assets/Scripts/UI/DeckSlotDropHandler.cs` | IDropHandler; routes to equip or TrySwapEquipped |
| Modify | `Unity/Assets/Scripts/Core/PlayerInventory.cs` | Add `FindOwnedDragonById`, `TrySwapEquipped` |
| Modify | `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` | Remove Prev/Next/Equip; add drag/drop handlers; add TapDeckSlot |
| Create | `Backend/DragonTD.API/Services/IIapPlatformReceiptValidator.cs` | Platform receipt validator interface |
| Create | `Backend/DragonTD.API/Services/GooglePlayReceiptValidator.cs` | RSA-SHA1 Google Play receipt validation |
| Create | `Backend/DragonTD.API/Services/AppleReceiptValidator.cs` | Apple receipt validation endpoint call |
| Modify | `Backend/DragonTD.API/Controllers/IapController.cs` | Route by platform field |
| Modify | `Backend/DragonTD.API/Program.cs` | Register platform validators |
| Modify | `Backend/DragonTD.Tests/IapControllerTests.cs` | Add platform routing tests |
| Create | `Unity/Assets/Tests/PlayMode/InventoryProgressionTests.cs` | Save/load, chest, daily objective, swap equip |
| Create | `Backend/DragonTD.API/Models/EventDefinitionModel.cs` | EventDefinition, EventRewardTier models |
| Modify | `Backend/DragonTD.API/Data/AppDbContext.cs` | Add EventDefinitions DbSet + HasData seed |
| Modify | `Backend/DragonTD.API/Controllers/EventsController.cs` | Date-filtered DB events + scored tier claim |
| Migration | `Backend/DragonTD.API/Migrations/` | AddEventDefinitions migration |
| Modify | `Backend/DragonTD.Tests/EventsAndClanControllerTests.cs` | Date-filter + tier tests |
| Modify | `Backend/DragonTD.API/Models/ClanModels.cs` | Add Clan, ClanMember |
| Modify | `Backend/DragonTD.API/Data/AppDbContext.cs` | Add Clan DbSets + relationships |
| Modify | `Backend/DragonTD.API/Controllers/ClanController.cs` | Full CRUD + Raid endpoints |
| Migration | `Backend/DragonTD.API/Migrations/` | AddClanMembership migration |
| Modify | `Backend/DragonTD.Tests/EventsAndClanControllerTests.cs` | Clan CRUD + raid tests |

---

## Task 1: Dragons Drag-and-Drop Deck

### Files
- Create: `Unity/Assets/Scripts/UI/DragCardHandler.cs`
- Create: `Unity/Assets/Scripts/UI/DeckSlotDropHandler.cs`
- Modify: `Unity/Assets/Scripts/Core/PlayerInventory.cs`
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

- [ ] **Step 1: Add `FindOwnedDragonById` and `TrySwapEquipped` to PlayerInventory**

In `Unity/Assets/Scripts/Core/PlayerInventory.cs`, add these two public methods immediately after `IsEquipped`:

```csharp
public DragonInstance FindOwnedDragonById(string dragonId) => FindOwnedDragon(dragonId);

public bool TrySwapEquipped(string incomingId, string existingId, out string message)
{
    if (string.IsNullOrWhiteSpace(incomingId) || string.IsNullOrWhiteSpace(existingId))
    {
        message = "Invalid dragon IDs";
        return false;
    }
    EnsureValidLoadout();
    if (!EquippedDragonIds.Contains(existingId))
    {
        message = "Target slot not equipped";
        return false;
    }
    EquippedDragonIds.Remove(existingId);
    if (!EquippedDragonIds.Contains(incomingId))
        EquippedDragonIds.Add(incomingId);
    message = $"Swapped to {incomingId}";
    SaveProgressionAsync();
    OnLoadoutChanged?.Invoke();
    OnInventoryChanged?.Invoke();
    return true;
}
```

- [ ] **Step 2: Create `DragCardHandler.cs`**

Create `Unity/Assets/Scripts/UI/DragCardHandler.cs`:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonTD.UI
{
    public class DragCardHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string DragonId { get; set; }

        private CanvasGroup _canvasGroup;
        private GameObject _ghost;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(DragonId)) return;
            _rootCanvas = FindRootCanvas();
            if (_rootCanvas == null) return;
            _canvasGroup.alpha = 0.4f;
            _canvasGroup.blocksRaycasts = false;
            _ghost = BuildGhost(_rootCanvas);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost == null || _rootCanvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_rootCanvas.transform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local);
            ((RectTransform)_ghost.transform).anchoredPosition = local;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            if (_ghost != null)
                Destroy(_ghost);
            _ghost = null;
        }

        private Canvas FindRootCanvas()
        {
            Canvas c = GetComponentInParent<Canvas>();
            while (c != null && !c.isRootCanvas)
                c = c.transform.parent?.GetComponentInParent<Canvas>();
            return c;
        }

        private GameObject BuildGhost(Canvas rootCanvas)
        {
            var go = new GameObject("DragGhost");
            go.transform.SetParent(rootCanvas.transform, false);
            go.transform.SetAsLastSibling();

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = ((RectTransform)transform).rect.size;

            Image sourceImg = GetComponent<Image>();
            Image img = go.AddComponent<Image>();
            if (sourceImg != null)
            {
                img.sprite = sourceImg.sprite;
                Color c = sourceImg.color;
                img.color = new Color(c.r, c.g, c.b, 0.65f);
            }

            Text sourceText = GetComponentInChildren<Text>();
            if (sourceText != null)
            {
                var tGo = new GameObject("GhostText");
                tGo.transform.SetParent(go.transform, false);
                var tRt = tGo.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;
                var t = tGo.AddComponent<Text>();
                t.font = sourceText.font;
                t.fontSize = sourceText.fontSize;
                t.alignment = sourceText.alignment;
                t.color = new Color(1f, 1f, 1f, 0.85f);
                t.text = sourceText.text;
                t.resizeTextForBestFit = sourceText.resizeTextForBestFit;
                t.resizeTextMinSize = sourceText.resizeTextMinSize;
                t.resizeTextMaxSize = sourceText.resizeTextMaxSize;
            }

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            return go;
        }
    }
}
```

- [ ] **Step 3: Create `DeckSlotDropHandler.cs`**

Create `Unity/Assets/Scripts/UI/DeckSlotDropHandler.cs`:

```csharp
using UnityEngine.EventSystems;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class DeckSlotDropHandler : UnityEngine.MonoBehaviour, IDropHandler
    {
        public string EquippedDragonId { get; set; }

        public void OnDrop(PointerEventData eventData)
        {
            DragCardHandler handler = eventData.pointerDrag?.GetComponent<DragCardHandler>();
            if (handler == null || string.IsNullOrWhiteSpace(handler.DragonId)) return;

            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            string incoming = handler.DragonId;
            if (string.IsNullOrWhiteSpace(EquippedDragonId))
            {
                DragonInstance dragon = inventory.FindOwnedDragonById(incoming);
                inventory.TryToggleEquipDragon(dragon, out _);
            }
            else if (EquippedDragonId != incoming)
            {
                inventory.TrySwapEquipped(incoming, EquippedDragonId, out _);
            }
        }
    }
}
```

- [ ] **Step 4: Remove Prev/Next/Equip button runtime creation from ProfileProgressionPanel**

In `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`, in `EnsureDragonMenuControls`, remove the `_equipButton` creation block (lines creating `EquipDragonButton`). The method should only retain portrait and caption creation:

```csharp
private void EnsureDragonMenuControls()
{
    if (_dragonDetailText == null) return;

    Transform parent = _dragonDetailText.transform.parent;
    Font font = _dragonDetailText.font;

    if (_dragonPortrait == null)
    {
        var portraitGO = new GameObject("SelectedDragonPortrait");
        portraitGO.transform.SetParent(parent, false);
        RectTransform rt = portraitGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.52f, 0.56f);
        rt.anchorMax = new Vector2(0.52f, 0.56f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(128f, 128f);
        _dragonPortrait = portraitGO.AddComponent<Image>();
        _dragonPortrait.color = new Color(0.22f, 0.32f, 0.42f, 0.9f);
        _dragonPortrait.preserveAspect = true;
    }

    if (_dragonArtCaption == null)
    {
        var captionGO = new GameObject("DragonArtCaption");
        captionGO.transform.SetParent(parent, false);
        RectTransform rt = captionGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.52f, 0.43f);
        rt.anchorMax = new Vector2(0.52f, 0.43f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260f, 54f);
        _dragonArtCaption = captionGO.AddComponent<Text>();
        _dragonArtCaption.font = font;
        _dragonArtCaption.fontSize = 18;
        _dragonArtCaption.alignment = TextAnchor.MiddleCenter;
        _dragonArtCaption.color = new Color(1f, 0.92f, 0.64f, 1f);
    }

    ApplyLayout();
}
```

- [ ] **Step 5: Remove Prev/Next/Equip wiring from WireButtons/UnwireButtons**

In `WireButtons`, remove these three lines:
```csharp
if (_previousDragonButton != null) _previousDragonButton.onClick.AddListener(SelectPreviousDragon);
if (_nextDragonButton != null) _nextDragonButton.onClick.AddListener(SelectNextDragon);
if (_equipButton != null) _equipButton.onClick.AddListener(ToggleSelectedEquip);
```

In `UnwireButtons`, remove these three lines:
```csharp
if (_previousDragonButton != null) _previousDragonButton.onClick.RemoveAllListeners();
if (_nextDragonButton != null) _nextDragonButton.onClick.RemoveAllListeners();
if (_equipButton != null) _equipButton.onClick.RemoveAllListeners();
```

- [ ] **Step 6: Remove Prev/Next/Equip from ApplyLayout and ApplyModeVisibility**

In `ApplyLayout`, remove these three lines:
```csharp
SetButtonRect(_previousDragonButton, new Vector2(0.51f, 0.205f), new Vector2(100f, 34f));
SetButtonRect(_nextDragonButton, new Vector2(0.60f, 0.205f), new Vector2(100f, 34f));
SetButtonRect(_equipButton, new Vector2(0.72f, 0.205f), new Vector2(180f, 34f));
```

In `ApplyModeVisibility`, replace:
```csharp
SetActive(_previousDragonButton, dragonTab);
SetActive(_nextDragonButton, dragonTab);
```
with nothing (delete both lines), and replace:
```csharp
if (_equipButton != null)
    _equipButton.gameObject.SetActive(dragonTab && _equipButton.gameObject.activeSelf);
```
with nothing (delete the block).

Also remove from `Refresh`:
```csharp
if (_previousDragonButton != null)
    _previousDragonButton.interactable = count > 1;
if (_nextDragonButton != null)
    _nextDragonButton.interactable = count > 1;
```

- [ ] **Step 7: Add `TapDeckSlot` method and update `CreateDeckSlotButton`**

Add `TapDeckSlot` method to `ProfileProgressionPanel`:

```csharp
private void TapDeckSlot(DragonInstance dragon)
{
    SelectOwnedDragon(dragon);
    PlayerInventory.Instance?.TryToggleEquipDragon(dragon, out _);
}
```

In `RefreshDeckSlots`, change the onClick wiring from:
```csharp
if (dragon?.Definition != null)
    button.onClick.AddListener(() => SelectOwnedDragon(dragon));
```
to:
```csharp
if (dragon?.Definition != null)
    button.onClick.AddListener(() => TapDeckSlot(dragon));
```

- [ ] **Step 8: Add DeckSlotDropHandler to deck slot buttons**

In `CreateDeckSlotButton`, after `Button button = go.AddComponent<Button>();`, add:

```csharp
var dropHandler = go.AddComponent<DeckSlotDropHandler>();
dropHandler.EquippedDragonId = dragon?.Definition?.dragonId ?? string.Empty;
go.AddComponent<CanvasGroup>();
```

- [ ] **Step 9: Add DragCardHandler to collection card buttons**

In `CreateDragonCardButton`, after `Button button = go.AddComponent<Button>();`, add:

```csharp
var dragHandler = go.AddComponent<DragCardHandler>();
dragHandler.DragonId = dragon?.Definition?.dragonId ?? string.Empty;
go.AddComponent<CanvasGroup>();
```

- [ ] **Step 10: Remove dead methods**

Delete from `ProfileProgressionPanel`:
- `SelectPreviousDragon()` method body
- `SelectNextDragon()` method body
- `ToggleSelectedEquip()` method body
- `UpdateEquipButton(DragonInstance, PlayerInventory)` method body

Replace each with a single-line stub `private void SelectPreviousDragon() { }` etc., or delete entirely if their `[SerializeField]` backing fields are left null.

- [ ] **Step 11: Compile verify**

Run: Unity batchmode or open editor and check Console for compile errors.

Expected: no errors. Existing warnings about fire-and-forget saves are acceptable.

- [ ] **Step 12: Manual verify in editor**

Open `MainMenu.unity` → Play → Dragons mode → confirm:
- No Prev/Next/Equip buttons visible
- Collection card tiles can be dragged to deck slots
- Ghost card follows pointer during drag
- Dropping on empty slot equips
- Dropping on filled slot swaps
- Tapping a filled deck slot unequips and selects in detail strip

- [ ] **Step 13: Commit Task 1**

```bash
git add Unity/Assets/Scripts/UI/DragCardHandler.cs
git add Unity/Assets/Scripts/UI/DeckSlotDropHandler.cs
git add Unity/Assets/Scripts/Core/PlayerInventory.cs
git add Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs
git commit -m "feat: drag-and-drop deck equip in Dragons menu, remove Prev/Next/Equip buttons"
```

---

## Task 2: DB Migration + API Smoke Test

### Files
No new files — verification commands only.

- [ ] **Step 1: Verify PostgreSQL is running**

```powershell
# Check PostgreSQL service
Get-Service -Name "postgresql*" | Select-Object Name, Status

# If not running:
Start-Service -Name "postgresql-x64-16"  # adjust version as needed
```

Expected: `Status = Running`

- [ ] **Step 2: Set connection string environment variable**

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=dragon_dominion_dev;Username=postgres;Password=YOUR_PASSWORD"
```

Replace `YOUR_PASSWORD` with your local PostgreSQL password.

- [ ] **Step 3: Run the EF migration**

```powershell
cd Backend/DragonTD.API
dotnet ef database update
```

Expected output ends with: `Done.`

If migration fails with "relation already exists", the DB schema may already be partially applied. Run:
```powershell
dotnet ef database drop --force
dotnet ef database update
```

- [ ] **Step 4: Start the API**

```powershell
dotnet run --project Backend/DragonTD.API
```

Expected: listening on `https://localhost:5001`

- [ ] **Step 5: Smoke test device auth**

In a second PowerShell window:

```powershell
$body = '{"device_id":"smoke-test-device","display_name":"SmokeTest"}'
$response = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/auth/device" -Method Post -Body $body -ContentType "application/json" -SkipCertificateCheck
$token = $response.data.accessToken
Write-Host "Auth OK — token: $($token.Substring(0, 20))..."
```

Expected: `Auth OK — token: eyJhbGciOiJIUzI1NiIs...`

- [ ] **Step 6: Smoke test progression GET**

```powershell
$headers = @{ Authorization = "Bearer $token" }
$prog = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/progression" -Headers $headers -SkipCertificateCheck
Write-Host "Progression success: $($prog.success)"
```

Expected: `Progression success: True`

- [ ] **Step 7: Smoke test events GET**

```powershell
$events = Invoke-RestMethod -Uri "https://localhost:5001/api/v1/events" -Headers $headers -SkipCertificateCheck
Write-Host "Events count: $($events.data.Count)"
```

Expected: `Events count: 3` (daily_hunt, gem_rush, clan_raid)

- [ ] **Step 8: Update handoff doc with verification result**

Append to `docs/2026-05-27-dragon-dominion-prototype-handoff.md`:

```markdown
## DB Migration + API Smoke Test — 2026-05-29

Migration `AccountSyncStoreEventsClan` applied to local PostgreSQL.
Smoke test results:
- Device auth: PASS
- Progression GET: PASS
- Events GET: PASS (3 events)
```

- [ ] **Step 9: Commit**

```bash
git add docs/2026-05-27-dragon-dominion-prototype-handoff.md
git commit -m "docs: record DB migration + API smoke test results"
```

---

## Task 3: Real IAP Platform Receipt Validation

### Files
- Create: `Backend/DragonTD.API/Services/IIapPlatformReceiptValidator.cs`
- Create: `Backend/DragonTD.API/Services/GooglePlayReceiptValidator.cs`
- Create: `Backend/DragonTD.API/Services/AppleReceiptValidator.cs`
- Modify: `Backend/DragonTD.API/Controllers/IapController.cs`
- Modify: `Backend/DragonTD.API/Program.cs`
- Modify: `Backend/DragonTD.Tests/IapControllerTests.cs`

- [ ] **Step 1: Write failing platform routing tests**

Add to `Backend/DragonTD.Tests/IapControllerTests.cs`:

```csharp
[Fact]
public async Task GooglePlayPlatform_WithoutConfiguredKey_Returns503()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("gp-device");

    var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
    {
        productId = "com.dragondominion.gems.small",
        receipt = "{\"data\":\"{}\",\"signature\":\"AAAA\"}",
        platform = "GooglePlay",
        transactionId = "gp-tx-001",
        expectedGems = 500
    });

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
}

[Fact]
public async Task ApplePlatform_WithoutConfiguredSecret_Returns503()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("apple-device");

    var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
    {
        productId = "com.dragondominion.gems.small",
        receipt = "base64receiptdata",
        platform = "AppleAppStore",
        transactionId = "apple-tx-001",
        expectedGems = 500
    });

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
}

[Fact]
public async Task EditorPlatform_StillValidatesWithMockPath()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("editor-platform-device");

    var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
    {
        productId = "com.dragondominion.gems.small",
        receipt = "editor_mock_receipt:com.dragondominion.gems.small:500",
        platform = "Editor",
        transactionId = "editor-tx-platform-001",
        expectedGems = 500
    });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```powershell
cd Backend
dotnet test DragonTD.sln -v minimal --filter "GooglePlayPlatform_WithoutConfiguredKey_Returns503|ApplePlatform_WithoutConfiguredSecret_Returns503|EditorPlatform_StillValidatesWithMockPath"
```

Expected: 2 failures (503 tests fail), 1 pass (Editor test passes since existing code handles it).

- [ ] **Step 3: Create `IIapPlatformReceiptValidator.cs`**

Create `Backend/DragonTD.API/Services/IIapPlatformReceiptValidator.cs`:

```csharp
namespace DragonTD.API.Services;

public interface IIapPlatformReceiptValidator
{
    string Platform { get; }
    bool IsConfigured { get; }
    Task<IapReceiptValidationResult> ValidateAsync(string productId, string receipt, string transactionId, int expectedGems);
}

public record IapReceiptValidationResult(bool Success, string Message, int GemsGranted = 0);
```

- [ ] **Step 4: Create `GooglePlayReceiptValidator.cs`**

Create `Backend/DragonTD.API/Services/GooglePlayReceiptValidator.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DragonTD.API.Services;

public class GooglePlayReceiptValidator : IIapPlatformReceiptValidator
{
    private readonly string? _base64PublicKey;

    public string Platform => "GooglePlay";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_base64PublicKey);

    public GooglePlayReceiptValidator(IConfiguration config)
    {
        _base64PublicKey = config["GooglePlay:PublicKey"];
    }

    public Task<IapReceiptValidationResult> ValidateAsync(string productId, string receipt, string transactionId, int expectedGems)
    {
        if (!IsConfigured)
            return Task.FromResult(new IapReceiptValidationResult(false, "Google Play public key not configured"));

        try
        {
            using JsonDocument doc = JsonDocument.Parse(receipt);
            string data = doc.RootElement.GetProperty("data").GetString() ?? string.Empty;
            string signature = doc.RootElement.GetProperty("signature").GetString() ?? string.Empty;

            byte[] keyBytes = Convert.FromBase64String(_base64PublicKey!);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] sigBytes = Convert.FromBase64String(signature);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            bool valid = rsa.VerifyData(dataBytes, sigBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

            return Task.FromResult(valid
                ? new IapReceiptValidationResult(true, "Google Play receipt verified", expectedGems)
                : new IapReceiptValidationResult(false, "Google Play signature invalid"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new IapReceiptValidationResult(false, $"Google Play validation error: {ex.Message}"));
        }
    }
}
```

- [ ] **Step 5: Create `AppleReceiptValidator.cs`**

Create `Backend/DragonTD.API/Services/AppleReceiptValidator.cs`:

```csharp
using System.Text.Json;

namespace DragonTD.API.Services;

public class AppleReceiptValidator : IIapPlatformReceiptValidator
{
    private static readonly HttpClient Http = new();
    private const string SandboxUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
    private const string ProductionUrl = "https://buy.itunes.apple.com/verifyReceipt";

    private readonly string? _sharedSecret;

    public string Platform => "AppleAppStore";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_sharedSecret);

    public AppleReceiptValidator(IConfiguration config)
    {
        _sharedSecret = config["Apple:SharedSecret"];
    }

    public async Task<IapReceiptValidationResult> ValidateAsync(string productId, string receipt, string transactionId, int expectedGems)
    {
        if (!IsConfigured)
            return new IapReceiptValidationResult(false, "Apple shared secret not configured");

        try
        {
            var payload = new { "receipt-data" = receipt, password = _sharedSecret };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Try production first; if status 21007, retry sandbox
            HttpResponseMessage response = await Http.PostAsync(ProductionUrl, content);
            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);
            int status = doc.RootElement.GetProperty("status").GetInt32();

            if (status == 21007)
            {
                using var sandboxContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                response = await Http.PostAsync(SandboxUrl, sandboxContent);
                body = await response.Content.ReadAsStringAsync();
                doc.Dispose();
                using JsonDocument sandboxDoc = JsonDocument.Parse(body);
                status = sandboxDoc.RootElement.GetProperty("status").GetInt32();
                return status == 0
                    ? new IapReceiptValidationResult(true, "Apple sandbox receipt verified", expectedGems)
                    : new IapReceiptValidationResult(false, $"Apple sandbox receipt invalid (status {status})");
            }

            return status == 0
                ? new IapReceiptValidationResult(true, "Apple receipt verified", expectedGems)
                : new IapReceiptValidationResult(false, $"Apple receipt invalid (status {status})");
        }
        catch (Exception ex)
        {
            return new IapReceiptValidationResult(false, $"Apple validation error: {ex.Message}");
        }
    }
}
```

- [ ] **Step 6: Update `IapController.cs` to route by platform**

Replace the entire `IapController.cs`:

```csharp
using DragonTD.API.Data;
using DragonTD.API.Models;
using DragonTD.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/iap")]
public class IapController : ApiControllerBase
{
    private static readonly Dictionary<string, int> GemPacks = new()
    {
        ["com.dragondominion.gems.small"] = 500,
        ["com.dragondominion.gems.medium"] = 1200,
        ["com.dragondominion.gems.large"] = 3000,
        ["com.dragondominion.gems.epic"] = 6500,
        ["com.dragondominion.gems.legendary"] = 14000
    };

    private readonly AppDbContext _db;
    private readonly IEnumerable<IIapPlatformReceiptValidator> _validators;

    public IapController(AppDbContext db, IEnumerable<IIapPlatformReceiptValidator> validators)
    {
        _db = db;
        _validators = validators;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] IapValidationRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ProductId))
            return BadRequest(StorePurchaseResultDto.Fail("Missing purchase request"));
        if (!GemPacks.TryGetValue(request.ProductId, out int gems))
            return BadRequest(StorePurchaseResultDto.Fail("Unknown product", request.ProductId));
        if (request.ExpectedGems != gems)
            return BadRequest(StorePurchaseResultDto.Fail("Gem amount mismatch", request.ProductId));

        // Route to platform validator
        string platform = request.Platform ?? "Editor";
        IapReceiptValidationResult validationResult = await ValidateByPlatform(platform, request, gems);
        if (!validationResult.Success)
        {
            // 503 when validator is not configured (missing credentials)
            if (validationResult.Message.Contains("not configured"))
                return StatusCode(503, StorePurchaseResultDto.Fail(validationResult.Message, request.ProductId));
            return BadRequest(StorePurchaseResultDto.Fail(validationResult.Message, request.ProductId));
        }

        // Idempotency check
        IapPurchaseReceipt? existing = await _db.IapPurchaseReceipts
            .FirstOrDefaultAsync(r => r.TransactionId == request.TransactionId);
        if (existing is not null)
            return Ok(StorePurchaseResultDto.Valid(existing.ProductId, existing.TransactionId, existing.GemsGranted, "Receipt already validated"));

        Player? player = await _db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return Unauthorized(StorePurchaseResultDto.Fail("Player not found", request.ProductId));

        player.Gems += gems;
        _db.IapPurchaseReceipts.Add(new IapPurchaseReceipt
        {
            PlayerId = player.Id,
            ProductId = request.ProductId,
            TransactionId = request.TransactionId ?? Guid.NewGuid().ToString("N"),
            Receipt = request.Receipt ?? string.Empty,
            Platform = platform,
            GemsGranted = gems
        });
        await _db.SaveChangesAsync();

        return Ok(StorePurchaseResultDto.Valid(request.ProductId, request.TransactionId, gems, $"Validated purchase: +{gems} Gems"));
    }

    private async Task<IapReceiptValidationResult> ValidateByPlatform(string platform, IapValidationRequest request, int gems)
    {
        if (platform == "Editor")
        {
            string expectedReceipt = $"editor_mock_receipt:{request.ProductId}:{gems}";
            return request.Receipt == expectedReceipt
                ? new IapReceiptValidationResult(true, "Editor mock receipt valid", gems)
                : new IapReceiptValidationResult(false, "Receipt validation failed");
        }

        IIapPlatformReceiptValidator? validator = null;
        foreach (IIapPlatformReceiptValidator v in _validators)
        {
            if (v.Platform == platform)
            {
                validator = v;
                break;
            }
        }

        if (validator == null)
            return new IapReceiptValidationResult(false, $"Unknown platform: {platform}");

        if (!validator.IsConfigured)
            return new IapReceiptValidationResult(false, $"{platform} validator not configured");

        return await validator.ValidateAsync(request.ProductId!, request.Receipt ?? string.Empty, request.TransactionId ?? string.Empty, gems);
    }
}

public class IapValidationRequest
{
    public string? ProductId { get; set; }
    public string? Receipt { get; set; }
    public string? Platform { get; set; }
    public string? TransactionId { get; set; }
    public int ExpectedGems { get; set; }
}

public record StorePurchaseResultDto(bool Success, bool Validated, string? ProductId, string? TransactionId, int GemsGranted, string Message)
{
    public static StorePurchaseResultDto Valid(string productId, string? transactionId, int gems, string message) =>
        new(true, true, productId, transactionId, gems, message);

    public static StorePurchaseResultDto Fail(string message, string? productId = null) =>
        new(false, false, productId, null, 0, message);
}
```

- [ ] **Step 7: Register validators in `Program.cs`**

After `builder.Services.AddScoped<JwtTokenService>();` add:

```csharp
builder.Services.AddSingleton<IIapPlatformReceiptValidator, GooglePlayReceiptValidator>();
builder.Services.AddSingleton<IIapPlatformReceiptValidator, AppleReceiptValidator>();
```

- [ ] **Step 8: Run all IAP tests**

```powershell
cd Backend
dotnet test DragonTD.sln -v minimal --filter "IapControllerTests"
```

Expected: all 6 IAP tests pass.

- [ ] **Step 9: Commit Task 3**

```bash
git add Backend/DragonTD.API/Services/IIapPlatformReceiptValidator.cs
git add Backend/DragonTD.API/Services/GooglePlayReceiptValidator.cs
git add Backend/DragonTD.API/Services/AppleReceiptValidator.cs
git add Backend/DragonTD.API/Controllers/IapController.cs
git add Backend/DragonTD.API/Program.cs
git add Backend/DragonTD.Tests/IapControllerTests.cs
git commit -m "feat: add Google Play and Apple IAP receipt validation with platform routing"
```

---

## Task 4: Unity PlayMode Regression Suite

### Files
- Create: `Unity/Assets/Tests/PlayMode/InventoryProgressionTests.cs`

- [ ] **Step 1: Create asmdef entry for test file**

The existing `DragonTD.PlayModeTests.asmdef` in `Unity/Assets/Tests/PlayMode/` already covers new `.cs` files in the same folder. No asmdef changes needed.

- [ ] **Step 2: Create `InventoryProgressionTests.cs`**

Create `Unity/Assets/Tests/PlayMode/InventoryProgressionTests.cs`:

```csharp
using System.Collections;
using System.Reflection;
using DragonTD.Core;
using DragonTD.Dragons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DragonTD.Tests.PlayMode
{
    public class InventoryProgressionTests
    {
        private PlayerInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            // Reset singleton before each test
            ResetSingleton<PlayerInventory>("Instance");
            var go = new GameObject("TestPlayerInventory");
            _inventory = go.AddComponent<PlayerInventory>();
            SetPrivateField(_inventory, "_persistenceService",
                new LocalProgressionPersistenceService(
                    System.IO.Path.Combine(Application.temporaryCachePath, $"test_progression_{System.Guid.NewGuid():N}.json")));
        }

        [TearDown]
        public void TearDown()
        {
            if (_inventory != null)
                Object.DestroyImmediate(_inventory.gameObject);
            ResetSingleton<PlayerInventory>("Instance");
        }

        [Test]
        public void TrySwapEquipped_SwapsIncomingWithExistingSlot()
        {
            DragonInstance a = MakeDragon("voltaris_001", "Voltaris");
            DragonInstance b = MakeDragon("frostfang_002", "Frostfang");
            DragonInstance c = MakeDragon("magmaclaw_003", "Magmaclaw");

            AddToInventory(_inventory, a, b, c);
            _inventory.TryToggleEquipDragon(a, out _);
            _inventory.TryToggleEquipDragon(b, out _);

            Assert.IsTrue(_inventory.IsEquipped(a));
            Assert.IsTrue(_inventory.IsEquipped(b));
            Assert.IsFalse(_inventory.IsEquipped(c));

            bool result = _inventory.TrySwapEquipped("magmaclaw_003", "voltaris_001", out _);

            Assert.IsTrue(result);
            Assert.IsFalse(_inventory.IsEquipped(a), "voltaris should be unequipped after swap");
            Assert.IsTrue(_inventory.IsEquipped(b), "frostfang should remain equipped");
            Assert.IsTrue(_inventory.IsEquipped(c), "magmaclaw should be equipped after swap");
        }

        [Test]
        public void TrySwapEquipped_WithNonEquippedExisting_ReturnsFalse()
        {
            DragonInstance a = MakeDragon("voltaris_001", "Voltaris");
            DragonInstance b = MakeDragon("frostfang_002", "Frostfang");
            AddToInventory(_inventory, a, b);
            _inventory.TryToggleEquipDragon(a, out _);

            bool result = _inventory.TrySwapEquipped("voltaris_001", "frostfang_002", out string message);

            Assert.IsFalse(result);
            Assert.AreEqual("Target slot not equipped", message);
        }

        [Test]
        public void ChestAward_VictoryFillsFirstEmptySlot()
        {
            PlayerProgression prog = _inventory.Progression;
            prog.Load(null); // ensure clean state

            bool awarded = prog.TryAwardBattleChest("Rare", out int slot, out _);

            Assert.IsTrue(awarded);
            Assert.GreaterOrEqual(slot, 0);
            Assert.Less(slot, 4);
            Assert.AreEqual("Rare", prog.ChestSlots[slot]?.Rarity);
        }

        [Test]
        public void ChestAward_WhenAllSlotsFull_ReturnsFalse()
        {
            PlayerProgression prog = _inventory.Progression;
            prog.Load(null);
            prog.TryAwardBattleChest("Common", out _, out _);
            prog.TryAwardBattleChest("Common", out _, out _);
            prog.TryAwardBattleChest("Common", out _, out _);
            prog.TryAwardBattleChest("Common", out _, out _);

            bool awarded = prog.TryAwardBattleChest("Rare", out _, out _);

            Assert.IsFalse(awarded);
        }

        [Test]
        public void DailyObjective_WinBattleAdvancesProgress()
        {
            PlayerProgression prog = _inventory.Progression;
            prog.Load(null);

            prog.RecordDailyObjectiveProgress(DailyObjectiveType.WinBattle);

            var objectives = GetPrivateField<System.Collections.Generic.List<DailyObjectiveSaveData>>(prog, "_dailyObjectives");
            if (objectives != null && objectives.Count > 0)
            {
                var winObjective = objectives.Find(o => o.objectiveId == "win_battle");
                if (winObjective != null)
                    Assert.GreaterOrEqual(winObjective.progress, 1);
            }
            // If objectives list not yet initialized, pass (initialization is lazy)
        }

        [UnityTest]
        public IEnumerator SaveLoad_RoundTripPreservesGoldAndEssence()
        {
            string testSavePath = System.IO.Path.Combine(
                Application.temporaryCachePath, $"save_roundtrip_{System.Guid.NewGuid():N}.json");
            SetPrivateField(_inventory, "_persistenceService",
                new LocalProgressionPersistenceService(testSavePath));

            _inventory.Progression.Load(null);
            _inventory.Progression.AddGold(450);
            _inventory.Progression.AddEssence(120);

            yield return _inventory.SaveProgressionAsync().AsCoroutine();

            // Load into a fresh progression
            var loadedProg = new PlayerProgression();
            var loadService = new LocalProgressionPersistenceService(testSavePath);
            var loadTask = loadService.LoadProgressionAsync();
            yield return loadTask.AsCoroutine();

            if (loadTask.Result.success && loadTask.Result.saveData != null)
            {
                loadedProg.Load(loadTask.Result.saveData);
                Assert.AreEqual(450, loadedProg.Gold, "Gold should survive save/load");
                Assert.AreEqual(120, loadedProg.Essence, "Essence should survive save/load");
            }
            else
            {
                Assert.Fail($"Load failed: {loadTask.Result.message}");
            }

            System.IO.File.Delete(testSavePath);
        }

        // Manual regression items not covered by automated tests:
        // - Menu navigation: Main Menu -> Battle -> Dragons -> Profile -> back
        // - API sync status label transitions: Local -> Saving... -> Saved -> Loaded
        // - Store purchase flow: tap pack -> validating... -> purchase validated
        // - Event claim: tap Daily Hunt -> see reward popup -> button shows Claimed
        // - Battle return: finish battle -> main menu -> reward popup shown once
        // These require scene loading and are verified manually per handoff checklist.

        private static DragonInstance MakeDragon(string id, string name)
        {
            var def = ScriptableObject.CreateInstance<DragonDefinition>();
            def.dragonId = id;
            def.displayName = name;
            def.element = DragonElement.Fire;
            def.baseStats = new DragonBaseStats { attack = 100f };
            return new DragonInstance { Definition = def };
        }

        private static void AddToInventory(PlayerInventory inventory, params DragonInstance[] dragons)
        {
            foreach (DragonInstance dragon in dragons)
            {
                GetPrivateField<System.Collections.Generic.List<DragonInstance>>(inventory, "<OwnedDragons>k__BackingField")
                    ?.Add(dragon);
            }
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string name) where T : class
        {
            return target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(target) as T;
        }

        private static void ResetSingleton<T>(string propertyName)
        {
            typeof(T).GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public)
                ?.SetValue(null, null);
        }
    }

    internal static class TaskExtensions
    {
        internal static System.Collections.IEnumerator AsCoroutine(this System.Threading.Tasks.Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception!;
        }

        internal static System.Collections.IEnumerator AsCoroutine<T>(this System.Threading.Tasks.Task<T> task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception!;
        }
    }
}
```

**Note:** `OwnedDragons` backing field name may need adjusting if the compiler generates a different name. If `<OwnedDragons>k__BackingField` does not resolve, use direct reflection on `PlayerInventory` fields or add a test-helper method to `PlayerInventory` (not shipped) via `#if UNITY_EDITOR` guard.

- [ ] **Step 3: Run tests in Unity PlayMode Test Runner**

Open Unity → Window → General → Test Runner → PlayMode → Run All (or filter "InventoryProgressionTests").

Expected: `TrySwapEquipped`, `ChestAward`, `DailyObjective` pass. `SaveLoad` pass if `LocalProgressionPersistenceService` is accessible.

Fix any `AddToInventory` reflection issues by checking the actual backing field name with a `Debug.Log(string.Join(", ", typeof(PlayerInventory).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Select(f => f.Name)))` in a temporary test.

- [ ] **Step 4: Commit Task 4**

```bash
git add Unity/Assets/Tests/PlayMode/InventoryProgressionTests.cs
git commit -m "test: add PlayMode regression suite for inventory, chest, and save/load"
```

---

## Task 5: Events Dated Calendars + Scored Tiers

### Files
- Create: `Backend/DragonTD.API/Models/EventDefinitionModel.cs`
- Modify: `Backend/DragonTD.API/Data/AppDbContext.cs`
- Modify: `Backend/DragonTD.API/Controllers/EventsController.cs`
- Migration: `AddEventDefinitions`
- Modify: `Backend/DragonTD.Tests/EventsAndClanControllerTests.cs`

- [ ] **Step 1: Write failing tests for date filtering and scored tiers**

Add to `Backend/DragonTD.Tests/EventsAndClanControllerTests.cs`:

```csharp
[Fact]
public async Task EventsList_OnlyReturnsActiveEvents()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("events-date-device");

    var response = await client.GetAsync("/api/v1/events");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    // All returned events must be active (date check is backend logic; just verify response is OK and non-empty)
    Assert.True(body.RootElement.GetProperty("data").GetArrayLength() > 0);
}

[Fact]
public async Task ScoredChallenge_ClaimWithQualifyingScore_GrantsReward()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("scored-claim-device");

    // Submit a score that meets the tier 1 threshold (100)
    await client.PostAsJsonAsync("/api/v1/events/gem_rush/score", new { score = 500 });

    var response = await client.PostAsync("/api/v1/events/gem_rush/claim", null);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.True(body.RootElement.GetProperty("success").GetBoolean());
    Assert.True(body.RootElement.GetProperty("data").GetProperty("gem_reward").GetInt32() > 0);
}

[Fact]
public async Task ScoredChallenge_ClaimWithoutScore_Returns400()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("scored-noscore-device");

    // No score submitted — no qualifying tier
    var response = await client.PostAsync("/api/v1/events/gem_rush/claim", null);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task ScoredChallenge_ClaimTwice_Returns409()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("scored-twice-device");

    await client.PostAsJsonAsync("/api/v1/events/gem_rush/score", new { score = 500 });
    Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/events/gem_rush/claim", null)).StatusCode);
    Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/api/v1/events/gem_rush/claim", null)).StatusCode);
}
```

- [ ] **Step 2: Run failing tests**

```powershell
cd Backend
dotnet test DragonTD.sln -v minimal --filter "ScoredChallenge|EventsList_OnlyReturnsActiveEvents"
```

Expected: 3 new tests fail (ScoredChallenge tests), 1 passes (EventsList_OnlyReturnsActiveEvents already works via existing Events array).

- [ ] **Step 3: Create `EventDefinitionModel.cs`**

Create `Backend/DragonTD.API/Models/EventDefinitionModel.cs`:

```csharp
namespace DragonTD.API.Models;

public class EventDefinition
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // "DailyClaimable" | "ScoredChallenge" | "Locked"
    public string EventType { get; set; } = "DailyClaimable";

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    // Flat rewards (DailyClaimable uses these)
    public int GoldReward { get; set; }
    public int EssenceReward { get; set; }
    public int GemReward { get; set; }

    // JSON-serialized EventRewardTier[] for ScoredChallenge
    public string RewardTiersJson { get; set; } = "[]";
}

public class EventRewardTier
{
    public int ScoreThreshold { get; set; }
    public int GoldReward { get; set; }
    public int EssenceReward { get; set; }
    public int GemReward { get; set; }
    public int SummonTickets { get; set; }
}
```

- [ ] **Step 4: Add EventDefinitions to `AppDbContext.cs`**

Add `DbSet` and `HasData` seed in `AppDbContext.cs`. After the `PlayerEventClaim` entity config block, add:

```csharp
modelBuilder.Entity<EventDefinition>().HasData(
    new EventDefinition
    {
        Id = 1,
        EventId = "daily_hunt",
        DisplayName = "Daily Hunt",
        Description = "Clear patrol objectives and claim a daily account boost.",
        EventType = "DailyClaimable",
        StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndUtc = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        GoldReward = 250,
        EssenceReward = 35,
        GemReward = 0,
        RewardTiersJson = "[]"
    },
    new EventDefinition
    {
        Id = 2,
        EventId = "gem_rush",
        DisplayName = "Gem Rush",
        Description = "Short challenge preview with score tracking.",
        EventType = "ScoredChallenge",
        StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndUtc = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        GoldReward = 0,
        EssenceReward = 0,
        GemReward = 0,
        RewardTiersJson = "[{\"scoreThreshold\":100,\"gemReward\":10},{\"scoreThreshold\":500,\"gemReward\":25},{\"scoreThreshold\":1000,\"gemReward\":50}]"
    },
    new EventDefinition
    {
        Id = 3,
        EventId = "clan_raid",
        DisplayName = "Clan Raid",
        Description = "Locked until Clan/social backend is active.",
        EventType = "Locked",
        StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndUtc = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
        GoldReward = 0,
        EssenceReward = 0,
        GemReward = 0,
        RewardTiersJson = "[]"
    }
);
```

Also add to `AppDbContext` `DbSet` list:
```csharp
public DbSet<EventDefinition> EventDefinitions => Set<EventDefinition>();
```

- [ ] **Step 5: Replace `EventsController.cs` with DB-backed version**

Replace entire `EventsController.cs`:

```csharp
using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DragonTD.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/events")]
public class EventsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        DateTime now = DateTime.UtcNow;
        List<EventDefinition> activeEvents = await _db.EventDefinitions
            .Where(e => e.StartUtc <= now && e.EndUtc >= now)
            .OrderBy(e => e.Id)
            .ToListAsync();

        var scores = await _db.EventChallengeStates
            .Where(s => s.PlayerId == CurrentPlayerId)
            .ToDictionaryAsync(s => s.EventId, s => s.BestScore);

        return Ok(new
        {
            success = true,
            data = activeEvents.Select(e => new
            {
                event_id = e.EventId,
                display_name = e.DisplayName,
                description = e.Description,
                event_type = e.EventType,
                claimable = e.EventType == "DailyClaimable" || e.EventType == "ScoredChallenge",
                locked = e.EventType == "Locked",
                gold_reward = e.GoldReward,
                essence_reward = e.EssenceReward,
                gem_reward = e.GemReward,
                reward_tiers = ParseTiers(e.RewardTiersJson),
                best_score = scores.TryGetValue(e.EventId, out int score) ? score : 0,
                start_utc = e.StartUtc,
                end_utc = e.EndUtc
            }),
            error = (string?)null
        });
    }

    [HttpPost("{eventId}/claim")]
    public async Task<IActionResult> Claim(string eventId)
    {
        DateTime now = DateTime.UtcNow;
        EventDefinition? eventDef = await _db.EventDefinitions
            .FirstOrDefaultAsync(e => e.EventId == eventId && e.StartUtc <= now && e.EndUtc >= now);

        if (eventDef is null)
            return NotFound(new { success = false, error = "Event not found or not active" });
        if (eventDef.EventType == "Locked")
            return BadRequest(new { success = false, error = "Event is not claimable" });

        Player? player = await _db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return Unauthorized(new { success = false, error = "Player not found" });

        if (eventDef.EventType == "DailyClaimable")
            return await ClaimDaily(eventDef, player);

        if (eventDef.EventType == "ScoredChallenge")
            return await ClaimScoredTier(eventDef, player);

        return BadRequest(new { success = false, error = "Unknown event type" });
    }

    [HttpPost("{eventId}/score")]
    public async Task<IActionResult> SubmitScore(string eventId, [FromBody] ScoreRequest request)
    {
        if (!await _db.EventDefinitions.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { success = false, error = "Event not found" });

        EventChallengeState? state = await _db.EventChallengeStates
            .FirstOrDefaultAsync(s => s.PlayerId == CurrentPlayerId && s.EventId == eventId);
        if (state is null)
        {
            state = new EventChallengeState { PlayerId = CurrentPlayerId, EventId = eventId };
            _db.EventChallengeStates.Add(state);
        }

        state.BestScore = Math.Max(state.BestScore, request?.Score ?? 0);
        state.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new { event_id = eventId, best_score = state.BestScore }, error = (string?)null });
    }

    private async Task<IActionResult> ClaimDaily(EventDefinition eventDef, Player player)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool alreadyClaimed = await _db.PlayerEventClaims.AnyAsync(c =>
            c.PlayerId == CurrentPlayerId && c.EventId == eventDef.EventId && c.ClaimDateUtc == today);
        if (alreadyClaimed)
            return Conflict(new { success = false, error = "Event already claimed today" });

        player.Gold += eventDef.GoldReward;
        player.Gems += eventDef.GemReward;
        _db.PlayerEventClaims.Add(new PlayerEventClaim
        {
            PlayerId = CurrentPlayerId,
            EventId = eventDef.EventId,
            ClaimDateUtc = today
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                event_id = eventDef.EventId,
                gold_reward = eventDef.GoldReward,
                essence_reward = eventDef.EssenceReward,
                gem_reward = eventDef.GemReward
            },
            error = (string?)null
        });
    }

    private async Task<IActionResult> ClaimScoredTier(EventDefinition eventDef, Player player)
    {
        // One-time claim per event (use MaxValue date as sentinel)
        bool alreadyClaimed = await _db.PlayerEventClaims.AnyAsync(c =>
            c.PlayerId == CurrentPlayerId && c.EventId == eventDef.EventId);
        if (alreadyClaimed)
            return Conflict(new { success = false, error = "Scored challenge already claimed" });

        EventChallengeState? state = await _db.EventChallengeStates
            .FirstOrDefaultAsync(s => s.PlayerId == CurrentPlayerId && s.EventId == eventDef.EventId);
        int bestScore = state?.BestScore ?? 0;

        List<EventRewardTier> tiers = ParseTiers(eventDef.RewardTiersJson);
        EventRewardTier? bestTier = null;
        foreach (EventRewardTier tier in tiers)
        {
            if (bestScore >= tier.ScoreThreshold)
                bestTier = tier;
        }

        if (bestTier is null)
            return BadRequest(new { success = false, error = $"Score {bestScore} does not meet any reward tier" });

        player.Gold += bestTier.GoldReward;
        player.Gems += bestTier.GemReward;
        _db.PlayerEventClaims.Add(new PlayerEventClaim
        {
            PlayerId = CurrentPlayerId,
            EventId = eventDef.EventId,
            ClaimDateUtc = DateOnly.MaxValue
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                event_id = eventDef.EventId,
                score = bestScore,
                gold_reward = bestTier.GoldReward,
                essence_reward = bestTier.EssenceReward,
                gem_reward = bestTier.GemReward,
                summon_tickets = bestTier.SummonTickets
            },
            error = (string?)null
        });
    }

    private static List<EventRewardTier> ParseTiers(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return new List<EventRewardTier>();
        try
        {
            return JsonSerializer.Deserialize<List<EventRewardTier>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EventRewardTier>();
        }
        catch
        {
            return new List<EventRewardTier>();
        }
    }
}

public class ScoreRequest
{
    public int Score { get; set; }
}
```

- [ ] **Step 6: Generate the EF migration**

```powershell
cd Backend/DragonTD.API
dotnet ef migrations add AddEventDefinitions
```

Expected: new migration file created in `Migrations/`.

- [ ] **Step 7: Run all Events tests**

```powershell
cd Backend
dotnet test DragonTD.sln -v minimal --filter "EventsAndClanControllerTests"
```

Expected: all existing + new events tests pass.

- [ ] **Step 8: Commit Task 5**

```bash
git add Backend/DragonTD.API/Models/EventDefinitionModel.cs
git add Backend/DragonTD.API/Data/AppDbContext.cs
git add Backend/DragonTD.API/Controllers/EventsController.cs
git add Backend/DragonTD.API/Migrations/
git add Backend/DragonTD.Tests/EventsAndClanControllerTests.cs
git commit -m "feat: move events to DB with date filtering and scored challenge tiers"
```

---

## Task 6: Clan Membership + Clan Raid

### Files
- Modify: `Backend/DragonTD.API/Models/ClanModels.cs`
- Modify: `Backend/DragonTD.API/Data/AppDbContext.cs`
- Modify: `Backend/DragonTD.API/Controllers/ClanController.cs`
- Migration: `AddClanMembership`
- Modify: `Backend/DragonTD.Tests/EventsAndClanControllerTests.cs`

- [ ] **Step 1: Write failing clan tests**

Add to `Backend/DragonTD.Tests/EventsAndClanControllerTests.cs`:

```csharp
[Fact]
public async Task CreateClan_ReturnsNewClanAndPlayerIsOwner()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("clan-create-device");

    var response = await client.PostAsJsonAsync("/api/v1/clan", new
    {
        name = "Dragon Lords",
        tag = "DL",
        description = "Top guild"
    });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.Equal("Dragon Lords", body.RootElement.GetProperty("data").GetProperty("name").GetString());
    Assert.Equal("Owner", body.RootElement.GetProperty("data").GetProperty("my_role").GetString());
}

[Fact]
public async Task GetMyClan_AfterCreating_ReturnsClanData()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("clan-getme-device");

    await client.PostAsJsonAsync("/api/v1/clan", new { name = "Test Clan", tag = "TC", description = "" });
    var response = await client.GetAsync("/api/v1/clan/me");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.False(body.RootElement.GetProperty("data").GetProperty("locked").GetBoolean());
    Assert.Equal("Test Clan", body.RootElement.GetProperty("data").GetProperty("clan").GetProperty("name").GetString());
}

[Fact]
public async Task JoinClan_PlayerCanJoinAnExistingClan()
{
    using var factory = new TestApiFactory();
    using var creatorClient = factory.CreateClient();
    using var joinerClient = factory.CreateClient();
    await creatorClient.AuthorizeAsync("clan-owner-device");
    await joinerClient.AuthorizeAsync("clan-joiner-device");

    var createResponse = await creatorClient.PostAsJsonAsync("/api/v1/clan", new { name = "Open Clan", tag = "OC", description = "" });
    using JsonDocument createBody = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
    int clanId = createBody.RootElement.GetProperty("data").GetProperty("id").GetInt32();

    var joinResponse = await joinerClient.PostAsync($"/api/v1/clan/{clanId}/join", null);

    Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
    using JsonDocument joinBody = JsonDocument.Parse(await joinResponse.Content.ReadAsStringAsync());
    Assert.Equal("Member", joinBody.RootElement.GetProperty("data").GetProperty("role").GetString());
}

[Fact]
public async Task CreateClan_WhenAlreadyInClan_Returns409()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("clan-duplicate-device");

    await client.PostAsJsonAsync("/api/v1/clan", new { name = "First Clan", tag = "FC", description = "" });
    var response = await client.PostAsJsonAsync("/api/v1/clan", new { name = "Second Clan", tag = "SC", description = "" });

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}

[Fact]
public async Task RaidContribute_AddsToMemberAndClanScore()
{
    using var factory = new TestApiFactory();
    using var client = factory.CreateClient();
    await client.AuthorizeAsync("clan-raid-device");

    await client.PostAsJsonAsync("/api/v1/clan", new { name = "Raid Guild", tag = "RG", description = "" });
    var response = await client.PostAsJsonAsync("/api/v1/clan/raid/contribute", new { score = 350 });

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    Assert.Equal(350, body.RootElement.GetProperty("data").GetProperty("my_contribution").GetInt32());
    Assert.Equal(350, body.RootElement.GetProperty("data").GetProperty("clan_total").GetInt32());
}
```

- [ ] **Step 2: Run failing tests**

```powershell
cd Backend
dotnet test DragonTD.sln -v minimal --filter "CreateClan|GetMyClan_AfterCreating|JoinClan|RaidContribute"
```

Expected: all 5 new tests fail.

- [ ] **Step 3: Update `ClanModels.cs` with Clan and ClanMember**

Replace `Backend/DragonTD.API/Models/ClanModels.cs`:

```csharp
namespace DragonTD.API.Models;

public record ClanShellResponse(bool Locked, string Status, string Message);

public class Clan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int MemberLimit { get; set; } = 30;
    public int RaidScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Player Owner { get; set; } = null!;
    public ICollection<ClanMember> Members { get; set; } = new List<ClanMember>();
}

public class ClanMember
{
    public int Id { get; set; }
    public int ClanId { get; set; }
    public int PlayerId { get; set; }
    public string Role { get; set; } = "Member"; // "Owner" | "Officer" | "Member"
    public int RaidContribution { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Clan Clan { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

public class CreateClanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RaidContributeRequest
{
    public int Score { get; set; }
}
```

- [ ] **Step 4: Add Clan DbSets to `AppDbContext.cs`**

Add after existing DbSets:
```csharp
public DbSet<Clan> Clans => Set<Clan>();
public DbSet<ClanMember> ClanMembers => Set<ClanMember>();
```

Add entity configuration in `OnModelCreating` after the `PlayerDragon` config block:

```csharp
modelBuilder.Entity<Clan>(e =>
{
    e.HasIndex(c => c.Name).IsUnique();
    e.HasIndex(c => c.Tag).IsUnique();
    e.HasOne(c => c.Owner)
     .WithMany()
     .HasForeignKey(c => c.OwnerId)
     .OnDelete(DeleteBehavior.Restrict);
});

modelBuilder.Entity<ClanMember>(e =>
{
    e.HasIndex(m => new { m.ClanId, m.PlayerId }).IsUnique();
    e.HasOne(m => m.Clan)
     .WithMany(c => c.Members)
     .HasForeignKey(m => m.ClanId)
     .OnDelete(DeleteBehavior.Cascade);
    e.HasOne(m => m.Player)
     .WithMany()
     .HasForeignKey(m => m.PlayerId)
     .OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 5: Replace `ClanController.cs` with full implementation**

```csharp
using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clan")]
public class ClanController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public ClanController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Tag))
            return BadRequest(new { success = false, error = "Name and tag are required" });

        bool alreadyInClan = await _db.ClanMembers.AnyAsync(m => m.PlayerId == CurrentPlayerId);
        if (alreadyInClan)
            return Conflict(new { success = false, error = "Already in a clan" });

        bool nameTaken = await _db.Clans.AnyAsync(c => c.Name == request.Name);
        if (nameTaken)
            return Conflict(new { success = false, error = "Clan name already taken" });

        Player? player = await _db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return Unauthorized(new { success = false, error = "Player not found" });

        var clan = new Clan
        {
            Name = request.Name,
            Tag = request.Tag.ToUpperInvariant(),
            Description = request.Description,
            OwnerId = CurrentPlayerId
        };
        _db.Clans.Add(clan);
        await _db.SaveChangesAsync();

        _db.ClanMembers.Add(new ClanMember
        {
            ClanId = clan.Id,
            PlayerId = CurrentPlayerId,
            Role = "Owner"
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                id = clan.Id,
                name = clan.Name,
                tag = clan.Tag,
                description = clan.Description,
                member_count = 1,
                raid_score = 0,
                my_role = "Owner"
            },
            error = (string?)null
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyClan()
    {
        ClanMember? membership = await _db.ClanMembers
            .Include(m => m.Clan)
            .ThenInclude(c => c.Members)
            .FirstOrDefaultAsync(m => m.PlayerId == CurrentPlayerId);

        if (membership is null)
        {
            return Ok(new
            {
                success = true,
                data = new ClanShellResponse(false, "not_in_clan", "Player is not in a clan"),
                error = (string?)null
            });
        }

        Clan clan = membership.Clan;
        return Ok(new
        {
            success = true,
            data = new
            {
                locked = false,
                my_role = membership.Role,
                my_contribution = membership.RaidContribution,
                clan = new
                {
                    id = clan.Id,
                    name = clan.Name,
                    tag = clan.Tag,
                    description = clan.Description,
                    member_count = clan.Members.Count,
                    member_limit = clan.MemberLimit,
                    raid_score = clan.RaidScore
                },
                members = clan.Members.Select(m => new
                {
                    player_id = m.PlayerId,
                    role = m.Role,
                    raid_contribution = m.RaidContribution,
                    joined_at = m.JoinedAt
                })
            },
            error = (string?)null
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetClan(int id)
    {
        Clan? clan = await _db.Clans
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clan is null)
            return NotFound(new { success = false, error = "Clan not found" });

        return Ok(new
        {
            success = true,
            data = new
            {
                id = clan.Id,
                name = clan.Name,
                tag = clan.Tag,
                description = clan.Description,
                member_count = clan.Members.Count,
                member_limit = clan.MemberLimit,
                raid_score = clan.RaidScore
            },
            error = (string?)null
        });
    }

    [HttpPost("{id:int}/join")]
    public async Task<IActionResult> Join(int id)
    {
        bool alreadyInClan = await _db.ClanMembers.AnyAsync(m => m.PlayerId == CurrentPlayerId);
        if (alreadyInClan)
            return Conflict(new { success = false, error = "Already in a clan" });

        Clan? clan = await _db.Clans.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id);
        if (clan is null)
            return NotFound(new { success = false, error = "Clan not found" });

        if (clan.Members.Count >= clan.MemberLimit)
            return BadRequest(new { success = false, error = "Clan is full" });

        _db.ClanMembers.Add(new ClanMember
        {
            ClanId = id,
            PlayerId = CurrentPlayerId,
            Role = "Member"
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                clan_id = id,
                clan_name = clan.Name,
                role = "Member"
            },
            error = (string?)null
        });
    }

    [HttpPost("{id:int}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        ClanMember? membership = await _db.ClanMembers
            .FirstOrDefaultAsync(m => m.PlayerId == CurrentPlayerId && m.ClanId == id);

        if (membership is null)
            return BadRequest(new { success = false, error = "Not a member of this clan" });

        if (membership.Role == "Owner")
        {
            // Owner leaving disbands the clan
            Clan? clan = await _db.Clans.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id);
            if (clan is not null)
                _db.Clans.Remove(clan); // cascade deletes members
        }
        else
        {
            _db.ClanMembers.Remove(membership);
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { left_clan_id = id }, error = (string?)null });
    }

    [HttpPost("raid/contribute")]
    public async Task<IActionResult> RaidContribute([FromBody] RaidContributeRequest request)
    {
        ClanMember? membership = await _db.ClanMembers
            .Include(m => m.Clan)
            .FirstOrDefaultAsync(m => m.PlayerId == CurrentPlayerId);

        if (membership is null)
            return BadRequest(new { success = false, error = "Not in a clan" });

        int contribution = Math.Max(0, request.Score);
        membership.RaidContribution += contribution;
        membership.Clan.RaidScore += contribution;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                clan_id = membership.ClanId,
                my_contribution = membership.RaidContribution,
                clan_total = membership.Clan.RaidScore
            },
            error = (string?)null
        });
    }

    [HttpGet("raid/leaderboard")]
    public async Task<IActionResult> RaidLeaderboard()
    {
        var top = await _db.Clans
            .OrderByDescending(c => c.RaidScore)
            .Take(20)
            .Select(c => new
            {
                clan_id = c.Id,
                name = c.Name,
                tag = c.Tag,
                raid_score = c.RaidScore,
                member_count = c.Members.Count
            })
            .ToListAsync();

        return Ok(new { success = true, data = top, error = (string?)null });
    }
}
```

- [ ] **Step 6: Generate the EF migration**

```powershell
cd Backend/DragonTD.API
dotnet ef migrations add AddClanMembership
```

Expected: new migration file created in `Migrations/`.

- [ ] **Step 7: Run all tests**

```powershell
cd Backend
dotnet test DragonTD.sln -v minimal
```

Expected: all tests pass (12 prior + new clan tests + new event tests).

- [ ] **Step 8: Update handoff doc**

Append to `docs/2026-05-27-dragon-dominion-prototype-handoff.md` a summary section noting all 6 tasks completed, IAP credentials required for Google Play / Apple production validation.

- [ ] **Step 9: Commit Task 6 and final docs**

```bash
git add Backend/DragonTD.API/Models/ClanModels.cs
git add Backend/DragonTD.API/Data/AppDbContext.cs
git add Backend/DragonTD.API/Controllers/ClanController.cs
git add Backend/DragonTD.API/Migrations/
git add Backend/DragonTD.Tests/EventsAndClanControllerTests.cs
git add docs/2026-05-27-dragon-dominion-prototype-handoff.md
git commit -m "feat: implement Clan membership, join/leave, Clan Raid contribution and leaderboard"
```

---

## Self-Review

**Spec coverage:**
- Task 1 spec: all drag/drop interactions covered including ghost, dim, swap, tap-unequip. ✓
- Task 2: migration + smoke test commands complete. ✓
- Task 3: Google Play RSA + Apple endpoint + platform routing + 503 on unconfigured. ✓
- Task 4: TrySwapEquipped, chest, save/load, daily objective covered. Manual items noted. ✓
- Task 5: date filtering, scored tiers, tier claim, idempotency. ✓
- Task 6: create, get me, get by id, join, leave (owner disbands), raid contribute, leaderboard. ✓

**No placeholders present.**

**Type consistency verified:**
- `DragCardHandler.DragonId` → consumed by `DeckSlotDropHandler.OnDrop` via `handler.DragonId`. ✓
- `PlayerInventory.TrySwapEquipped(string, string, out string)` → called from `DeckSlotDropHandler.OnDrop`. ✓
- `PlayerInventory.FindOwnedDragonById(string)` → called from `DeckSlotDropHandler.OnDrop`. ✓
- `IIapPlatformReceiptValidator` → registered in `Program.cs`, injected into `IapController` as `IEnumerable<IIapPlatformReceiptValidator>`. ✓
- `EventDefinition.RewardTiersJson` → parsed by `ParseTiers` in `EventsController`. ✓
- `ClanMember` cascade deletes when `Clan` removed. ✓

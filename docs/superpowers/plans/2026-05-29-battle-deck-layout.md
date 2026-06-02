# Battle Deck Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the cramped Dragons menu with a Raid Rush-inspired Battle Deck layout for equipped dragons, owned dragons, and future inventory tabs.

**Architecture:** Keep the existing `ProfileProgressionPanel` runtime UI pattern for this prototype pass, but reorganize Dragons mode into three zones: equipped deck slots, tab bar, and collection grid/detail actions. Profile mode remains unchanged.

**Tech Stack:** Unity UI (`Text`, `Image`, `Button`, `RectTransform`), C#, existing `PlayerInventory`, `DragonInstance`, `DragonRoleUtility`, and runtime-created controls.

---

### Task 1: Add Battle Deck Runtime Controls

**Files:**
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

- [x] **Step 1: Add deck layout fields**

Add private fields for deck title, tab buttons, deck slot container, collection grid container, selected detail strip, and active tab enum.

- [x] **Step 2: Add setup calls**

In `OnEnable`, call the new control creation methods before `RuntimeFontScaler.Apply(gameObject)`.

- [x] **Step 3: Keep existing controls reusable**

Reuse existing `_dragonButtonContainer` as the owned collection grid container and existing action buttons for equip/level/train/evolve.

### Task 2: Convert Dragons Mode Layout

**Files:**
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

- [x] **Step 1: Update `ApplyLayout` for Dragons mode**

Set Dragons mode layout to:

- deck title centered near top
- summon/ticket text under title
- equipped deck slots across upper-middle
- tab buttons below equipped slots
- owned dragon grid below tabs
- selected dragon detail/actions in lower strip

- [x] **Step 2: Hide old portrait-first layout**

Disable the large selected portrait/caption in Dragons mode for this pass. Keep detail text and action buttons visible in the bottom detail strip.

- [x] **Step 3: Keep Profile mode intact**

Only change positions/visibility while `_panelMode == PanelMode.Dragons`; leave Profile mode controls readable.

### Task 3: Build Equipped Dragon Slots

**Files:**
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

- [x] **Step 1: Render six equipped slots**

Create six deck slot buttons/cards in `_deckSlotContainer`.

- [x] **Step 2: Fill slots from `inventory.GetBattleDragons()`**

Each filled slot should show:

- slot number
- dragon display name
- level
- rarity
- element
- short role tags

Empty slots should show `Empty Slot`.

- [x] **Step 3: Selecting an equipped slot selects that dragon**

Clicking a filled equipped slot should update `_selectedIndex` to that owned dragon and refresh the panel.

### Task 4: Add Tabs

**Files:**
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

- [x] **Step 1: Add tab enum**

Add `DeckTab` with `Dragons`, `Skills`, `Parts`, `Items`.

- [x] **Step 2: Wire tab buttons**

Tabs should set the current tab and refresh. `Dragons` shows owned dragons. Other tabs show clean placeholder cards.

- [x] **Step 3: Visual selected state**

Selected tab should be gold/purple accented. Inactive tabs should be blue.

### Task 5: Convert Owned Dragon List To Card Grid

**Files:**
- Modify: `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs`

- [x] **Step 1: Replace row labels with cards**

Update `RefreshDragonButtons` / card creation so owned dragons render as grid cards instead of full-width text rows.

- [x] **Step 2: Card content**

Each card should show:

- equipped marker
- display name
- level
- rarity
- element
- short role tags

- [x] **Step 3: Rarity coloring**

Use existing rarity colors as the card background or accent.

### Task 6: Verification And Docs

**Files:**
- Modify: `docs/2026-05-27-dragon-dominion-prototype-handoff.md`

- [ ] **Step 1: Run Unity batchmode compile**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.16f1\Editor\Unity.exe' -batchmode -quit -projectPath 'D:\DragonTD\DragonTD\Unity' -logFile 'D:\DragonTD\DragonTD\unity-battle-deck-layout.log'
```

Expected: no `error CS` entries and batchmode exits successfully.

- [x] **Step 2: Update handoff doc**

Add a short entry describing the Battle Deck layout pass and what remains for manual visual testing.

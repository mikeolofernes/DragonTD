# Portrait-First Card Design Spec

Date: 2026-05-29

## Summary

Replace all placeholder-colored dragon UI surfaces with portrait-first card designs.
Three surfaces: battle HUD placement cards, Dragons-menu deck slots, and the profile panel portrait + idle video area.

## Element Color Palette

Used for glow border, top accent bar, and name tint throughout all surfaces.

| Element | Hex | Usage |
|---------|-----|-------|
| Lightning | `#a855f7` | Voltaris |
| Ice | `#60a5fa` | Frostfang, Tideclaw |
| Fire | `#fb923c` | Magmaclaw, Emberveil |
| Earth | `#84cc16` | Stonehide |
| Light | `#eab308` | Celestara |
| Shadow | `#7c3aed` | Shadowfang |
| Wind | `#22d3ee` | Zephyrwing |
| Water | `#38bdf8` | Tideclaw (alt: use Ice) |

Card background per element: element color darkened to ~8% opacity against `#080b12` base.

Rarity pill colors: Common `#6b7280` · Uncommon `#22c55e` · Rare `#3b82f6` · Epic `#a855f7` · Legendary `#eab308` · Mythic `#ef4444`.

## Surface 1: Battle HUD Placement Cards

**File:** `Unity/Assets/Scripts/UI/DragonPlacementCard.cs`

### Layout (per card)

- **Width:** 72px · **Height:** 110px · **Corner radius:** 8px
- **Portrait area:** top 80px — `Image` component, portrait sprite, `PreserveAspect = true`, `maskable`
- **Background behind portrait:** element-dark gradient (top of card to portrait bottom)
- **Top accent bar:** 3px height, full width, element color linear gradient (left→center→left)
- **Mana cost badge:** top-right corner, `TextMeshPro` or `Text`, dark BG pill, element-color border, shows `{manaCost}`
- **Name bar:** bottom 30px — dark BG `#0a0c14` at 95% opacity, dragon `displayName` in 9px white bold, element dot (6px circle) + element name in 7px below name
- **Border / glow:** 2px solid element color + `Shadow` component or outline shader emitting element color glow

### States

| State | Appearance |
|-------|-----------|
| Normal | Full opacity, element glow |
| Selected (placement mode) | Bright element glow pulse, scale 1.05 |
| Wave active (locked) | `CanvasGroup.alpha = 0.4`, no interaction |
| No portrait | Show `primaryColor` solid fill instead of portrait |

### Implementation notes

- Portrait `Image` uses the `Sprite` already assigned to `DragonDefinition.visualData.portrait` after SceneBootstrapper runs
- Fallback: if `portrait == null`, use `primaryColor` solid fill (existing behavior preserved)
- Card size is fixed; portrait is cropped/fit via `Image.Type = Simple + PreserveAspect`

## Surface 2: Dragons Menu Deck Slots

**File:** `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` → `CreateDeckSlotButton`

### Layout (per slot)

- **Width:** flex (1/6 of deck container) · **Height:** 88px · **Corner radius:** 8px
- **Portrait area:** top 64px — `Image` component, portrait sprite, `PreserveAspect = true`
- **Background:** element-dark gradient if occupied; `#0a0c14` with `2px dashed #2a2a3a` border if empty
- **Top accent bar:** 3px, element color (occupied only)
- **Element glow box-shadow:** `0 0 8px elementColor50%, 0 0 0 2px elementColor` (occupied)
- **Name bar:** bottom 24px — dragon name in 8px bold, `Lv X` in 7px muted
- **Empty slot:** centered `+` text in `#2a2a4a`, `Drop here` label in 7px below
- **IDropHandler:** already implemented — DropHandler already exists on this slot

### States

| State | Appearance |
|-------|-----------|
| Occupied | Element glow + portrait + name |
| Empty | Dashed border, `+` icon, `Drop here` hint |
| Drag-over (valid) | Brighter border pulse |

## Surface 3: Profile Panel — Portrait + Idle Video

**File:** `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` → `EnsureDragonMenuControls` and new `EnsureIdleVideoPlayer`

### Layout

- **Portrait container:** 180×180px `RectTransform`, element glow + 2px border, corner radius 10px
- **Content inside container:**
  - Top 180px: `RawImage` for VideoPlayer output (when video available) **OR** `Image` for static portrait (fallback)
  - Top accent bar: 4px height, element color gradient
  - Bottom-right badge: `▶ IDLE` pill (8px text) when video is playing; hidden when static portrait
- **Name bar** (below container, not inside):
  - Dragon `displayName` 14px bold, element-colored
  - Rarity pill + element name row

### VideoPlayer setup

- One `VideoPlayer` component on the portrait container `GameObject`
- `renderMode = VideoRenderMode.RenderTexture`
- `RenderTexture` size: 256×256, Format `ARGB32`
- `RawImage` fills the portrait container, assigned the `RenderTexture`
- `VideoPlayer.url` = `Application.dataPath + "/Art/Dragons/{dragonId}/idle_anim.mp4"`
- `VideoPlayer.isLooping = true`, `VideoPlayer.playOnAwake = false`
- Called via `PlayIdleVideo(DragonInstance dragon)` — loads URL, calls `Play()`
- If file not found: hide `RawImage`, show static `Image` with portrait sprite
- On dragon selection change: stop current video, call `PlayIdleVideo(newDragon)`

### Static portrait fallback

When `idle_anim.mp4` does not exist for a dragon, show the `portrait.png` as a static `Image` instead of the `RawImage`. The `▶ IDLE` badge is hidden.

## Files Changed

| File | Change |
|------|--------|
| `Unity/Assets/Scripts/UI/DragonPlacementCard.cs` | Rebuild card visual layout: element glow border, portrait Image, accent bar, mana badge, name bar |
| `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` | Replace portrait `Image` with VideoPlayer+RawImage stack; update `CreateDeckSlotButton` with element colors and portrait; add `PlayIdleVideo`, `StopIdleVideo`, `EnsureIdleVideoPlayer` |
| `Unity/Assets/Scripts/Core/DragonColorUtility.cs` | **New file** — static helper: `GetElementColor(DragonElement)` and `GetRarityColor(DragonRarity)` returning `Color` |

## Out of Scope

- Shader-based glow (use Unity UI `Shadow` or `Outline` component as approximation)
- Animated idle in the battle scene towers (Phase 3)
- Portrait art for emberveil_008, tideclaw_009, zephyrwing_010 (pending art creation)
- Map/environment art (separate spec)

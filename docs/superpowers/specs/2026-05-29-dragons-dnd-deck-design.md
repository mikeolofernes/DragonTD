# Dragons Menu — Drag-and-Drop Deck Design

Date: 2026-05-29

## Summary

Replace the Prev/Next/Equip/Unequip button controls in the Dragons menu with a drag-and-drop deck system. Deck slot order is irrelevant; the only operations are equip, unequip, and swap.

## Removed Controls

- `Prev` / `Next` navigation buttons (dragon selection now tap-only in the grid)
- `Equip` / `Unequip` button in the detail strip

## Interaction Model

### Collection Grid (lower section)

- **Tap** — selects the dragon; detail strip updates. No change from current behavior.
- **Drag start** — spawns a ghost card (semi-transparent clone) that follows the pointer. The source tile dims while dragging.
- Already-equipped cards remain visible in the grid but are dimmed (`alpha 0.4`). They can be dragged to swap into a different slot.

### Deck Slots (top row, 6 slots)

| Action | Result |
|--------|--------|
| Drag unequipped card → empty slot | Equip. Card dims in grid. Slot fills. |
| Drag unequipped card → filled slot | Swap: unequip the existing dragon, equip the dragged dragon. |
| Drag equipped (dimmed) card → empty slot | Move: unequip from old slot, equip into new slot. |
| Drag equipped (dimmed) card → filled slot | Swap the two equipped dragons between slots. Net effect: same two dragons equipped, positions irrelevant. |
| Tap filled slot | Unequip. Slot clears. Grid card undims. |
| Tap empty slot | No-op. |

### Ghost Card

- Cloned from the source card UI, scaled to same size.
- Semi-transparent (`alpha 0.6`), rendered above all other UI (canvas sort order top).
- Follows pointer exactly.
- Destroyed on drop (success or cancel).
- Dragging off all valid drop targets and releasing cancels the drag; source card returns to normal alpha.

## Unity Implementation

Use Unity EventSystem drag interfaces on card tiles and drop handler on deck slots:

- `IBeginDragHandler.OnBeginDrag` — instantiate ghost, dim source
- `IDragHandler.OnDrag` — move ghost to pointer position
- `IEndDragHandler.OnEndDrag` — destroy ghost, restore source alpha if drop was cancelled
- `IDropHandler.OnDrop` — deck slot receives the dragged card reference, executes equip/swap/move via `PlayerInventory`

Works identically on touch (Unity maps touch to pointer events) and mouse. No platform-specific code needed.

## Detail Strip

Unchanged. Level Up, Train Bond, Evolve remain. These operate on the currently selected dragon (tap-selected from grid or tap-selected from deck slot).

Tapping a filled deck slot unequips AND selects that dragon for the detail strip.

## Data Layer

`PlayerInventory.EquippedDragonIds` (existing `List<string>`, max 6) is the source of truth.

New methods needed:

```csharp
// Equip into next available slot (existing: TryToggleEquipDragon covers this)
// Swap two dragons between equipped and unequipped state
bool TrySwapEquipped(string incomingDragonId, string existingDragonId);
// Unequip by dragon ID (existing: TryToggleEquipDragon covers this)
```

`TrySwapEquipped` removes `existingDragonId` from `EquippedDragonIds`, adds `incomingDragonId`, saves. Returns false only if either ID is not found in owned dragons.

## Files Changed

- `Unity/Assets/Scripts/UI/ProfileProgressionPanel.cs` — remove Prev/Next/Equip/Unequip, add drag handlers on card tiles and drop handlers on deck slots
- `Unity/Assets/Scripts/Core/PlayerInventory.cs` — add `TrySwapEquipped`
- `Unity/Assets/Prefabs/UI/ProfileProgressionPanel.prefab` — updated if regenerated

## Out of Scope

- Animated slot fill transitions
- Drag-to-reorder within the deck (order is irrelevant)
- Multi-select drag

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

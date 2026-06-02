using System.Collections.Generic;
using UnityEngine;
using DragonTD.Dragons;
using DragonTD.Core;

namespace DragonTD.UI
{
    // Scrollable panel listing equipped battle dragons as placement cards.
    public class DragonCollectionPanel : MonoBehaviour
    {
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardPrefab;

        private void Start()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += Refresh;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged -= Refresh;
        }

        private void Refresh() => Populate(PlayerInventory.Instance.GetBattleDragons());

        public void Populate(List<DragonInstance> inventory)
        {
            if (_cardContainer == null || _cardPrefab == null || inventory == null)
                return;

            foreach (Transform child in _cardContainer)
                Destroy(child.gameObject);

            foreach (DragonInstance dragon in inventory)
            {
                GameObject cardGO = Instantiate(_cardPrefab, _cardContainer);
                cardGO.GetComponent<DragonPlacementCard>()?.Setup(dragon);
            }

            GetComponentInParent<ResponsiveBattleUILayout>()?.ApplyLayout();
        }
    }
}

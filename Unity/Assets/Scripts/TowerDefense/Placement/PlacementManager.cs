using UnityEngine;
using DragonTD.Dragons;
using DragonTD.Core;

namespace DragonTD.TowerDefense
{
    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }

        private DragonInstance _selectedDragon;
        private bool _isPlacing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void BeginPlacement(DragonInstance dragon)
        {
            if (_isPlacing)
                CancelPlacement();

            _selectedDragon = dragon;
            _isPlacing = true;
        }

        public void CancelPlacement()
        {
            _isPlacing = false;
            _selectedDragon = null;
        }

        private void Update()
        {
            if (!_isPlacing) return;

            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0f;

                GridTile tile = GridManager.Instance.GetTileAtWorldPos(worldPos);
                if (tile != null && tile.CanPlace() &&
                    ResourceManager.Instance.TrySpendMana(_selectedDragon.Data.ManaCost))
                {
                    PlaceDragon(tile);
                }
            }
        }

        private void PlaceDragon(GridTile tile)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(tile.GridPosition.x, tile.GridPosition.y);
            GameObject dragonGO = Object.Instantiate(_selectedDragon.Data.Prefab, worldPos, Quaternion.identity);

            DragonTower tower = dragonGO.GetComponent<DragonTower>();
            if (tower != null)
            {
                tower.Setup(_selectedDragon);
            }

            tile.SetOccupied(true);
            CancelPlacement();
        }
    }
}

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

            // Support both touch (mobile) and mouse (editor)
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                HandleTapAt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
#endif

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    // Two-finger tap cancels placement on mobile
                    if (Input.touchCount == 2)
                    {
                        CancelPlacement();
                        return;
                    }
                    HandleTapAt(Camera.main.ScreenToWorldPoint(touch.position));
                }
            }
        }

        private void HandleTapAt(Vector3 worldPos)
        {
            worldPos.z = 0f;
            GridTile tile = GridManager.Instance.GetTileAtWorldPos(worldPos);
            if (tile != null && tile.CanPlace() &&
                ResourceManager.Instance.TrySpendMana(_selectedDragon.Definition.manaCost))
            {
                PlaceDragon(tile);
            }
        }

        private void PlaceDragon(GridTile tile)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(tile.GridPosition.x, tile.GridPosition.y);

            GameObject prefab = _selectedDragon.Definition.visualData.hatchlingPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[PlacementManager] No prefab assigned for {_selectedDragon.Definition.displayName}");
                CancelPlacement();
                return;
            }

            GameObject dragonGO = Object.Instantiate(prefab, worldPos, Quaternion.identity);

            DragonTower tower = dragonGO.GetComponent<DragonTower>();
            if (tower != null)
                tower.Setup(_selectedDragon);

            tile.SetOccupied(true);
            CancelPlacement();
        }
    }
}

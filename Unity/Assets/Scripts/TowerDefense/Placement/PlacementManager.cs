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
        private LineRenderer _rangePreview;
        private GridTile _previewTile;

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
            ShowRangePreview(dragon);
        }

        public void CancelPlacement()
        {
            _isPlacing = false;
            _selectedDragon = null;
            ClearTilePreview();
            HideRangePreview();
        }

        private void Update()
        {
            if (!_isPlacing) return;
            Vector3 pointerPosition = GetPointerWorldPosition();
            UpdateRangePreview(pointerPosition);
            UpdateTilePreview(pointerPosition);

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
            UpdateRangePreview(worldPos);
            GridTile tile = GridManager.Instance.GetTileAtWorldPos(worldPos);
            if (tile != null && tile.CanPlace() &&
                ResourceManager.Instance.TrySpendMana(_selectedDragon.Definition.manaCost))
            {
                PlaceDragon(tile);
            }
            else
            {
                if (tile == null)
                    GameManager.Instance?.ShowBattleMessage("Select a buildable tile");
                else if (!tile.CanPlace())
                    GameManager.Instance?.ShowBattleMessage("Cannot place on path or occupied tile");
                else
                    GameManager.Instance?.ShowBattleMessage($"Need {_selectedDragon.Definition.manaCost} MP");
                UpdateTilePreview(worldPos);
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
                tower.Setup(_selectedDragon, tile, _selectedDragon.Definition.manaCost);

            tile.SetOccupied(true);
            BattleStatsTracker.Instance?.RecordTowerPlaced(_selectedDragon.Definition.manaCost);
            GameManager.Instance?.ShowBattleMessage($"{_selectedDragon.Definition.displayName} deployed");
            CancelPlacement();
        }

        private void ShowRangePreview(DragonInstance dragon)
        {
            if (_rangePreview == null)
            {
                GameObject preview = new GameObject("PlacementRangePreview");
                _rangePreview = preview.AddComponent<LineRenderer>();
                _rangePreview.loop = true;
                _rangePreview.useWorldSpace = true;
                _rangePreview.positionCount = 64;
                _rangePreview.startWidth = 0.045f;
                _rangePreview.endWidth = 0.045f;
                _rangePreview.sortingOrder = 20;
                _rangePreview.material = new Material(Shader.Find("Sprites/Default"));
            }

            Color color = dragon.Definition.visualData.primaryColor;
            color.a = 0.9f;
            _rangePreview.startColor = color;
            _rangePreview.endColor = color;
            _rangePreview.enabled = true;
            UpdateRangePreview(GetPointerWorldPosition());
        }

        private void HideRangePreview()
        {
            if (_rangePreview != null)
                _rangePreview.enabled = false;
        }

        private void UpdateTilePreview(Vector3 worldPos)
        {
            if (GridManager.Instance == null) return;

            GridTile tile = GridManager.Instance.GetTileAtWorldPos(worldPos);
            if (_previewTile != null && _previewTile != tile)
                _previewTile.SetPlacementPreview(false, false);

            _previewTile = tile;
            if (_previewTile != null)
                _previewTile.SetPlacementPreview(true, _previewTile.CanPlace());
        }

        private void ClearTilePreview()
        {
            if (_previewTile != null)
                _previewTile.SetPlacementPreview(false, false);
            _previewTile = null;
        }

        private void UpdateRangePreview(Vector3 center)
        {
            if (_rangePreview == null || _selectedDragon == null) return;

            center.z = 0f;
            float range = _selectedDragon.Definition.NormalAttack != null
                ? _selectedDragon.Definition.NormalAttack.range
                : _selectedDragon.Definition.baseStats.range;

            for (int i = 0; i < _rangePreview.positionCount; i++)
            {
                float radians = i / (float)_rangePreview.positionCount * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * range;
                _rangePreview.SetPosition(i, point);
            }
        }

        private Vector3 GetPointerWorldPosition()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;
#if UNITY_EDITOR || UNITY_STANDALONE
            Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
            mouse.z = 0f;
            return mouse;
#else
            if (Input.touchCount > 0)
            {
                Vector3 touch = cam.ScreenToWorldPoint(Input.GetTouch(0).position);
                touch.z = 0f;
                return touch;
            }
            return Vector3.zero;
#endif
        }
    }
}

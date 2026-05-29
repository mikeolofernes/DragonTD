using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [SerializeField] private GridTile _tilePrefab;
        [SerializeField] private int _width = 20;
        [SerializeField] private int _height = 12;
        [SerializeField] private Vector3 _originPosition = Vector3.zero;
        [SerializeField] private Vector2Int[] _pathTiles;
        [SerializeField] private Vector2Int[] _highGroundTiles;
        [SerializeField] private Vector2Int[] _manaCrystalTiles;
        [SerializeField] private Vector2Int[] _scorchedTiles;
        [SerializeField] private Vector2Int[] _frostTiles;

        private GridTile[,] _grid;

        public int Width => _width;
        public int Height => _height;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            BuildGrid();
        }

        private void BuildGrid()
        {
            if (_tilePrefab == null)
            {
                Debug.LogWarning("[GridManager] No _tilePrefab assigned — grid will not be built.");
                return;
            }
            _grid = new GridTile[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 worldPos = GridToWorld(x, y);
                    GridTile tile = Instantiate(_tilePrefab, worldPos, Quaternion.identity, transform);
                    TileType tileType = IsPathTile(x, y) ? TileType.Path : TileType.Buildable;
                    tile.Initialize(new Vector2Int(x, y), tileType);
                    if (tileType == TileType.Buildable)
                        tile.SetBonus(GetBonusForTile(x, y));
                    _grid[x, y] = tile;
                }
            }
        }

        public GridTile GetTile(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return null;
            return _grid[x, y];
        }

        public GridTile GetTileAtWorldPos(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - _originPosition.x) + 0.5f);
            int y = Mathf.FloorToInt((worldPos.y - _originPosition.y) + 0.5f);
            return GetTile(x, y);
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(
                _originPosition.x + x,
                _originPosition.y + y,
                _originPosition.z
            );
        }

        public bool TryGetBuildableTile(Vector3 worldPos, out GridTile tile)
        {
            tile = GetTileAtWorldPos(worldPos);
            return tile != null && tile.CanPlace();
        }

        public void ClearOccupancy()
        {
            if (_grid == null) return;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_grid[x, y] != null)
                        _grid[x, y].SetOccupied(false);
                }
            }
        }

        private bool IsPathTile(int x, int y)
        {
            if (_pathTiles == null) return false;
            foreach (var p in _pathTiles)
                if (p.x == x && p.y == y) return true;
            return false;
        }

        private TileBonusType GetBonusForTile(int x, int y)
        {
            if (ContainsTile(_highGroundTiles, x, y)) return TileBonusType.HighGround;
            if (ContainsTile(_manaCrystalTiles, x, y)) return TileBonusType.ManaCrystal;
            if (ContainsTile(_scorchedTiles, x, y)) return TileBonusType.Scorched;
            if (ContainsTile(_frostTiles, x, y)) return TileBonusType.Frost;

            return TileBonusType.None;
        }

        private static bool ContainsTile(Vector2Int[] tiles, int x, int y)
        {
            if (tiles == null || tiles.Length == 0) return false;
            foreach (var tile in tiles)
                if (tile.x == x && tile.y == y) return true;
            return false;
        }
    }
}

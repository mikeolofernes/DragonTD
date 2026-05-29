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

        [Header("Path Auto-Tile Sprites")]
        [SerializeField] private Sprite _pathStraightH;
        [SerializeField] private Sprite _pathStraightV;
        [SerializeField] private Sprite _pathCornerTL;
        [SerializeField] private Sprite _pathCornerTR;
        [SerializeField] private Sprite _pathCornerBL;
        [SerializeField] private Sprite _pathCornerBR;

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
            ApplyAutoTiling();
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

        private void ApplyAutoTiling()
        {
            if (_grid == null) return;
            bool hasAnyVariant = _pathStraightH != null || _pathStraightV != null ||
                                 _pathCornerTL  != null || _pathCornerTR  != null ||
                                 _pathCornerBL  != null || _pathCornerBR  != null;
            if (!hasAnyVariant) return; // no sprites assigned — keep prefab default

            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                GridTile tile = _grid[x, y];
                if (tile == null || tile.TileType != TileType.Path) continue;

                bool left  = x > 0          && _grid[x-1, y]?.TileType == TileType.Path;
                bool right = x < _width-1   && _grid[x+1, y]?.TileType == TileType.Path;
                bool up    = y < _height-1  && _grid[x, y+1]?.TileType == TileType.Path;
                bool down  = y > 0          && _grid[x, y-1]?.TileType == TileType.Path;

                int mask = (left ? 1 : 0) | (right ? 2 : 0) | (up ? 4 : 0) | (down ? 8 : 0);
                Sprite s = ResolveAutoTileSprite(mask);
                if (s != null) tile.SetAutoTileSprite(s);
            }
        }

        private Sprite ResolveAutoTileSprite(int mask)
        {
            return mask switch
            {
                3  => _pathStraightH ?? _pathStraightV,
                12 => _pathStraightV ?? _pathStraightH,
                10 => _pathCornerTL  ?? _pathStraightH,
                9  => _pathCornerTR  ?? _pathStraightH,
                6  => _pathCornerBL  ?? _pathStraightH,
                5  => _pathCornerBR  ?? _pathStraightH,
                // T-junctions: fall back to the dominant axis sprite
                7  => _pathStraightH ?? _pathStraightV, // left+right+up → T up
                11 => _pathStraightH ?? _pathStraightV, // left+right+down → T down
                14 => _pathStraightV ?? _pathStraightH, // up+right+down → T right
                13 => _pathStraightV ?? _pathStraightH, // up+left+down → T left
                15 => _pathStraightH ?? _pathStraightV, // crossroad
                // End caps: single connection
                1  or 2  => _pathStraightH ?? _pathStraightV,
                4  or 8  => _pathStraightV ?? _pathStraightH,
                _  => _pathStraightH ?? _pathStraightV
            };
        }
    }
}

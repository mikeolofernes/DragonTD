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
        [SerializeField] private Sprite _pathFallback;   // generic path — used when specific variant is missing
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

            if (ChapterContent.Active != null && ChapterContent.Active.map != null)
                LoadTilesFromMap(ChapterContent.Active.map);

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
            bool hasAnyVariant = _pathFallback   != null || _pathStraightH != null || _pathStraightV != null ||
                                 _pathCornerTL   != null || _pathCornerTR  != null ||
                                 _pathCornerBL   != null || _pathCornerBR  != null;
            if (!hasAnyVariant) return;

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
            // Fall back chain: specific variant → straight → generic path fallback
            Sprite h = _pathStraightH ?? _pathFallback;
            Sprite v = _pathStraightV ?? _pathFallback;
            return mask switch
            {
                3  => h,
                12 => v,
                10 => _pathCornerTL ?? h,
                9  => _pathCornerTR ?? h,
                6  => _pathCornerBL ?? h,
                5  => _pathCornerBR ?? h,
                7  or 11 => h,
                14 or 13 => v,
                15 => h,
                1  or 2  => h,
                4  or 8  => v,
                _  => h ?? v
            };
        }

        private void LoadTilesFromMap(MapDefinition map)
        {
            var path = new System.Collections.Generic.List<Vector2Int>();
            var high = new System.Collections.Generic.List<Vector2Int>();
            var mana = new System.Collections.Generic.List<Vector2Int>();
            var scorch = new System.Collections.Generic.List<Vector2Int>();
            var frost = new System.Collections.Generic.List<Vector2Int>();

            for (int y = 0; y < MapDefinition.Rows; y++)
            for (int x = 0; x < MapDefinition.Cols; x++)
            {
                if (map.GetTileType(x, y) == TileType.Path)
                    path.Add(new Vector2Int(x, y));
                switch (map.GetBonusType(x, y))
                {
                    case TileBonusType.HighGround:  high.Add(new Vector2Int(x, y)); break;
                    case TileBonusType.ManaCrystal: mana.Add(new Vector2Int(x, y)); break;
                    case TileBonusType.Scorched:    scorch.Add(new Vector2Int(x, y)); break;
                    case TileBonusType.Frost:       frost.Add(new Vector2Int(x, y)); break;
                }
            }

            _pathTiles = path.ToArray();
            _highGroundTiles = high.ToArray();
            _manaCrystalTiles = mana.ToArray();
            _scorchedTiles = scorch.ToArray();
            _frostTiles = frost.ToArray();
        }
    }
}

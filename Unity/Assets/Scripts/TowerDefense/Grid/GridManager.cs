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
            _grid = new GridTile[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Vector3 worldPos = GridToWorld(x, y);
                    GridTile tile = Instantiate(_tilePrefab, worldPos, Quaternion.identity, transform);
                    tile.Initialize(new Vector2Int(x, y), TileType.Buildable);
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
    }
}

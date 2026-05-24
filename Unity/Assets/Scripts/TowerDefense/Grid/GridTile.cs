using UnityEngine;

namespace DragonTD.TowerDefense
{
    public enum TileType { Buildable, Path, Blocked, Base }

    public class GridTile : MonoBehaviour
    {
        [SerializeField] private TileType _tileType = TileType.Buildable;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _buildableColor = Color.green;
        [SerializeField] private Color _pathColor = Color.gray;
        [SerializeField] private Color _blockedColor = Color.red;

        public TileType TileType => _tileType;
        public bool IsOccupied { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        public void Initialize(Vector2Int pos, TileType type)
        {
            GridPosition = pos;
            _tileType = type;
            UpdateVisual();
        }

        public bool CanPlace() => _tileType == TileType.Buildable && !IsOccupied;
        public void SetOccupied(bool occupied) { IsOccupied = occupied; UpdateVisual(); }

        private void UpdateVisual()
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.color = _tileType switch
            {
                TileType.Path => _pathColor,
                TileType.Blocked => _blockedColor,
                _ => _buildableColor
            };
        }

        private void OnMouseEnter() { if (CanPlace()) _spriteRenderer.color = Color.yellow; }
        private void OnMouseExit() { UpdateVisual(); }
    }
}

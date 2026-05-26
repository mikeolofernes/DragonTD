using UnityEngine;

namespace DragonTD.TowerDefense
{
    public enum TileType { Buildable, Path, Blocked, Base }

    public class GridTile : MonoBehaviour
    {
        [SerializeField] private TileType _tileType = TileType.Buildable;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _buildableSprite;
        [SerializeField] private Sprite _pathSprite;
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

            Sprite s = _tileType == TileType.Path
                ? (_pathSprite != null ? _pathSprite : _spriteRenderer.sprite)
                : (_buildableSprite != null ? _buildableSprite : _spriteRenderer.sprite);
            if (s != null) _spriteRenderer.sprite = s;

            _spriteRenderer.color = _tileType switch
            {
                TileType.Path    => _pathSprite    != null ? Color.white : _pathColor,
                TileType.Blocked => _blockedColor,
                _                => _buildableSprite != null ? (IsOccupied ? new Color(0.7f,0.7f,0.3f) : Color.white) : _buildableColor
            };
        }

        private void OnMouseEnter() { if (CanPlace()) _spriteRenderer.color = Color.yellow; }
        private void OnMouseExit() { UpdateVisual(); }
    }
}

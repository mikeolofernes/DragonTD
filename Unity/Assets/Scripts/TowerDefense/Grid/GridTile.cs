using UnityEngine;

namespace DragonTD.TowerDefense
{
    public enum TileType { Buildable, Path, Blocked, Base }
    public enum TileBonusType { None, HighGround, ManaCrystal, Scorched, Frost }

    public class GridTile : MonoBehaviour
    {
        [SerializeField] private TileType _tileType = TileType.Buildable;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Sprite _buildableSprite;
        [SerializeField] private Sprite _pathSprite;
        [SerializeField] private Sprite _highGroundSprite;
        [SerializeField] private Sprite _manaCrystalSprite;
        [SerializeField] private Sprite _scorchedSprite;
        [SerializeField] private Sprite _frostSprite;
        [SerializeField] private Color _buildableColor = Color.green;
        [SerializeField] private Color _pathColor = Color.gray;
        [SerializeField] private Color _blockedColor = Color.red;
        [SerializeField] private Color _validPlacementColor = new Color(0.25f, 1f, 0.35f, 1f);
        [SerializeField] private Color _invalidPlacementColor = new Color(1f, 0.18f, 0.12f, 1f);

        public TileType TileType => _tileType;
        public TileBonusType BonusType { get; private set; }
        public string BonusName => BonusType switch
        {
            TileBonusType.HighGround => "High Ground",
            TileBonusType.ManaCrystal => "Mana Crystal",
            TileBonusType.Scorched => "Scorched Tile",
            TileBonusType.Frost => "Frost Tile",
            _ => "No Bonus"
        };
        public string BonusDescription => BonusType switch
        {
            TileBonusType.HighGround => "+1 range",
            TileBonusType.ManaCrystal => "-25% skill cooldown",
            TileBonusType.Scorched => "+20% fire damage",
            TileBonusType.Frost => "+25% slow strength",
            _ => ""
        };
        public bool IsOccupied { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        private bool _isPreviewed;
        private bool _isPreviewValid;
        private TextMesh _bonusLabel;

        public void Initialize(Vector2Int pos, TileType type)
        {
            GridPosition = pos;
            _tileType = type;
            UpdateVisual();
        }

        public void SetBonus(TileBonusType bonusType)
        {
            BonusType = bonusType;
            EnsureBonusLabel();
            UpdateVisual();
        }

        private void EnsureBonusLabel()
        {
            if (BonusType == TileBonusType.None)
            {
                if (_bonusLabel != null)
                    _bonusLabel.gameObject.SetActive(false);
                return;
            }

            if (_bonusLabel == null)
            {
                var labelGO = new GameObject("BonusLabel");
                labelGO.transform.SetParent(transform, false);
                labelGO.transform.localPosition = new Vector3(0f, 0.34f, -0.05f);
                _bonusLabel = labelGO.AddComponent<TextMesh>();
                _bonusLabel.anchor = TextAnchor.MiddleCenter;
                _bonusLabel.alignment = TextAlignment.Center;
                _bonusLabel.characterSize = 0.08f;
                _bonusLabel.fontSize = 24;
                var renderer = labelGO.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sortingOrder = 9;
            }

            _bonusLabel.gameObject.SetActive(true);
            _bonusLabel.text = BonusType switch
            {
                TileBonusType.HighGround => "+R",
                TileBonusType.ManaCrystal => "CD",
                TileBonusType.Scorched => "FIRE",
                TileBonusType.Frost => "SLOW",
                _ => ""
            };
            _bonusLabel.color = Color.black;
        }

        public bool CanPlace() => _tileType == TileType.Buildable && !IsOccupied;
        public void SetOccupied(bool occupied) { IsOccupied = occupied; UpdateVisual(); }

        public void SetPlacementPreview(bool active, bool valid)
        {
            _isPreviewed = active;
            _isPreviewValid = valid;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_spriteRenderer == null) return;

            Sprite s = _tileType == TileType.Path
                ? (_pathSprite != null ? _pathSprite : _spriteRenderer.sprite)
                : (_buildableSprite != null ? _buildableSprite : _spriteRenderer.sprite);
            if (s != null) _spriteRenderer.sprite = s;

            if (_isPreviewed)
            {
                _spriteRenderer.color = _isPreviewValid ? _validPlacementColor : _invalidPlacementColor;
                return;
            }

            if (_tileType == TileType.Buildable && BonusType != TileBonusType.None)
            {
                Sprite bonusSprite = BonusType switch
                {
                    TileBonusType.HighGround  => _highGroundSprite,
                    TileBonusType.ManaCrystal => _manaCrystalSprite,
                    TileBonusType.Scorched    => _scorchedSprite,
                    TileBonusType.Frost       => _frostSprite,
                    _                         => null
                };
                if (bonusSprite != null) _spriteRenderer.sprite = bonusSprite;
                _spriteRenderer.color = IsOccupied ? new Color(0.7f, 0.7f, 0.3f, 1f) : Color.white;
                return;
            }

            _spriteRenderer.color = _tileType switch
            {
                TileType.Path    => _pathSprite    != null ? Color.white : _pathColor,
                TileType.Blocked => _blockedColor,
                _                => _buildableSprite != null ? (IsOccupied ? new Color(0.7f,0.7f,0.3f) : Color.white) : _buildableColor
            };
        }

        private void OnMouseEnter()
        {
            if (BonusType != TileBonusType.None && !IsOccupied)
                DragonTD.Core.GameManager.Instance?.ShowBattleMessage($"{BonusName}: {BonusDescription}");
            if (!_isPreviewed && CanPlace()) _spriteRenderer.color = Color.yellow;
        }

        private void OnMouseExit() { UpdateVisual(); }
    }
}

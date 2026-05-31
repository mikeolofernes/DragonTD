using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class WallBase : MonoBehaviour
    {
        public static WallBase Instance { get; private set; }

        [SerializeField] private float _maxHp = 1000f;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public float MaxHp       => _maxHp;
        public float CurrentHp   { get; private set; }
        public float HpPercent   => MaxHp > 0f ? CurrentHp / MaxHp : 0f;
        public bool  IsDestroyed { get; private set; }

        public event System.Action OnWallDestroyed;
        public event System.Action<float> OnHpChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            CurrentHp = _maxHp;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Configure(float maxHp)
        {
            _maxHp      = maxHp;
            CurrentHp   = maxHp;
            IsDestroyed = false;
            UpdateVisual();
        }

        public void TakeDamage(float damage)
        {
            if (IsDestroyed) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - damage);
            OnHpChanged?.Invoke(HpPercent);
            UpdateVisual();
            if (CurrentHp <= 0f)
            {
                IsDestroyed = true;
                OnWallDestroyed?.Invoke();
            }
        }

        private void UpdateVisual()
        {
            if (_spriteRenderer == null) return;
            float t = HpPercent;
            _spriteRenderer.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f, 1f),
                                                new Color(0.9f, 0.55f, 0.1f, 1f), t);
        }

        public static WallBase Create(float wallWorldX, int gridRows, float wallHp)
        {
            var go = new GameObject("WallBase");
            go.transform.position = new Vector3(wallWorldX, (gridRows * 0.5f) - 4f, 0f);

            var tex = new Texture2D(4, 4);
            for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
            tex.Apply();
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.sortingOrder = 3;
            go.transform.localScale = new Vector3(1f, gridRows, 1f);

            var wall = go.AddComponent<WallBase>();
            wall._spriteRenderer = sr;
            wall.Configure(wallHp);
            return wall;
        }
    }
}

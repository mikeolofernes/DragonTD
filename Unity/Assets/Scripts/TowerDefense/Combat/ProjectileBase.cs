using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class ProjectileBase : MonoBehaviour
    {
        private Transform _target;
        private float _damage;
        private float _speed = 10f;
        private Color _impactColor = Color.white;
        private DamageSource _damageSource = DamageSource.Projectile;
        private int _visualLevel = 1;
        private SpriteRenderer _spriteRenderer;
        private TrailRenderer _trailRenderer;

        public void Initialize(Transform target, float damage, float speed = 10f)
        {
            Initialize(target, damage, speed, Color.white);
        }

        public void Initialize(Transform target, float damage, float speed, Color color)
        {
            Initialize(target, damage, speed, color, DamageSource.Projectile);
        }

        public void Initialize(Transform target, float damage, float speed, Color color, DamageSource damageSource)
        {
            Initialize(target, damage, speed, color, damageSource, 1);
        }

        public void Initialize(Transform target, float damage, float speed, Color color, DamageSource damageSource, int visualLevel)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
            _impactColor = color;
            _damageSource = damageSource;
            _visualLevel = Mathf.Clamp(visualLevel, 1, PrototypeBalance.FusedTowerLevel);

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
                _spriteRenderer.sortingOrder = _damageSource == DamageSource.LightningProjectile ? 9 : 7;
                float baseScale = _damageSource == DamageSource.LightningProjectile ? 0.28f : 0.22f;
                transform.localScale = Vector3.one * (baseScale + ((_visualLevel - 1) * 0.08f));
            }

            ConfigureTrail(color);
        }

        private void ConfigureTrail(Color color)
        {
            _trailRenderer = GetComponent<TrailRenderer>();
            if (_trailRenderer == null)
                _trailRenderer = gameObject.AddComponent<TrailRenderer>();

            Color startColor = new Color(color.r, color.g, color.b, 0.8f);
            Color endColor = new Color(color.r, color.g, color.b, 0f);
            bool lightning = _damageSource == DamageSource.LightningProjectile;
            _trailRenderer.time = (lightning ? 0.28f : 0.22f) + ((_visualLevel - 1) * 0.07f);
            _trailRenderer.startWidth = (lightning ? 0.16f : 0.12f) + ((_visualLevel - 1) * 0.06f);
            _trailRenderer.endWidth = 0.02f + ((_visualLevel - 1) * 0.015f);
            _trailRenderer.startColor = startColor;
            _trailRenderer.endColor = endColor;
            _trailRenderer.sortingOrder = 4;

            if (_trailRenderer.sharedMaterial == null)
                _trailRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        private void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            EnemyBase enemy = _target.GetComponent<EnemyBase>();
            if (enemy != null && enemy.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _target.position, _speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _target.position) < 0.2f)
            {
                Hit();
            }
        }

        private void Hit()
        {
            EnemyBase enemy = _target.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage, _impactColor, _damageSource);
                enemy.FlashHit(_impactColor);
            }
            Destroy(gameObject);
        }
    }
}

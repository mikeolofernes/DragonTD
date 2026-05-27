using System.Collections;
using UnityEngine;
using DragonTD.Core;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected EnemyData _data;

        protected float _currentHp;
        protected float _maxHp;
        protected float _moveSpeed;
        protected Transform[] _waypoints;
        protected int _waypointIndex;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _flashRoutine;
        private EnemyHealthBar _healthBar;
        private Coroutine _slowRoutine;
        private Coroutine _burnRoutine;
        private Coroutine _vulnerabilityRoutine;
        private float _moveSpeedMultiplier = 1f;
        private float _damageTakenMultiplier = 1f;
        private bool _shieldCracked;
        private float _burnSuppressRegenUntil;
        private TextMesh _traitLabel;
        private SpriteRenderer _traitBadge;
        private LineRenderer _shieldRing;
        private Vector3 _baseScale = Vector3.one;
        private float _nextRegenPulseTime;
        private bool _reachedBase;

        public bool IsDead { get; private set; }
        public EnemyData Data => _data;
        public float HpPercent => _maxHp > 0f ? _currentHp / _maxHp : 0f;
        public DragonElement EnemyElement => _data.Element;
        public bool HasElement => _data.HasElement;
        public EnemyTrait Trait => _data != null ? _data.Trait : EnemyTrait.None;
        public float CurrentMoveSpeed => _moveSpeed * _moveSpeedMultiplier;

        public event System.Action<EnemyBase> OnDied;
        public event System.Action<float> OnHpChanged;

        protected void RaiseHpChanged() => OnHpChanged?.Invoke(HpPercent);

        public float DistanceToGoal
        {
            get
            {
                if (_waypoints == null || _waypointIndex >= _waypoints.Length) return 0f;
                float dist = Vector3.Distance(transform.position, _waypoints[_waypointIndex].position);
                for (int i = _waypointIndex + 1; i < _waypoints.Length; i++)
                    dist += Vector3.Distance(_waypoints[i - 1].position, _waypoints[i].position);
                return dist;
            }
        }

        public void Initialize(Transform[] waypoints)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            if (_healthBar != null)
                _healthBar.Initialize(this);
            _waypoints = waypoints;
            _maxHp = _data.MaxHp;
            _moveSpeed = _data.MoveSpeed;
            _currentHp = _maxHp;
            _waypointIndex = 0;
            _shieldCracked = Trait != EnemyTrait.Shielded;
            _baseScale = transform.localScale;
            ApplyTraitVisuals();
            EnsureTraitLabel();
            OnHpChanged?.Invoke(HpPercent);
        }

        public void ApplyDifficultyMultiplier(float multiplier)
        {
            _maxHp *= multiplier;
            _currentHp = _maxHp;
            _moveSpeed *= Mathf.Lerp(1f, multiplier, 0.5f);
            OnHpChanged?.Invoke(HpPercent);
        }

        private void Update()
        {
            if (IsDead) return;
            MoveTowardsWaypoint();
            Tick();
            AnimateTraitVisuals();
        }

        protected virtual void Tick() { }

        private void LateUpdate()
        {
            if (IsDead) return;
            TickTraitEffects();
        }

        protected virtual void MoveTowardsWaypoint()
        {
            if (_waypoints == null || _waypointIndex >= _waypoints.Length) return;

            Transform target = _waypoints[_waypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target.position, CurrentMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                _waypointIndex++;
                if (_waypointIndex >= _waypoints.Length)
                    ReachBase();
            }
        }

        public virtual void TakeDamage(float damage)
        {
            TakeDamage(damage, Color.white);
        }

        public virtual void TakeDamage(float damage, Color indicatorColor)
        {
            TakeDamage(damage, indicatorColor, DamageSource.Projectile);
        }

        public virtual void TakeDamage(float damage, Color indicatorColor, DamageSource source)
        {
            if (IsDead) return;
            float adjustedDamage = ApplyTraitDamageRules(damage, indicatorColor, source);
            float effective = Mathf.Max(1f, (adjustedDamage * _damageTakenMultiplier) - _data.Armor);
            _currentHp -= effective;
            DamageIndicator.Spawn(transform.position + Vector3.up * 0.55f, effective, PrototypeBalance.DamageColor(source, indicatorColor));
            OnHpChanged?.Invoke(HpPercent);
            if (_currentHp <= 0f) Die();
        }

        public void ApplyStatusEffects(StatusEffect[] effects, Color sourceColor, float magnitudeMultiplier = 1f)
        {
            if (effects == null) return;

            foreach (StatusEffect effect in effects)
                ApplyStatusEffect(effect, sourceColor, magnitudeMultiplier);
        }

        public void ApplyStatusEffect(StatusEffect effect, Color sourceColor, float magnitudeMultiplier = 1f)
        {
            if (effect == null || IsDead) return;
            StatusEffect appliedEffect = new StatusEffect
            {
                effectId = effect.effectId,
                displayName = effect.displayName,
                duration = effect.duration,
                magnitude = effect.magnitude * Mathf.Max(0.1f, magnitudeMultiplier),
                stackable = effect.stackable,
                maxStacks = effect.maxStacks
            };

            string id = (appliedEffect.effectId ?? string.Empty).ToLowerInvariant();
            if (id.Contains("slow") || id.Contains("frost") || id.Contains("blizzard"))
            {
                if (Trait == EnemyTrait.Flying)
                {
                    appliedEffect.magnitude *= PrototypeBalance.FlyingSlowMultiplier;
                    DamageIndicator.SpawnText(transform.position + Vector3.up * 1.05f, "RESIST", PrototypeBalance.ResistFeedbackColor);
                }
                DamageIndicator.SpawnText(transform.position + Vector3.up * 0.85f, "SLOW", sourceColor);
                if (_slowRoutine != null) StopCoroutine(_slowRoutine);
                _slowRoutine = StartCoroutine(SlowRoutine(appliedEffect));
            }
            else if (id.Contains("burn") || id.Contains("magma") || id.Contains("ignite"))
            {
                _burnSuppressRegenUntil = Time.time + appliedEffect.duration + 0.5f;
                DamageIndicator.SpawnText(transform.position + Vector3.up * 0.85f, "BURN", sourceColor);
                if (_burnRoutine != null) StopCoroutine(_burnRoutine);
                _burnRoutine = StartCoroutine(BurnRoutine(appliedEffect, sourceColor));
            }
            else if (id.Contains("vulnerable") || id.Contains("void") || id.Contains("shock"))
            {
                DamageIndicator.SpawnText(transform.position + Vector3.up * 0.85f, "VULN", sourceColor);
                if (_vulnerabilityRoutine != null) StopCoroutine(_vulnerabilityRoutine);
                _vulnerabilityRoutine = StartCoroutine(VulnerabilityRoutine(appliedEffect));
            }
        }

        public void FlashHit(Color color)
        {
            if (_spriteRenderer == null || !gameObject.activeInHierarchy) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashHitRoutine(color));
        }

        private IEnumerator FlashHitRoutine(Color color)
        {
            Color original = _spriteRenderer.color;
            _spriteRenderer.color = Color.Lerp(original, color, 0.65f);
            yield return new WaitForSeconds(0.08f);
            if (_spriteRenderer != null)
                _spriteRenderer.color = original;
            _flashRoutine = null;
        }

        private IEnumerator SlowRoutine(StatusEffect effect)
        {
            _moveSpeedMultiplier = Mathf.Clamp(1f - effect.magnitude, 0.25f, 1f);
            yield return new WaitForSeconds(Mathf.Max(0.1f, effect.duration));
            _moveSpeedMultiplier = 1f;
            _slowRoutine = null;
        }

        private IEnumerator BurnRoutine(StatusEffect effect, Color color)
        {
            float elapsed = 0f;
            float tick = 0.5f;
            while (elapsed < effect.duration && !IsDead)
            {
                TakeDamage(Mathf.Max(1f, effect.magnitude), color, DamageSource.Burn);
                yield return new WaitForSeconds(tick);
                elapsed += tick;
            }
            _burnRoutine = null;
        }

        private IEnumerator VulnerabilityRoutine(StatusEffect effect)
        {
            _damageTakenMultiplier = Mathf.Max(1f, 1f + effect.magnitude);
            yield return new WaitForSeconds(Mathf.Max(0.1f, effect.duration));
            _damageTakenMultiplier = 1f;
            _vulnerabilityRoutine = null;
        }

        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
            DeathPopEffect.Spawn(transform.position, _spriteRenderer != null ? _spriteRenderer.color : Color.white);
            OnDied?.Invoke(this);
            if (!_reachedBase)
                BattleStatsTracker.Instance?.RecordEnemyKilled();
            WaveManager.Instance?.OnEnemyDied();
            if (!_reachedBase)
            {
                ResourceManager.Instance?.AddGold(_data.GoldValue);
                BattleStatsTracker.Instance?.RecordReward(_data.GoldValue, 0);
            }
            Destroy(gameObject);
        }

        private float ApplyTraitDamageRules(float damage, Color color, DamageSource source)
        {
            float adjusted = damage;

            if (Trait == EnemyTrait.Shielded && !_shieldCracked)
            {
                if (source == DamageSource.Skill || source == DamageSource.LightningProjectile)
                {
                    _shieldCracked = true;
                    if (_shieldRing != null)
                        _shieldRing.enabled = false;
                    UpdateTraitLabel();
                    DamageIndicator.SpawnText(transform.position + Vector3.up * 1.1f, "SHIELD BREAK", PrototypeBalance.ShieldBreakColor);
                    SkillCastEffect.SpawnPulse(transform.position, 1f, PrototypeBalance.ShieldBreakColor);
                }
                else
                {
                    adjusted *= Mathf.Clamp(_data.ProjectileDamageMultiplier <= 0f ? PrototypeBalance.ShieldProjectileMultiplier : _data.ProjectileDamageMultiplier, 0.1f, 1f);
                    DamageIndicator.SpawnText(transform.position + Vector3.up * 1.1f, "RESIST", PrototypeBalance.ResistFeedbackColor);
                }
            }

            if (source == DamageSource.Skill && _data.SkillDamageMultiplier > 0f)
                adjusted *= _data.SkillDamageMultiplier;

            return adjusted;
        }

        private void TickTraitEffects()
        {
            if (Trait != EnemyTrait.Regenerating) return;
            if (Time.time < _burnSuppressRegenUntil) return;
            if (_currentHp >= _maxHp) return;

            float regen = Mathf.Max(0f, _data.RegenPerSecond) * Time.deltaTime;
            if (regen <= 0f) return;

            _currentHp = Mathf.Min(_maxHp, _currentHp + regen);
            OnHpChanged?.Invoke(HpPercent);
            if (Time.time >= _nextRegenPulseTime)
            {
                _nextRegenPulseTime = Time.time + 0.75f;
                SkillCastEffect.SpawnPulse(transform.position, 0.7f, PrototypeBalance.HealColor);
                DamageIndicator.SpawnText(transform.position + Vector3.up * 0.95f, $"+{Mathf.CeilToInt(regen / Mathf.Max(Time.deltaTime, 0.001f))}/s", PrototypeBalance.HealColor);
            }
        }

        private void AnimateTraitVisuals()
        {
            if (Trait == EnemyTrait.Flying)
            {
                float bob = 1f + Mathf.Sin(Time.time * 5f) * 0.04f;
                transform.localScale = _baseScale * bob;
            }
            else if (Trait == EnemyTrait.Runner)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.025f;
                transform.localScale = _baseScale * pulse;
            }
        }

        private void EnsureTraitLabel()
        {
            if (Trait == EnemyTrait.None) return;

            EnsureTraitBadge();

            var labelGO = new GameObject("TraitLabel");
            labelGO.transform.SetParent(transform, false);
            labelGO.transform.localPosition = new Vector3(0f, 1.38f, -0.1f);
            _traitLabel = labelGO.AddComponent<TextMesh>();
            _traitLabel.anchor = TextAnchor.MiddleCenter;
            _traitLabel.alignment = TextAlignment.Center;
            _traitLabel.characterSize = 0.115f;
            _traitLabel.fontSize = 36;
            var renderer = labelGO.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 35;
            UpdateTraitLabel();
        }

        private void EnsureTraitBadge()
        {
            if (_traitBadge != null) return;

            var badgeGO = new GameObject("TraitBadge");
            badgeGO.transform.SetParent(transform, false);
            badgeGO.transform.localPosition = new Vector3(0f, 1.38f, 0f);
            badgeGO.transform.localScale = new Vector3(1.2f, 0.34f, 1f);
            _traitBadge = badgeGO.AddComponent<SpriteRenderer>();
            _traitBadge.sprite = RuntimeWhiteSprite();
            _traitBadge.sortingOrder = 34;
        }

        private void UpdateTraitLabel()
        {
            if (_traitLabel == null) return;

            _traitLabel.text = Trait switch
            {
                EnemyTrait.Runner => "RUN",
                EnemyTrait.Brute => "ARMOR",
                EnemyTrait.Shielded => _shieldCracked ? "CRACK" : "SHIELD",
                EnemyTrait.Regenerating => "REGEN",
                EnemyTrait.Flying => "FLY",
                _ => ""
            };
            _traitLabel.color = Trait switch
            {
                EnemyTrait.Shielded => _shieldCracked ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.55f, 0.85f, 1f),
                EnemyTrait.Regenerating => new Color(0.3f, 1f, 0.45f),
                EnemyTrait.Flying => new Color(0.95f, 0.85f, 1f),
                EnemyTrait.Brute => new Color(1f, 0.75f, 0.35f),
                _ => Color.white
            };
            if (_traitBadge != null)
            {
                Color badgeColor = Trait switch
                {
                    EnemyTrait.Shielded => _shieldCracked ? new Color(0.2f, 0.2f, 0.25f, 0.8f) : new Color(0.05f, 0.25f, 0.55f, 0.88f),
                    EnemyTrait.Regenerating => new Color(0.02f, 0.35f, 0.12f, 0.88f),
                    EnemyTrait.Flying => new Color(0.32f, 0.12f, 0.48f, 0.88f),
                    EnemyTrait.Brute => new Color(0.42f, 0.24f, 0.06f, 0.88f),
                    EnemyTrait.Runner => new Color(0.1f, 0.32f, 0.08f, 0.88f),
                    _ => new Color(0f, 0f, 0f, 0.7f)
                };
                _traitBadge.color = badgeColor;
            }
        }

        private void ApplyTraitVisuals()
        {
            if (_spriteRenderer == null) return;

            _spriteRenderer.color = Trait switch
            {
                EnemyTrait.Shielded => new Color(0.45f, 0.78f, 1f, 1f),
                EnemyTrait.Regenerating => new Color(0.45f, 1f, 0.45f, 1f),
                EnemyTrait.Flying => new Color(0.9f, 0.65f, 1f, 1f),
                EnemyTrait.Brute => new Color(0.74f, 0.48f, 0.22f, 1f),
                EnemyTrait.Runner => new Color(0.6f, 1f, 0.35f, 1f),
                _ => _spriteRenderer.color
            };

            if (Trait == EnemyTrait.Shielded)
                EnsureShieldRing();
            if (Trait == EnemyTrait.Runner)
                EnsureTraitTrail(new Color(0.55f, 1f, 0.25f, 0.75f), 0.22f);
            if (Trait == EnemyTrait.Flying)
                EnsureTraitTrail(new Color(0.85f, 0.55f, 1f, 0.62f), 0.28f);
        }

        private void EnsureTraitTrail(Color color, float time)
        {
            if (GetComponent<TrailRenderer>() != null) return;

            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = time;
            trail.startWidth = 0.26f;
            trail.endWidth = 0.02f;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.sortingOrder = 3;
            trail.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void EnsureShieldRing()
        {
            if (_shieldRing != null) return;

            var ringGO = new GameObject("ShieldRing");
            ringGO.transform.SetParent(transform, false);
            ringGO.transform.localPosition = Vector3.zero;
            _shieldRing = ringGO.AddComponent<LineRenderer>();
            _shieldRing.loop = true;
            _shieldRing.useWorldSpace = false;
            _shieldRing.positionCount = 48;
            _shieldRing.startWidth = 0.045f;
            _shieldRing.endWidth = 0.045f;
            _shieldRing.sortingOrder = 18;
            _shieldRing.material = new Material(Shader.Find("Sprites/Default"));
            _shieldRing.startColor = new Color(0.45f, 0.9f, 1f, 0.95f);
            _shieldRing.endColor = new Color(0.45f, 0.9f, 1f, 0.95f);

            for (int i = 0; i < _shieldRing.positionCount; i++)
            {
                float radians = i / (float)_shieldRing.positionCount * Mathf.PI * 2f;
                _shieldRing.SetPosition(i, new Vector3(Mathf.Cos(radians) * 0.75f, Mathf.Sin(radians) * 0.75f, 0f));
            }
        }

        private static Sprite _runtimeWhiteSprite;
        private static Sprite RuntimeWhiteSprite()
        {
            if (_runtimeWhiteSprite != null)
                return _runtimeWhiteSprite;

            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            _runtimeWhiteSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            return _runtimeWhiteSprite;
        }

        protected void ReachBase()
        {
            _reachedBase = true;
            BattleStatsTracker.Instance?.RecordEnemyLeaked();
            GameManager.Instance?.LoseLife(_data.DamageToBase);
            Die();
        }
    }
}

using System.Collections;
using UnityEngine;
using DragonTD.Core;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public class EnemyBase : MonoBehaviour
    {
        private const float VisualScaleMultiplier = 1f;

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
        private Coroutine _stunRoutine;
        private float _moveSpeedMultiplier = 1f;
        private float _damageTakenMultiplier = 1f;
        private bool _shieldCracked;
        private float _shieldBrokenTime = float.NegativeInfinity;
        private float _burnSuppressRegenUntil;
        private TextMesh _traitLabel;
        private SpriteRenderer _traitBadge;
        private LineRenderer _shieldRing;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _lastPosition;
        private Vector3 _movementDirection = Vector3.right;
        private float _walkPhase;
        private Transform _shadow;
        private Transform _visualRoot;
        private EnemySkeletalAnimator _skeletalAnimator;
        private EnemyAnimatorBridge _animatorBridge;
        private Vector3 _desiredMovementDirection = Vector3.right;
        private float _nextRegenPulseTime;
        private bool _reachedBase;
        private bool  _isLaneMode;
        private float _wallWorldX;

        [SerializeField] private float _deathAnimationDelay = 0.35f;

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
            _skeletalAnimator = GetComponentInChildren<EnemySkeletalAnimator>(true);
            _animatorBridge = GetComponentInChildren<EnemyAnimatorBridge>(true);
            _spriteRenderer = ConfigureCenteredVisualRenderer();
            _healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            if (_healthBar != null)
                _healthBar.Initialize(this);
            _waypoints = waypoints;
            _maxHp = _data.MaxHp;
            _moveSpeed = _data.MoveSpeed;
            _currentHp = _maxHp;
            _waypointIndex = 0;
            Debug.Log($"[EnemyBase] Init '{name}' pos={transform.position}, waypoints={_waypoints?.Length ?? -1}, speed={_moveSpeed}");
            SkipReachedWaypoints();
            Debug.Log($"[EnemyBase] AfterSkip: idx={_waypointIndex}, dead={IsDead}, nextWP={((_waypoints != null && _waypointIndex < _waypoints?.Length) ? _waypoints[_waypointIndex]?.position.ToString() : "NONE")}");
            _shieldCracked = Trait != EnemyTrait.Shielded;
            _baseScale = transform.localScale * VisualScaleMultiplier;
            transform.localScale = _baseScale;
            _lastPosition = transform.position;
            _walkPhase = Random.value * Mathf.PI * 2f;
            ApplyTraitVisuals();
            RemoveTraitLabel();
            EnsureShadow();
            OnHpChanged?.Invoke(HpPercent);
        }

        public void InitializeLane(float wallWorldX)
        {
            _isLaneMode = true;
            _wallWorldX = wallWorldX;
            _skeletalAnimator = GetComponentInChildren<EnemySkeletalAnimator>(true);
            _animatorBridge = GetComponentInChildren<EnemyAnimatorBridge>(true);
            _spriteRenderer = ConfigureCenteredVisualRenderer();
            _healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            if (_healthBar != null) _healthBar.Initialize(this);
            _waypoints = new Transform[0];
            _maxHp = _data.MaxHp;
            _moveSpeed = _data.MoveSpeed;
            _currentHp = _maxHp;
            _waypointIndex = 0;
            _shieldCracked = Trait != EnemyTrait.Shielded;
            _baseScale = transform.localScale * VisualScaleMultiplier;
            transform.localScale = _baseScale;
            _lastPosition = transform.position;
            _walkPhase = Random.value * Mathf.PI * 2f;
            ApplyTraitVisuals();
            RemoveTraitLabel();
            EnsureShadow();
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

        protected virtual void Tick()
        {
            // Shield regeneration: restore a broken shield after ShieldRegenDelay.
            if (Trait != EnemyTrait.Shielded) return;
            if (!_shieldCracked) return;
            if (_data == null || _data.ShieldRegenDelay <= 0f) return;
            if (Time.time - _shieldBrokenTime < _data.ShieldRegenDelay) return;

            _shieldCracked = false;
            if (_shieldRing != null)
                _shieldRing.enabled = true;
            UpdateTraitLabel();
            SkillCastEffect.SpawnPulse(transform.position, 1f, new Color(0.45f, 0.9f, 1f, 0.95f));
            DamageIndicator.SpawnText(transform.position + Vector3.up * 1.1f, "SHIELD UP", new Color(0.45f, 0.9f, 1f, 1f));
        }

        private void LateUpdate()
        {
            if (IsDead) return;
            TickTraitEffects();
        }

        private bool _loggedFirstMove;
        protected virtual void MoveTowardsWaypoint()
        {
            if (_isLaneMode)
            {
                _desiredMovementDirection = Vector3.right;
                transform.position += Vector3.right * CurrentMoveSpeed * Time.deltaTime;
                if (transform.position.x >= _wallWorldX)
                    HitWall();
                return;
            }

            if (!_loggedFirstMove)
            {
                _loggedFirstMove = true;
                Debug.Log($"[EnemyBase] FirstMove '{name}': pos={transform.position}, wpIdx={_waypointIndex}, wpLen={_waypoints?.Length ?? -1}, speed={CurrentMoveSpeed}, timeScale={Time.timeScale}, dead={IsDead}");
            }

            SkipReachedWaypoints();
            if (_waypoints == null || _waypointIndex >= _waypoints.Length) return;

            Transform target = _waypoints[_waypointIndex];
            Vector3 toTarget = target.position - transform.position;
            if (toTarget.sqrMagnitude > 0.0004f)
                _desiredMovementDirection = toTarget.normalized;

            transform.position = Vector3.MoveTowards(transform.position, target.position, CurrentMoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                _waypointIndex++;
                if (_waypointIndex < _waypoints.Length && _waypoints[_waypointIndex] != null)
                {
                    Vector3 nextLeg = _waypoints[_waypointIndex].position - transform.position;
                    if (nextLeg.sqrMagnitude > 0.0004f)
                        _desiredMovementDirection = nextLeg.normalized;
                }

                if (_waypointIndex >= _waypoints.Length)
                    ReachBase();
            }
        }

        private void SkipReachedWaypoints()
        {
            if (_waypoints == null) return;

            while (_waypointIndex < _waypoints.Length)
            {
                Transform waypoint = _waypoints[_waypointIndex];
                if (waypoint == null)
                {
                    _waypointIndex++;
                    continue;
                }

                if (Vector3.Distance(transform.position, waypoint.position) > 0.05f)
                    break;

                _waypointIndex++;
            }

            if (_waypointIndex < _waypoints.Length && _waypoints[_waypointIndex] != null)
            {
                Vector3 nextLeg = _waypoints[_waypointIndex].position - transform.position;
                if (nextLeg.sqrMagnitude > 0.0004f)
                    _desiredMovementDirection = nextLeg.normalized;
            }
            else if (_waypoints.Length > 0)
            {
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

        public void ApplyStun(float duration)
        {
            if (IsDead) return;
            if (_stunRoutine != null) StopCoroutine(_stunRoutine);
            _animatorBridge?.SetStunned(true);
            _stunRoutine = StartCoroutine(StunRoutine(duration));
        }

        private IEnumerator StunRoutine(float duration)
        {
            float prev = _moveSpeedMultiplier;
            _moveSpeedMultiplier = 0f;
            DamageIndicator.SpawnText(transform.position + Vector3.up * 0.85f, "STUN", new Color(1f, 0.9f, 0.2f, 1f));
            yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
            _moveSpeedMultiplier = prev;
            _stunRoutine = null;
            _animatorBridge?.SetStunned(false);
        }

        public void FlashHit(Color color)
        {
            _animatorBridge?.PlayHit();
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
            _animatorBridge?.PlayDeath();
            if (!_reachedBase) AudioManager.Instance?.PlaySfx(SfxKey.EnemyDeath);
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
            DisableColliders();
            float destroyDelay = _animatorBridge != null && _animatorBridge.HasRuntimeAnimator
                ? Mathf.Max(0f, _deathAnimationDelay)
                : 0f;
            Destroy(gameObject, destroyDelay);
        }

        private float ApplyTraitDamageRules(float damage, Color color, DamageSource source)
        {
            float adjusted = damage;

            if (Trait == EnemyTrait.Shielded && !_shieldCracked)
            {
                if (source == DamageSource.Skill || source == DamageSource.LightningProjectile)
                {
                    _shieldCracked = true;
                    _shieldBrokenTime = Time.time;
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
            Vector3 delta = transform.position - _lastPosition;
            bool isMoving = delta.sqrMagnitude > 0.000001f && CurrentMoveSpeed > 0.01f;
            if (isMoving)
                _movementDirection = _desiredMovementDirection.sqrMagnitude > 0.0001f
                    ? _desiredMovementDirection.normalized
                    : delta.normalized;

            bool useRuntimeAnimator = _animatorBridge != null && _animatorBridge.HasPolishedPresentation;
            _animatorBridge?.SetMovement(_movementDirection, Mathf.Clamp01(CurrentMoveSpeed / 3f), isMoving || Trait == EnemyTrait.Flying);
            _animatorBridge?.SetStunned(_stunRoutine != null);

            if (Trait == EnemyTrait.Flying)
            {
                float hover = Mathf.Sin(Time.time * 5.2f + _walkPhase);
                if (!useRuntimeAnimator)
                {
                    float scale = 1f + hover * 0.045f;
                    transform.localScale = _baseScale * scale;
                    transform.rotation = Quaternion.Euler(0f, 0f, hover * 2.5f);
                    ApplyVisualLocalMotion(new Vector3(0f, hover * 0.08f, 0f), Vector3.one, Quaternion.identity, 18f);
                }
                UpdateShadow(0.72f - hover * 0.08f, 0.24f - hover * 0.03f, 0.24f);
            }
            else if (isMoving)
            {
                transform.localScale = _baseScale;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * 18f);

                if (useRuntimeAnimator)
                {
                    ApplyVisualLocalMotion(Vector3.zero, Vector3.one, Quaternion.identity, 20f);
                    UpdateShadow(0.82f, 0.25f, 0.32f);
                }
                else if (_skeletalAnimator != null && _skeletalAnimator.IsConfigured)
                {
                    _skeletalAnimator.TickWalk(_movementDirection, Mathf.Clamp01(CurrentMoveSpeed / 3f), Trait);
                    ApplyVisualLocalMotion(Vector3.zero, Vector3.one, Quaternion.identity, 20f);
                    UpdateShadow(0.82f, 0.25f, 0.32f);
                }
                else
                {
                    float speed = Trait == EnemyTrait.Runner ? 13.5f : Trait == EnemyTrait.Brute ? 7.5f : 10.5f;
                    float step = Mathf.Sin(Time.time * speed + _walkPhase);
                    float bob = Mathf.Abs(step) * 0.08f;
                    float stride = step * 0.035f;
                    float footfall = Mathf.Abs(step);
                    float squash = 1f + footfall * 0.055f;
                    float stretch = 1f - footfall * 0.04f;
                    float lean = Mathf.Clamp(-_movementDirection.x * 5.5f + step * 2.1f, -7f, 7f);

                    ApplyVisualLocalMotion(new Vector3(stride, bob, 0f), new Vector3(squash, stretch, 1f), Quaternion.Euler(0f, 0f, lean), 20f);
                    UpdateShadow(0.82f + footfall * 0.08f, 0.25f - footfall * 0.04f, 0.32f);
                }

                if (_spriteRenderer != null && Mathf.Abs(_movementDirection.x) > 0.05f)
                    _spriteRenderer.flipX = _movementDirection.x < 0f;
            }
            else
            {
                if (!useRuntimeAnimator)
                {
                    float idle = Mathf.Sin(Time.time * 2.4f + _walkPhase) * 0.018f;
                    transform.localScale = _baseScale * (1f + idle);
                }
                else
                {
                    transform.localScale = _baseScale;
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * 12f);
                if (!useRuntimeAnimator)
                    _skeletalAnimator?.TickIdle();
                ApplyVisualLocalMotion(Vector3.zero, Vector3.one, Quaternion.identity, 12f);
                UpdateShadow(0.78f, 0.25f, 0.3f);
            }

            _lastPosition = transform.position;
        }

        private SpriteRenderer ConfigureCenteredVisualRenderer()
        {
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (_animatorBridge != null && _animatorBridge.HasPolishedPresentation)
            {
                SpriteRenderer presentationRenderer = _animatorBridge.PrimarySpriteRenderer;
                if (_animatorBridge.HasDirectionalPresentation)
                    return presentationRenderer != null ? presentationRenderer : rootRenderer;
                if (_animatorBridge.HasRuntimeAnimator && presentationRenderer == null)
                    return rootRenderer;
                DisableCompetingSpriteRenderers(presentationRenderer);
                return presentationRenderer != null ? presentationRenderer : rootRenderer;
            }

            if (rootRenderer == null)
                return null;

            if (_skeletalAnimator != null && _skeletalAnimator.IsConfigured)
            {
                rootRenderer.enabled = false;
                return rootRenderer;
            }

            if (_visualRoot == null)
            {
                var visualGO = new GameObject("EnemyVisual");
                visualGO.transform.SetParent(transform, false);
                _visualRoot = visualGO.transform;
            }

            SpriteRenderer visualRenderer = _visualRoot.GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
                visualRenderer = _visualRoot.gameObject.AddComponent<SpriteRenderer>();

            visualRenderer.sprite = rootRenderer.sprite;
            visualRenderer.color = rootRenderer.color;
            visualRenderer.flipX = rootRenderer.flipX;
            visualRenderer.flipY = rootRenderer.flipY;
            visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
            visualRenderer.sortingOrder = rootRenderer.sortingOrder;
            visualRenderer.sharedMaterial = rootRenderer.sharedMaterial;
            rootRenderer.enabled = false;
            _visualRoot.localPosition = Vector3.zero;
            _visualRoot.localRotation = Quaternion.identity;
            _visualRoot.localScale = Vector3.one;
            return visualRenderer;
        }

        private void DisableCompetingSpriteRenderers(SpriteRenderer presentationRenderer)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || renderer == presentationRenderer) continue;
                renderer.enabled = false;
            }

            if (presentationRenderer != null)
                presentationRenderer.enabled = true;
        }

        private void DisableColliders()
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        private void ApplyVisualLocalMotion(Vector3 localPosition, Vector3 localScale, Quaternion localRotation, float damping)
        {
            if (_visualRoot == null) return;

            _visualRoot.localPosition = Vector3.Lerp(_visualRoot.localPosition, localPosition, Time.deltaTime * damping);
            _visualRoot.localScale = Vector3.Lerp(_visualRoot.localScale, localScale, Time.deltaTime * damping);
            _visualRoot.localRotation = Quaternion.Slerp(_visualRoot.localRotation, localRotation, Time.deltaTime * damping);
        }

        private void RemoveTraitLabel()
        {
            if (_traitLabel != null)
                Destroy(_traitLabel.gameObject);
            if (_traitBadge != null)
                Destroy(_traitBadge.gameObject);
            _traitLabel = null;
            _traitBadge = null;
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
                RemoveTraitTrail();
            if (Trait == EnemyTrait.Flying)
                RemoveTraitTrail();
        }

        private void RemoveTraitTrail()
        {
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
                Destroy(trail);
        }

        private void EnsureShadow()
        {
            if (_shadow != null) return;

            var shadowGO = new GameObject("EnemyShadow");
            shadowGO.transform.SetParent(transform, false);
            shadowGO.transform.localPosition = new Vector3(0f, -0.42f, 0.05f);
            var sr = shadowGO.AddComponent<SpriteRenderer>();
            sr.sprite = SoftShadowSprite();
            sr.color = new Color(0f, 0f, 0f, Trait == EnemyTrait.Flying ? 0.2f : 0.34f);
            sr.sortingOrder = 0;
            _shadow = shadowGO.transform;
            UpdateShadow(0.78f, 0.25f, Trait == EnemyTrait.Flying ? 0.2f : 0.34f);
        }

        private void UpdateShadow(float width, float height, float alpha)
        {
            if (_shadow == null) return;
            _shadow.position = new Vector3(transform.position.x, transform.position.y - 0.42f, transform.position.z + 0.05f);
            _shadow.rotation = Quaternion.identity;
            _shadow.localScale = new Vector3(width, height, 1f);
            if (_shadow.TryGetComponent(out SpriteRenderer sr))
                sr.color = new Color(0f, 0f, 0f, alpha);
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
        private static Sprite _softShadowSprite;
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

        private static Sprite SoftShadowSprite()
        {
            if (_softShadowSprite != null)
                return _softShadowSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            _softShadowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _softShadowSprite;
        }

        protected void ReachBase()
        {
            _reachedBase = true;
            AudioManager.Instance?.PlaySfx(SfxKey.EnemyReachBase);
            BattleStatsTracker.Instance?.RecordEnemyLeaked();
            GameManager.Instance?.LoseLife(_data.DamageToBase);
            Die();
        }

        private void HitWall()
        {
            if (_reachedBase) return;
            _reachedBase = true;
            if (WallBase.Instance != null)
                WallBase.Instance.TakeDamage(_data.DamageToBase * 50f);
            BattleStatsTracker.Instance?.RecordEnemyLeaked();
            Die();
        }
    }
}

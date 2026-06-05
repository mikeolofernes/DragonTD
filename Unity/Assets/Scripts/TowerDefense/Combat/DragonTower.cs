using UnityEngine;
using DragonTD.Dragons;
using DragonTD.Core;

namespace DragonTD.TowerDefense
{
    public class DragonTower : MonoBehaviour
    {
        private const float DeployedVisualScaleMultiplier = 1.25f;

        private DragonInstance _dragonInstance;
        private float _attackCooldown;
        private float _lastAttackTime;
        private float _activeSkillCooldown;
        private float _lastActiveSkillTime = float.NegativeInfinity;
        private TowerRangeIndicator _rangeIndicator;
        private int _upgradeLevel = 1;
        private GridTile _placedTile;
        private int _manaRefund;
        private float _rangeBonus;
        private float _damageMultiplier = 1f;
        private float _statusMagnitudeMultiplier = 1f;
        private Vector3 _baseScale;
        private TextMesh _levelBadge;
        private SpriteRenderer _levelBadgeBackground;
        private LineRenderer _levelAura;
        private string _fusedPartnerId = string.Empty;
        private string _fusedPartnerName = string.Empty;
        private DragonElement _fusedPartnerElement;
        private bool _hasInheritedPassive;
        private float _nextInheritedPassiveTime;
        private float _ultimateCooldown;
        private float _lastUltimateCastTime = float.NegativeInfinity;

        // Fake 2.5D: ground shadow + idle bob + breathing + attack lunge
        private Vector3 _basePos;
        private Vector3 _levelScale = Vector3.one;
        private Transform _shadow;
        private Transform _ambientGlow;
        private SpriteRenderer _ambientGlowRenderer;
        private float _bobPhase;
        private float _attackLungeUntil = float.NegativeInfinity;
        private Vector3 _attackLungeDir;

        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private Color _projectileColor = Color.white;
        [SerializeField] private bool _autoCastActiveSkill;

        public string DisplayName => _dragonInstance?.Definition?.displayName ?? "Dragon";
        public string DragonId => _dragonInstance?.Definition?.dragonId ?? string.Empty;
        public int UpgradeLevel => _upgradeLevel;
        public string TierLabel => IsFused ? _hasInheritedPassive ? "Hybrid Fused" : "Fused" : $"Lv {_upgradeLevel}";
        public string FusionPreviewName => _hasInheritedPassive ? $"{DisplayName} + {_fusedPartnerName}" : DisplayName;
        public int UpgradeCost => _upgradeLevel >= PrototypeBalance.MaxUpgradeLevel ? 0 : PrototypeBalance.UpgradeBaseCost * _upgradeLevel;
        public bool IsMaxUpgrade => _upgradeLevel >= PrototypeBalance.MaxUpgradeLevel;
        public bool IsFused => _upgradeLevel >= PrototypeBalance.FusedTowerLevel;
        public bool IsReadyToFuse => _upgradeLevel == PrototypeBalance.MaxUpgradeLevel;
        public SkillDefinition ActiveSkill => _dragonInstance?.Definition?.ActiveSkill;
        public Color ProjectileColor => _projectileColor;
        public float AttackRange => _dragonInstance?.Definition?.NormalAttack != null
            ? _dragonInstance.Definition.NormalAttack.range + _rangeBonus
            : (_dragonInstance?.Definition?.baseStats.range ?? 0f) + _rangeBonus;
        public float ActiveSkillCooldownRemaining => ActiveSkill == null
            ? 0f
            : Mathf.Max(0f, _activeSkillCooldown - (Time.time - _lastActiveSkillTime));
        public bool IsActiveSkillReady => ActiveSkill != null && ActiveSkillCooldownRemaining <= 0f;

        public void Setup(DragonInstance instance)
        {
            Setup(instance, null, instance?.Definition?.manaCost ?? 0);
        }

        public void Setup(DragonInstance instance, GridTile placedTile, int manaCost)
        {
            _dragonInstance = instance;
            _placedTile = placedTile;
            _baseScale = transform.localScale * DeployedVisualScaleMultiplier;
            transform.localScale = _baseScale;
            _manaRefund = Mathf.CeilToInt(manaCost * PrototypeBalance.SellManaRefundPercent);
            _projectileColor = instance.Definition.visualData.primaryColor;
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (spriteRenderer.sprite == null)
                {
                    Sprite portrait = instance.Definition.visualData != null ? instance.Definition.visualData.portrait : null;
                    if (portrait != null)
                    {
                        spriteRenderer.sprite = portrait;
                        spriteRenderer.color  = Color.white;
                    }
                    else
                    {
                        spriteRenderer.color = _projectileColor;
                    }
                }
            }
            EnsureClickableCollider();
            float baseAttackCooldown = instance.Definition.NormalAttack != null
                ? instance.Definition.NormalAttack.cooldown : 1f;
            _attackCooldown = baseAttackCooldown / Mathf.Max(0.1f, instance.AttackSpeed);
            _activeSkillCooldown = instance.Definition.ActiveSkill != null
                ? instance.Definition.ActiveSkill.cooldown : 10f;
            _ultimateCooldown = instance.UltimateSkill != null ? instance.UltimateSkill.cooldown : 999f;
            ApplyTileBonus();
            _rangeIndicator = GetComponent<TowerRangeIndicator>();
            if (_rangeIndicator == null)
                _rangeIndicator = gameObject.AddComponent<TowerRangeIndicator>();
            _rangeIndicator.Configure(AttackRange, _projectileColor);
            ApplyLevelVisuals();

            _basePos = transform.position;
            _bobPhase = Random.value * Mathf.PI * 2f;
            EnsureShadow();
            EnsureAmbientGlow();
        }

        private void Update()
        {
            if (_dragonInstance == null) return;

            UpdateFake25D();

            EnemyBase target = FindNearestEnemy();
            if (target == null) return;

            if (Time.time - _lastAttackTime >= _attackCooldown)
            {
                FireAt(target);
                _lastAttackTime = Time.time;
            }

            if (_autoCastActiveSkill &&
                _dragonInstance.Definition.ActiveSkill != null &&
                Time.time - _lastActiveSkillTime >= _activeSkillCooldown)
            {
                TryCastActiveSkill(target);
            }

            if (_dragonInstance.UltimateSkill != null &&
                GameManager.Instance?.State == GameState.Wave &&
                Time.time - _lastUltimateCastTime >= _ultimateCooldown)
            {
                EnemyBase ultimateTarget = FindNearestEnemy();
                if (ultimateTarget != null)
                    TryCastUltimate(ultimateTarget);
            }
        }

        private EnemyBase FindNearestEnemy()
        {
            float range = AttackRange;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            EnemyBase nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < nearestDist) { nearestDist = dist; nearest = enemy; }
            }
            return nearest;
        }

        private void FireAt(EnemyBase target)
        {
            if (target != null)
            {
                Vector3 dir = target.transform.position - _basePos;
                dir.z = 0f;
                _attackLungeDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.zero;
                _attackLungeUntil = Time.time + 0.16f;
            }

            if (_projectilePrefab == null || _firePoint == null) return;

            float dmgMult = _dragonInstance.Definition.NormalAttack != null
                ? _dragonInstance.Definition.NormalAttack.GetDamageMultiplier(_dragonInstance.SkillLevel) : 1f;
            float elemMult = target.HasElement
                ? ElementInteraction.GetMultiplier(_dragonInstance.Definition.element, target.EnemyElement) : 1f;
            float damage = _dragonInstance.Attack * dmgMult * elemMult * _damageMultiplier * LevelDamageMultiplier;
            ShowElementFeedback(target, elemMult);
            BattleStatsTracker.Instance?.RecordDamage(DamageAttributionName, damage, IsFused);
            ApplyInheritedPassive(target);

            GameObject go = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);
            DamageSource source = _dragonInstance.Definition.element == DragonElement.Lightning
                ? DamageSource.LightningProjectile
                : DamageSource.Projectile;
            go.GetComponent<ProjectileBase>()?.Initialize(target.transform, damage, 10f, CurrentProjectileColor, source, _upgradeLevel);
            AudioManager.Instance?.PlaySfx(SfxKey.Attack);
            ApplyPassiveEffect(target);
        }

        private void ApplyPassiveEffect(EnemyBase target)
        {
            SkillDefinition passive = _dragonInstance?.Definition?.skillSet?.passiveSkill;
            if (passive == null || passive.passiveType == PassiveSkillType.None) return;
            if (target == null || target.IsDead) return;

            switch (passive.passiveType)
            {
                case PassiveSkillType.SlowOnHit:
                case PassiveSkillType.BurnOnHit:
                case PassiveSkillType.PoisonOnHit:
                    if (passive.statusEffects != null && passive.statusEffects.Length > 0)
                        target.ApplyStatusEffects(passive.statusEffects, _projectileColor);
                    break;

                case PassiveSkillType.StunOnHit:
                    if (Random.value <= passive.passiveChance)
                    {
                        float dur = passive.statusEffects != null && passive.statusEffects.Length > 0
                            ? passive.statusEffects[0].duration : 0.8f;
                        target.ApplyStun(dur);
                    }
                    break;

                case PassiveSkillType.AoeSplash:
                {
                    float splash = _dragonInstance.Attack * passive.passiveSplashPercent * _damageMultiplier * LevelDamageMultiplier;
                    foreach (Collider2D hit in Physics2D.OverlapCircleAll(target.transform.position, passive.aoeRadius))
                    {
                        EnemyBase nb = hit.GetComponent<EnemyBase>();
                        if (nb == null || nb == target || nb.IsDead) continue;
                        nb.TakeDamage(splash, _projectileColor, DamageSource.Skill);
                    }
                    break;
                }

                case PassiveSkillType.ChainLightning:
                {
                    float chain = _dragonInstance.Attack * passive.passiveSplashPercent * _damageMultiplier * LevelDamageMultiplier;
                    int arcs = 0;
                    foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, AttackRange))
                    {
                        if (arcs >= passive.passiveChainCount) break;
                        EnemyBase nb = hit.GetComponent<EnemyBase>();
                        if (nb == null || nb == target || nb.IsDead) continue;
                        nb.TakeDamage(chain, PrototypeBalance.LightningDamageColor, DamageSource.LightningProjectile);
                        DamageIndicator.SpawnText(nb.transform.position + Vector3.up * 0.65f, "CHAIN", PrototypeBalance.LightningDamageColor);
                        arcs++;
                    }
                    break;
                }
            }
        }

        private void ShowElementFeedback(EnemyBase target, float multiplier)
        {
            if (target == null) return;
            if (multiplier >= 1.25f)
                DamageIndicator.SpawnText(target.transform.position + Vector3.up * 1.25f, "WEAK", PrototypeBalance.WeakFeedbackColor);
            else if (multiplier <= 0.8f)
                DamageIndicator.SpawnText(target.transform.position + Vector3.up * 1.25f, "RESIST", PrototypeBalance.ResistFeedbackColor);
        }

        private void OnMouseDown()
        {
            TowerSelectionManager.Ensure().HandleTowerClicked(this);
        }

        private void EnsureClickableCollider()
        {
            if (GetComponent<Collider2D>() != null) return;

            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.1f, 1.1f);
        }

        public bool TryUpgrade()
        {
            if (_dragonInstance == null || ResourceManager.Instance == null) return false;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlanningPhase)
            {
                GameManager.Instance.ShowBattleMessage("Upgrade between waves");
                return false;
            }

            if (_upgradeLevel >= PrototypeBalance.MaxUpgradeLevel)
            {
                GameManager.Instance?.ShowBattleMessage($"{_dragonInstance.Definition.displayName} is max level");
                return false;
            }

            int cost = UpgradeCost;
            if (!ResourceManager.Instance.TrySpendGold(cost))
            {
                GameManager.Instance?.ShowBattleMessage($"Need {cost} gold to upgrade");
                return false;
            }

            _upgradeLevel++;
            _dragonInstance.SkillLevel = Mathf.Max(_dragonInstance.SkillLevel, _upgradeLevel);
            ApplyLevelVisuals();
            DamageIndicator.SpawnText(transform.position + Vector3.up * 1.25f, $"Lv {_upgradeLevel}", _projectileColor);
            AudioManager.Instance?.PlaySfx(SfxKey.Upgrade);
            GameManager.Instance?.ShowBattleMessage($"{_dragonInstance.Definition.displayName} upgraded to Lv {_upgradeLevel}");
            return true;
        }

        public bool CanMergeWith(DragonTower other, out string reason)
        {
            if (other == null || other == this)
            {
                reason = "Pick another matching dragon";
                return false;
            }

            if (IsFused)
            {
                reason = $"{DisplayName} is already fused";
                return false;
            }

            if (other.IsFused)
            {
                reason = "Cannot consume a fused tower";
                return false;
            }

            if (!IsReadyToFuse || !other.IsReadyToFuse)
            {
                reason = "Both dragons must be Lv3 to fuse";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryMergeWith(DragonTower other)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlanningPhase)
            {
                GameManager.Instance.ShowBattleMessage("Fuse between waves");
                return false;
            }

            if (!CanMergeWith(other, out string reason))
            {
                GameManager.Instance?.ShowBattleMessage(reason);
                return false;
            }

            _upgradeLevel = PrototypeBalance.FusedTowerLevel;
            _dragonInstance.SkillLevel = Mathf.Max(_dragonInstance.SkillLevel, _upgradeLevel);
            InheritPassiveFrom(other);
            ApplyLevelVisuals();
            SkillCastEffect.SpawnPulse(transform.position, _hasInheritedPassive ? 2.05f : 1.75f, PrototypeBalance.WeakFeedbackColor);
            DamageIndicator.SpawnText(transform.position + Vector3.up * 1.35f, _hasInheritedPassive ? $"FUSED +{_fusedPartnerName}" : "FUSED", PrototypeBalance.WeakFeedbackColor);
            other.RemoveMergedSource();
            string bonus = _hasInheritedPassive ? $" and inherited {_fusedPartnerName}'s passive" : string.Empty;
            BattleStatsTracker.Instance?.RecordFusion(DamageAttributionName, _hasInheritedPassive);
            GameManager.Instance?.ShowBattleMessage($"{DisplayName} fused{bonus} - stronger than two Lv3 towers");
            return true;
        }

        public string BuildFusionPreview(DragonTower other)
        {
            if (!CanMergeWith(other, out string reason))
                return reason;

            bool hybrid = !string.Equals(DragonId, other.DragonId, System.StringComparison.Ordinal);
            string resultType = hybrid ? "Hybrid Fused" : "Fused";
            string passive = hybrid ? $"{other.DisplayName} passive: {PassiveDescription(other._dragonInstance.Definition.element)}" : "Same dragon: pure power fusion";
            float power = hybrid ? PrototypeBalance.HybridFusionDamageMultiplier : PrototypeBalance.FusedTowerDamageMultiplier;
            return $"{resultType}: {DisplayName} + {other.DisplayName}\n{passive}\nPower: {power:0.00}x. Click again to confirm.";
        }

        public bool CanCastActiveSkill(out string reason)
        {
            if (_dragonInstance == null || ActiveSkill == null)
            {
                reason = "No active skill";
                return false;
            }

            float remaining = ActiveSkillCooldownRemaining;
            if (remaining > 0f)
            {
                reason = $"{ActiveSkill.displayName} ready in {Mathf.CeilToInt(remaining)}s";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryCastActiveSkill(EnemyBase target)
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Wave)
            {
                GameManager.Instance.ShowBattleMessage("Active skills are available during waves");
                return false;
            }

            if (!CanCastActiveSkill(out string reason))
            {
                GameManager.Instance?.ShowBattleMessage(reason);
                return false;
            }

            SkillDefinition skill = ActiveSkill;
            bool isFortify = (skill.skillId ?? string.Empty).ToLowerInvariant().Contains("fortify");
            if (!isFortify && (target == null || target.IsDead))
            {
                GameManager.Instance?.ShowBattleMessage("Pick an enemy target");
                return false;
            }

            AbilityExecutor.ExecuteActiveSkill(skill, _dragonInstance, target, transform.position, _damageMultiplier * LevelDamageMultiplier, _statusMagnitudeMultiplier);
            BattleStatsTracker.Instance?.RecordSkillCast();
            _lastActiveSkillTime = Time.time;
            return true;
        }

        public bool TryCastUltimate(EnemyBase target)
        {
            SkillDefinition ultimate = _dragonInstance?.UltimateSkill;
            if (ultimate == null) return false;
            if (target == null || target.IsDead) return false;
            if (GameManager.Instance?.State != GameState.Wave) return false;

            AbilityExecutor.ExecuteActiveSkill(ultimate, _dragonInstance, target, transform.position, _damageMultiplier * LevelDamageMultiplier, _statusMagnitudeMultiplier);
            BattleStatsTracker.Instance?.RecordSkillCast();
            _lastUltimateCastTime = Time.time;
            AudioManager.Instance?.PlaySfx(SfxKey.Ultimate);
            DamageIndicator.SpawnText(transform.position + Vector3.up * 1.55f, "ULTIMATE!", PrototypeBalance.WeakFeedbackColor);
            return true;
        }

        public void Sell()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlanningPhase)
            {
                GameManager.Instance.ShowBattleMessage("Sell between waves");
                return;
            }

            if (_placedTile != null)
                _placedTile.SetOccupied(false);
            ResourceManager.Instance?.AddMana(_manaRefund);
            AudioManager.Instance?.PlaySfx(SfxKey.TowerSell);
            BattleStatsTracker.Instance?.RecordManaRefunded(_manaRefund);
            GameManager.Instance?.ShowBattleMessage($"{DisplayName} sold: +{_manaRefund} MP");
            Destroy(gameObject);
        }

        private void RemoveMergedSource()
        {
            if (_placedTile != null)
                _placedTile.SetOccupied(false);
            Destroy(gameObject);
        }

        private void ApplyTileBonus()
        {
            _rangeBonus = 0f;
            _damageMultiplier = 1f;
            _statusMagnitudeMultiplier = 1f;

            if (_placedTile == null) return;

            switch (_placedTile.BonusType)
            {
                case TileBonusType.HighGround:
                    _rangeBonus = PrototypeBalance.HighGroundRangeBonus;
                    break;
                case TileBonusType.ManaCrystal:
                    _activeSkillCooldown *= PrototypeBalance.ManaCrystalCooldownMultiplier;
                    break;
                case TileBonusType.Scorched:
                    if (_dragonInstance.Definition.element == DragonElement.Fire)
                        _damageMultiplier = PrototypeBalance.ScorchedFireDamageMultiplier;
                    break;
                case TileBonusType.Frost:
                    _statusMagnitudeMultiplier = PrototypeBalance.FrostStatusMagnitudeMultiplier;
                    break;
            }

            if (_placedTile.BonusType != TileBonusType.None)
                GameManager.Instance?.ShowBattleMessage($"{DisplayName} gains {_placedTile.BonusName}: {_placedTile.BonusDescription}");
        }

        private void ApplyLevelVisuals()
        {
            if (_baseScale == Vector3.zero)
                _baseScale = transform.localScale;

            transform.localScale = _baseScale * (IsFused ? 1.48f : 1f + ((_upgradeLevel - 1) * 0.12f));
            _levelScale = transform.localScale;
            EnsureLevelBadge();
            UpdateLevelBadge();
            UpdateLevelAura();

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && _upgradeLevel > 1)
            {
                float tint = IsFused ? 0.5f : _upgradeLevel == 2 ? 0.18f : 0.32f;
                Color targetColor = IsFused ? PrototypeBalance.WeakFeedbackColor : _projectileColor;
                spriteRenderer.color = Color.Lerp(Color.white, targetColor, tint);
            }

            _rangeIndicator?.Configure(AttackRange, _projectileColor);
        }

        private void EnsureLevelBadge()
        {
            if (_levelBadge != null) return;

            var backgroundGO = new GameObject("LevelBadgeBackground");
            backgroundGO.transform.SetParent(transform, false);
            backgroundGO.transform.localPosition = new Vector3(0.48f, 0.72f, -0.03f);
            backgroundGO.transform.localScale = new Vector3(0.42f, 0.24f, 1f);
            _levelBadgeBackground = backgroundGO.AddComponent<SpriteRenderer>();
            _levelBadgeBackground.sprite = RuntimeWhiteSprite();
            _levelBadgeBackground.sortingOrder = 28;

            var badgeGO = new GameObject("LevelBadge");
            badgeGO.transform.SetParent(transform, false);
            badgeGO.transform.localPosition = new Vector3(0.48f, 0.72f, -0.08f);
            _levelBadge = badgeGO.AddComponent<TextMesh>();
            _levelBadge.anchor = TextAnchor.MiddleCenter;
            _levelBadge.alignment = TextAlignment.Center;
            _levelBadge.characterSize = 0.09f;
            _levelBadge.fontSize = 34;
            var renderer = badgeGO.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 29;
        }

        private void UpdateLevelBadge()
        {
            if (_levelBadge == null) return;

            _levelBadge.text = IsFused ? _hasInheritedPassive ? "HYBRID" : "FUSED" : $"Lv{_upgradeLevel}";
            _levelBadge.characterSize = IsFused ? 0.055f : 0.09f;
            _levelBadge.color = _upgradeLevel >= PrototypeBalance.MaxUpgradeLevel
                ? new Color(1f, 0.9f, 0.32f, 1f)
                : Color.white;
            if (_levelBadgeBackground != null)
            {
                _levelBadgeBackground.transform.localScale = IsFused
                    ? new Vector3(0.68f, 0.26f, 1f)
                    : new Vector3(0.42f, 0.24f, 1f);
                Color bg = _upgradeLevel switch
                {
                    1 => new Color(0f, 0f, 0f, 0.7f),
                    2 => new Color(_projectileColor.r, _projectileColor.g, _projectileColor.b, 0.82f),
                    3 => new Color(0.75f, 0.5f, 0.05f, 0.9f),
                    _ => _hasInheritedPassive ? CurrentProjectileColorWithAlpha(0.95f) : new Color(1f, 0.78f, 0.08f, 0.95f)
                };
                _levelBadgeBackground.color = bg;
            }
        }

        private void UpdateLevelAura()
        {
            if (_upgradeLevel <= 1)
            {
                if (_levelAura != null)
                    _levelAura.enabled = false;
                return;
            }

            EnsureLevelAura();
            Color color = IsFused && _hasInheritedPassive ? CurrentProjectileColor : _upgradeLevel >= PrototypeBalance.MaxUpgradeLevel ? PrototypeBalance.WeakFeedbackColor : _projectileColor;
            color.a = IsFused ? 1f : _upgradeLevel >= PrototypeBalance.MaxUpgradeLevel ? 0.92f : 0.72f;
            _levelAura.startColor = color;
            _levelAura.endColor = color;
            _levelAura.startWidth = IsFused ? 0.095f : _upgradeLevel >= PrototypeBalance.MaxUpgradeLevel ? 0.065f : 0.045f;
            _levelAura.endWidth = _levelAura.startWidth;
            _levelAura.enabled = true;
        }

        private void InheritPassiveFrom(DragonTower other)
        {
            _fusedPartnerId = other.DragonId;
            _fusedPartnerName = other.DisplayName;
            _fusedPartnerElement = other._dragonInstance.Definition.element;
            _hasInheritedPassive = !string.Equals(DragonId, _fusedPartnerId, System.StringComparison.Ordinal);
        }

        private void ApplyInheritedPassive(EnemyBase target)
        {
            if (!_hasInheritedPassive || target == null || target.IsDead) return;
            if (Time.time < _nextInheritedPassiveTime) return;

            StatusEffect passive = CreateInheritedPassiveEffect();
            if (passive == null) return;

            _nextInheritedPassiveTime = Time.time + PrototypeBalance.HybridPassiveCooldown;
            target.ApplyStatusEffect(passive, CurrentProjectileColor, 1f);
            DamageIndicator.SpawnText(target.transform.position + Vector3.up * 1.05f, PassiveLabel(), CurrentProjectileColor);
        }

        private StatusEffect CreateInheritedPassiveEffect()
        {
            return _fusedPartnerElement switch
            {
                DragonElement.Fire => new StatusEffect { effectId = "fusion_burn_passive", displayName = "Inherited Burn", duration = 2f, magnitude = 8f },
                DragonElement.Ice => new StatusEffect { effectId = "fusion_slow_passive", displayName = "Inherited Slow", duration = 1.8f, magnitude = 0.18f },
                DragonElement.Lightning => new StatusEffect { effectId = "fusion_shock_passive", displayName = "Inherited Shock", duration = 1.8f, magnitude = 0.16f },
                DragonElement.Shadow => new StatusEffect { effectId = "fusion_void_passive", displayName = "Inherited Void", duration = 2f, magnitude = 0.18f },
                DragonElement.Earth => new StatusEffect { effectId = "fusion_vulnerable_passive", displayName = "Inherited Crush", duration = 1.8f, magnitude = 0.14f },
                DragonElement.Light => new StatusEffect { effectId = "fusion_vulnerable_passive", displayName = "Inherited Radiance", duration = 1.8f, magnitude = 0.14f },
                DragonElement.Water => new StatusEffect { effectId = "fusion_slow_passive", displayName = "Inherited Drench", duration = 1.8f, magnitude = 0.16f },
                DragonElement.Wind => new StatusEffect { effectId = "fusion_slow_passive", displayName = "Inherited Gale", duration = 1.6f, magnitude = 0.15f },
                _ => null
            };
        }

        private static string PassiveDescription(DragonElement element)
        {
            return element switch
            {
                DragonElement.Fire => "burn ticks",
                DragonElement.Ice => "slow",
                DragonElement.Lightning => "shock vulnerability",
                DragonElement.Shadow => "void vulnerability",
                DragonElement.Earth => "crush vulnerability",
                DragonElement.Light => "radiant vulnerability",
                DragonElement.Water => "drench slow",
                DragonElement.Wind => "gale slow",
                _ => "bonus effect"
            };
        }

        private string PassiveLabel()
        {
            return _fusedPartnerElement switch
            {
                DragonElement.Fire => "BURN+",
                DragonElement.Ice => "SLOW+",
                DragonElement.Lightning => "SHOCK+",
                DragonElement.Shadow => "VOID+",
                DragonElement.Earth => "CRUSH+",
                DragonElement.Light => "LIGHT+",
                DragonElement.Water => "DRENCH+",
                DragonElement.Wind => "GALE+",
                _ => "PASSIVE+"
            };
        }

        private Color CurrentProjectileColor => _hasInheritedPassive
            ? Color.Lerp(_projectileColor, ElementColor(_fusedPartnerElement), 0.45f)
            : _projectileColor;

        private string DamageAttributionName => IsFused
            ? _hasInheritedPassive ? $"{DisplayName}+{_fusedPartnerName}" : $"{DisplayName} Fused"
            : DisplayName;

        private Color CurrentProjectileColorWithAlpha(float alpha)
        {
            Color color = CurrentProjectileColor;
            color.a = alpha;
            return color;
        }

        private static Color ElementColor(DragonElement element) => element switch
        {
            DragonElement.Fire => new Color(1f, 0.35f, 0.1f, 1f),
            DragonElement.Ice => new Color(0.3f, 0.85f, 1f, 1f),
            DragonElement.Lightning => new Color(1f, 0.82f, 0.15f, 1f),
            DragonElement.Earth => new Color(0.42f, 0.62f, 0.28f, 1f),
            DragonElement.Light => new Color(1f, 0.92f, 0.55f, 1f),
            DragonElement.Shadow => new Color(0.45f, 0.22f, 0.85f, 1f),
            DragonElement.Water => new Color(0.22f, 0.58f, 1f, 1f),
            DragonElement.Wind => new Color(0.62f, 1f, 0.72f, 1f),
            _ => Color.white
        };

        private float LevelDamageMultiplier => IsFused
            ? _hasInheritedPassive ? PrototypeBalance.HybridFusionDamageMultiplier : PrototypeBalance.FusedTowerDamageMultiplier
            : 1f;

        private void EnsureLevelAura()
        {
            if (_levelAura != null) return;

            var auraGO = new GameObject("LevelAura");
            auraGO.transform.SetParent(transform, false);
            _levelAura = auraGO.AddComponent<LineRenderer>();
            _levelAura.loop = true;
            _levelAura.useWorldSpace = false;
            _levelAura.positionCount = 72;
            _levelAura.sortingOrder = 16;
            _levelAura.material = new Material(Shader.Find("Sprites/Default"));
            float radius = 0.74f;
            for (int i = 0; i < _levelAura.positionCount; i++)
            {
                float radians = i / (float)_levelAura.positionCount * Mathf.PI * 2f;
                _levelAura.SetPosition(i, new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f));
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

        // ── Fake 2.5D motion ────────────────────────────────────────────────
        private void UpdateFake25D()
        {
            // Idle bob (vertical float) + brief attack lunge toward the last target.
            float bob = Mathf.Sin(Time.time * 2.6f + _bobPhase) * 0.06f;

            Vector3 lunge = Vector3.zero;
            if (Time.time < _attackLungeUntil)
            {
                float t = (_attackLungeUntil - Time.time) / 0.16f; // 1→0 over the lunge
                lunge = _attackLungeDir * (Mathf.Sin(t * Mathf.PI) * 0.13f);
            }

            transform.position = _basePos + new Vector3(lunge.x, bob + lunge.y * 0.5f, 0f);

            // Breathing scale on top of the current level scale.
            float breath = 1f + Mathf.Sin(Time.time * 2.6f + _bobPhase) * 0.03f;
            if (_levelScale != Vector3.zero)
                transform.localScale = _levelScale * breath;

            // Keep the shadow planted on the ground (counter the parent's bob/lunge),
            // and shrink it slightly as the dragon rises for a lift-off feel.
            if (_shadow != null)
            {
                float rise = bob + Mathf.Abs(lunge.y) * 0.5f;
                _shadow.position = new Vector3(_basePos.x + lunge.x * 0.4f, _basePos.y - 0.42f, _basePos.z + 0.05f);
                float shadowScale = Mathf.Clamp(1f - rise * 1.5f, 0.7f, 1.1f);
                _shadow.localScale = new Vector3(0.92f * shadowScale, 0.34f * shadowScale, 1f);
            }

            if (_ambientGlow != null && _ambientGlowRenderer != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 3.4f + _bobPhase) * 0.08f;
                float attackPop = Time.time < _attackLungeUntil ? 0.18f : 0f;
                float fusedBonus = IsFused ? 0.24f : _upgradeLevel >= PrototypeBalance.MaxUpgradeLevel ? 0.12f : 0f;
                _ambientGlow.position = new Vector3(_basePos.x, _basePos.y - 0.05f, _basePos.z + 0.02f);
                _ambientGlow.localScale = Vector3.one * (1.25f + fusedBonus + attackPop) * pulse;
                Color glow = CurrentProjectileColor;
                glow.a = IsFused ? 0.28f : 0.16f;
                _ambientGlowRenderer.color = glow;
            }
        }

        private void EnsureShadow()
        {
            if (_shadow != null) return;

            var shadowGO = new GameObject("TowerShadow");
            shadowGO.transform.SetParent(transform, false);
            var sr = shadowGO.AddComponent<SpriteRenderer>();
            sr.sprite = SoftShadowSprite();
            sr.color = new Color(0f, 0f, 0f, 0.42f);
            sr.sortingOrder = 0; // below the tower sprite (sortingOrder 2)
            _shadow = shadowGO.transform;
            _shadow.localScale = new Vector3(0.92f, 0.34f, 1f);
        }

        private void EnsureAmbientGlow()
        {
            if (_ambientGlow != null) return;

            var glowGO = new GameObject("TowerAmbientGlow");
            glowGO.transform.SetParent(transform, false);
            var sr = glowGO.AddComponent<SpriteRenderer>();
            sr.sprite = SoftShadowSprite();
            sr.color = CurrentProjectileColorWithAlpha(0.16f);
            sr.sortingOrder = 1;
            _ambientGlow = glowGO.transform;
            _ambientGlowRenderer = sr;
            _ambientGlow.localScale = Vector3.one * 1.25f;
        }

        private static Sprite _softShadowSprite;
        private static Sprite SoftShadowSprite()
        {
            if (_softShadowSprite != null) return _softShadowSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float a = Mathf.Clamp01(1f - d);
                a = a * a; // soft falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            _softShadowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _softShadowSprite;
        }

        private void OnDrawGizmosSelected()
        {
            if (_dragonInstance == null) return;
            float range = AttackRange;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);
            if (_dragonInstance.Definition.ActiveSkill is { isAoe: true } skill)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, skill.aoeRadius);
            }
        }
    }
}

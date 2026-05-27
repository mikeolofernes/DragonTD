using UnityEngine;

namespace DragonTD.TowerDefense
{
    public static class PrototypeBalance
    {
        public const int StartingMana = 300;
        public const int StartingGold = 130;
        public const int UpgradeBaseCost = 35;
        public const int MaxUpgradeLevel = 3;
        public const int FusedTowerLevel = 4;
        public const float FusedTowerDamageMultiplier = 2.35f;
        public const float HybridFusionDamageMultiplier = 3.1f;
        public const float HybridPassiveCooldown = 1.25f;
        public const float SellManaRefundPercent = 0.7f;

        public const float HighGroundRangeBonus = 1f;
        public const float ManaCrystalCooldownMultiplier = 0.75f;
        public const float ScorchedFireDamageMultiplier = 1.2f;
        public const float FrostStatusMagnitudeMultiplier = 1.25f;

        public const float ShieldProjectileMultiplier = 0.38f;
        public const float ShieldSkillMultiplier = 1.15f;
        public const float RegenPerSecond = 16f;
        public const float FlyingSlowMultiplier = 0.45f;

        public static readonly Color ProjectileDamageColor = new Color(1f, 0.95f, 0.58f, 1f);
        public static readonly Color LightningDamageColor = new Color(1f, 0.86f, 0.16f, 1f);
        public static readonly Color SkillDamageColor = new Color(0.45f, 0.9f, 1f, 1f);
        public static readonly Color BurnDamageColor = new Color(1f, 0.38f, 0.12f, 1f);
        public static readonly Color HealColor = new Color(0.38f, 1f, 0.48f, 1f);
        public static readonly Color WeakFeedbackColor = new Color(1f, 0.92f, 0.28f, 1f);
        public static readonly Color ResistFeedbackColor = new Color(0.62f, 0.78f, 1f, 1f);
        public static readonly Color ShieldBreakColor = new Color(0.55f, 0.95f, 1f, 1f);

        public static Color DamageColor(DamageSource source, Color fallback)
        {
            return source switch
            {
                DamageSource.LightningProjectile => LightningDamageColor,
                DamageSource.Skill => SkillDamageColor,
                DamageSource.Burn => BurnDamageColor,
                DamageSource.Projectile => ProjectileDamageColor,
                _ => fallback
            };
        }
    }
}

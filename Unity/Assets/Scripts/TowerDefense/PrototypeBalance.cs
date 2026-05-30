using UnityEngine;

namespace DragonTD.TowerDefense
{
    public static class PrototypeBalance
    {
        private static PrototypeBalanceConfig _config;

        private static PrototypeBalanceConfig Config
        {
            get
            {
                if (_config == null)
                    _config = Resources.Load<PrototypeBalanceConfig>("PrototypeBalanceConfig");
                return _config;
            }
        }

        public static int StartingMana => Config != null ? Config.startingMana : 320;
        public static int StartingGold => Config != null ? Config.startingGold : 150;
        public static int UpgradeBaseCost => Config != null ? Config.upgradeBaseCost : 30;
        public static float GlobalDamageBalance => Config != null ? Config.globalDamageBalance : 1.15f;
        public static int MaxUpgradeLevel => Config != null ? Config.maxUpgradeLevel : 3;
        public static int FusedTowerLevel => Config != null ? Config.fusedTowerLevel : 4;
        public static float FusedTowerDamageMultiplier => Config != null ? Config.fusedTowerDamageMultiplier : 2.35f;
        public static float HybridFusionDamageMultiplier => Config != null ? Config.hybridFusionDamageMultiplier : 3.1f;
        public static float HybridPassiveCooldown => Config != null ? Config.hybridPassiveCooldown : 1.25f;
        public static float SellManaRefundPercent => Config != null ? Config.sellManaRefundPercent : 0.7f;

        public static float HighGroundRangeBonus => Config != null ? Config.highGroundRangeBonus : 1f;
        public static float ManaCrystalCooldownMultiplier => Config != null ? Config.manaCrystalCooldownMultiplier : 0.75f;
        public static float ScorchedFireDamageMultiplier => Config != null ? Config.scorchedFireDamageMultiplier : 1.2f;
        public static float FrostStatusMagnitudeMultiplier => Config != null ? Config.frostStatusMagnitudeMultiplier : 1.25f;

        public static float ShieldProjectileMultiplier => Config != null ? Config.shieldProjectileMultiplier : 0.38f;
        public static float ShieldSkillMultiplier => Config != null ? Config.shieldSkillMultiplier : 1.15f;
        public static float RegenPerSecond => Config != null ? Config.regenPerSecond : 16f;
        public static float FlyingSlowMultiplier => Config != null ? Config.flyingSlowMultiplier : 0.45f;

        public static Color ProjectileDamageColor => Config != null ? Config.projectileDamageColor : new Color(1f, 0.95f, 0.58f, 1f);
        public static Color LightningDamageColor => Config != null ? Config.lightningDamageColor : new Color(1f, 0.86f, 0.16f, 1f);
        public static Color SkillDamageColor => Config != null ? Config.skillDamageColor : new Color(0.45f, 0.9f, 1f, 1f);
        public static Color BurnDamageColor => Config != null ? Config.burnDamageColor : new Color(1f, 0.38f, 0.12f, 1f);
        public static Color HealColor => Config != null ? Config.healColor : new Color(0.38f, 1f, 0.48f, 1f);
        public static Color WeakFeedbackColor => Config != null ? Config.weakFeedbackColor : new Color(1f, 0.92f, 0.28f, 1f);
        public static Color ResistFeedbackColor => Config != null ? Config.resistFeedbackColor : new Color(0.62f, 0.78f, 1f, 1f);
        public static Color ShieldBreakColor => Config != null ? Config.shieldBreakColor : new Color(0.55f, 0.95f, 1f, 1f);

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

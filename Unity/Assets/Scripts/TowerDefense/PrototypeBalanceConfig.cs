using UnityEngine;

namespace DragonTD.TowerDefense
{
    [CreateAssetMenu(fileName = "PrototypeBalanceConfig", menuName = "Dragon Dominion/Prototype Balance Config")]
    public class PrototypeBalanceConfig : ScriptableObject
    {
        [Header("Starting Resources")]
        public int startingMana = 320;
        public int startingGold = 150;

        [Header("Tower Progression")]
        public int upgradeBaseCost = 30;
        public int maxUpgradeLevel = 3;
        public int fusedTowerLevel = 4;
        public float fusedTowerDamageMultiplier = 2.35f;
        public float hybridFusionDamageMultiplier = 3.1f;
        public float hybridPassiveCooldown = 1.25f;
        public float sellManaRefundPercent = 0.7f;

        [Header("Global Balance")]
        public float globalDamageBalance = 1.15f;

        [Header("Tile Modifiers")]
        public float highGroundRangeBonus = 1f;
        public float manaCrystalCooldownMultiplier = 0.75f;
        public float scorchedFireDamageMultiplier = 1.2f;
        public float frostStatusMagnitudeMultiplier = 1.25f;

        [Header("Enemy Traits")]
        public float shieldProjectileMultiplier = 0.38f;
        public float shieldSkillMultiplier = 1.15f;
        public float regenPerSecond = 16f;
        public float flyingSlowMultiplier = 0.45f;

        [Header("Feedback Colors")]
        public Color projectileDamageColor = new Color(1f, 0.95f, 0.58f, 1f);
        public Color lightningDamageColor = new Color(1f, 0.86f, 0.16f, 1f);
        public Color skillDamageColor = new Color(0.45f, 0.9f, 1f, 1f);
        public Color burnDamageColor = new Color(1f, 0.38f, 0.12f, 1f);
        public Color healColor = new Color(0.38f, 1f, 0.48f, 1f);
        public Color weakFeedbackColor = new Color(1f, 0.92f, 0.28f, 1f);
        public Color resistFeedbackColor = new Color(0.62f, 0.78f, 1f, 1f);
        public Color shieldBreakColor = new Color(0.55f, 0.95f, 1f, 1f);
    }
}

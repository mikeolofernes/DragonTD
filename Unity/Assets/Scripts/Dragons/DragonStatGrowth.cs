using UnityEngine;

namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonStatGrowth
    {
        public AnimationCurve hpCurve;       // x=level(0-100), y=multiplier
        public AnimationCurve attackCurve;
        public AnimationCurve armorCurve;
        // Common=1.0, Uncommon=1.2, Rare=1.5, Epic=1.8, Legendary=2.2, Mythic=2.5, Ancient=3.0
        public float rarityMultiplier = 1f;
        // Earth HP bonus, Flame ATK bonus, etc. applied on top of rarity
        public float classMultiplier = 1f;
    }
}

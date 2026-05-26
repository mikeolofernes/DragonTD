using UnityEngine;
using DragonTD.Dragons;

namespace DragonTD.Summoning
{
    [System.Serializable]
    public class RarityRate
    {
        public DragonRarity Rarity;
        [Range(0f, 1f)] public float Rate;
    }

    [System.Serializable]
    public class DragonWeight
    {
        public DragonDefinition Dragon;
        public int Weight;
    }

    [CreateAssetMenu(fileName = "NewSummonPool", menuName = "Dragon Dominion/Summon Pool")]
    public class SummonPool : ScriptableObject
    {
        public string BannerName;

        // Default rates: Common=0.40, Uncommon=0.30, Rare=0.20, Epic=0.07, Legendary=0.025, Mythic=0.005
        public RarityRate[] RarityRates = new RarityRate[]
        {
            new RarityRate { Rarity = DragonRarity.Common,    Rate = 0.400f },
            new RarityRate { Rarity = DragonRarity.Uncommon,  Rate = 0.300f },
            new RarityRate { Rarity = DragonRarity.Rare,      Rate = 0.200f },
            new RarityRate { Rarity = DragonRarity.Epic,      Rate = 0.070f },
            new RarityRate { Rarity = DragonRarity.Legendary, Rate = 0.025f },
            new RarityRate { Rarity = DragonRarity.Mythic,    Rate = 0.005f },
        };

        public DragonWeight[] AvailableDragons;

        public int SummonCostGems = 300;
        public int TenPullCostGems = 2700;

        public bool HasRateUp;
        public DragonDefinition RateUpDragon;
    }
}

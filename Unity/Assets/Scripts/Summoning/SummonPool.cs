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
        public DragonData Dragon;
        public int Weight;
    }

    [CreateAssetMenu(fileName = "NewSummonPool", menuName = "DragonTD/SummonPool")]
    public class SummonPool : ScriptableObject
    {
        public string BannerName;

        // Default rates: C=0.40, B=0.30, A=0.20, S=0.07, SS=0.025, SSS=0.005
        public RarityRate[] RarityRates = new RarityRate[]
        {
            new RarityRate { Rarity = DragonRarity.C,   Rate = 0.400f },
            new RarityRate { Rarity = DragonRarity.B,   Rate = 0.300f },
            new RarityRate { Rarity = DragonRarity.A,   Rate = 0.200f },
            new RarityRate { Rarity = DragonRarity.S,   Rate = 0.070f },
            new RarityRate { Rarity = DragonRarity.SS,  Rate = 0.025f },
            new RarityRate { Rarity = DragonRarity.SSS, Rate = 0.005f },
        };

        public DragonWeight[] AvailableDragons;

        public int SummonCostGems = 300;
        public int TenPullCostGems = 2700;

        public bool HasRateUp;
        public DragonData RateUpDragon;
    }
}

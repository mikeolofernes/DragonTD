using System.Collections.Generic;
using DragonTD.Dragons;
using UnityEngine;

namespace DragonTD.Summoning
{
    [System.Serializable]
    public class PityTracker
    {
        public int PullsSinceLastEpic = 0;
        public int TotalPulls = 0;
    }

    public class GachaSystem
    {
        private Dictionary<string, PityTracker> _pity = new Dictionary<string, PityTracker>();

        public DragonDefinition SinglePull(SummonPool pool)
        {
            if (!_pity.TryGetValue(pool.BannerName, out PityTracker tracker))
            {
                tracker = new PityTracker();
                _pity[pool.BannerName] = tracker;
            }

            tracker.PullsSinceLastEpic++;
            tracker.TotalPulls++;

            DragonRarity rarity;

            // Hard pity at 100: force Mythic
            if (tracker.PullsSinceLastEpic >= 100)
            {
                rarity = DragonRarity.Mythic;
            }
            // Soft pity at 50+: triple Epic+ rates
            else
            {
                bool softPity = tracker.PullsSinceLastEpic >= 50;
                rarity = RollRarity(pool, softPity);
            }

            // Reset pity counter if Epic or above
            if (rarity >= DragonRarity.Epic)
            {
                tracker.PullsSinceLastEpic = 0;
            }

            return PickDragonOfRarity(pool, rarity);
        }

        public DragonDefinition[] TenPull(SummonPool pool)
        {
            DragonDefinition[] results = new DragonDefinition[10];
            bool hasRareOrAbove = false;

            for (int i = 0; i < 9; i++)
            {
                results[i] = SinglePull(pool);
                if (results[i] != null && results[i].rarity >= DragonRarity.Rare)
                    hasRareOrAbove = true;
            }

            // 10th pull: guarantee at least Rare if none found yet
            if (!hasRareOrAbove)
            {
                if (!_pity.TryGetValue(pool.BannerName, out PityTracker tracker))
                {
                    tracker = new PityTracker();
                    _pity[pool.BannerName] = tracker;
                }
                tracker.PullsSinceLastEpic++;
                tracker.TotalPulls++;

                DragonRarity rarity = RollRarity(pool, softPity: true);
                if (rarity < DragonRarity.Rare)
                    rarity = DragonRarity.Rare;

                if (rarity >= DragonRarity.Epic)
                    tracker.PullsSinceLastEpic = 0;

                results[9] = PickDragonOfRarity(pool, rarity);
            }
            else
            {
                results[9] = SinglePull(pool);
            }

            return results;
        }

        private DragonRarity RollRarity(SummonPool pool, bool softPity)
        {
            float totalWeight = 0f;
            float[] rates = new float[pool.RarityRates.Length];

            for (int i = 0; i < pool.RarityRates.Length; i++)
            {
                float rate = pool.RarityRates[i].Rate;
                if (softPity && pool.RarityRates[i].Rarity >= DragonRarity.Epic)
                    rate *= 3f;
                rates[i] = rate;
                totalWeight += rate;
            }

            float roll = Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < pool.RarityRates.Length; i++)
            {
                cumulative += rates[i];
                if (roll <= cumulative)
                    return pool.RarityRates[i].Rarity;
            }

            return pool.RarityRates[0].Rarity;
        }

        private DragonDefinition PickDragonOfRarity(SummonPool pool, DragonRarity rarity)
        {
            // Rate-up: 50% chance to return rate-up dragon if rarity matches
            if (pool.HasRateUp && pool.RateUpDragon != null && pool.RateUpDragon.rarity == rarity)
            {
                if (Random.value < 0.5f)
                    return pool.RateUpDragon;
            }

            List<DragonWeight> matching = new List<DragonWeight>();
            int totalWeight = 0;

            foreach (DragonWeight dw in pool.AvailableDragons)
            {
                if (dw.Dragon != null && dw.Dragon.rarity == rarity)
                {
                    matching.Add(dw);
                    totalWeight += dw.Weight;
                }
            }

            if (matching.Count > 0 && totalWeight > 0)
            {
                int roll = Random.Range(0, totalWeight);
                int cumulative = 0;
                foreach (DragonWeight dw in matching)
                {
                    cumulative += dw.Weight;
                    if (roll < cumulative)
                        return dw.Dragon;
                }
            }

            // Fallback: any dragon in pool
            if (pool.AvailableDragons != null && pool.AvailableDragons.Length > 0)
                return pool.AvailableDragons[Random.Range(0, pool.AvailableDragons.Length)].Dragon;

            return null;
        }

        public PityTracker GetPity(string bannerName)
        {
            if (_pity.TryGetValue(bannerName, out PityTracker tracker))
                return tracker;
            return new PityTracker();
        }

        public void RestorePity(string bannerName, int pullsSinceLastEpic, int totalPulls)
        {
            if (!_pity.TryGetValue(bannerName, out PityTracker tracker))
            {
                tracker = new PityTracker();
                _pity[bannerName] = tracker;
            }
            tracker.PullsSinceLastEpic = Mathf.Max(0, pullsSinceLastEpic);
            tracker.TotalPulls         = Mathf.Max(0, totalPulls);
        }
    }
}

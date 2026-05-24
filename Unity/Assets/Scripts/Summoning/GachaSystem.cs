using System.Collections.Generic;
using DragonTD.Dragons;
using UnityEngine;

namespace DragonTD.Summoning
{
    [System.Serializable]
    public class PityTracker
    {
        public int PullsSinceLastS = 0;
        public int TotalPulls = 0;
    }

    public class GachaSystem
    {
        private Dictionary<string, PityTracker> _pity = new Dictionary<string, PityTracker>();

        public DragonData SinglePull(SummonPool pool)
        {
            if (!_pity.TryGetValue(pool.BannerName, out PityTracker tracker))
            {
                tracker = new PityTracker();
                _pity[pool.BannerName] = tracker;
            }

            tracker.PullsSinceLastS++;
            tracker.TotalPulls++;

            DragonRarity rarity;

            // Hard pity at 100: force SSS
            if (tracker.PullsSinceLastS >= 100)
            {
                rarity = DragonRarity.SSS;
            }
            // Soft pity at 50+: boost S and above rates
            else
            {
                bool softPity = tracker.PullsSinceLastS >= 50;
                rarity = RollRarity(pool, softPity);
            }

            // Reset pity counter if S or above was rolled
            if (rarity >= DragonRarity.S)
            {
                tracker.PullsSinceLastS = 0;
            }

            return PickDragonOfRarity(pool, rarity);
        }

        public DragonData[] TenPull(SummonPool pool)
        {
            DragonData[] results = new DragonData[10];
            bool hasAOrAbove = false;

            for (int i = 0; i < 9; i++)
            {
                results[i] = SinglePull(pool);
                if (results[i].Rarity >= DragonRarity.A)
                {
                    hasAOrAbove = true;
                }
            }

            // 10th pull: guarantee at least A-rarity if none found yet
            if (!hasAOrAbove)
            {
                if (!_pity.TryGetValue(pool.BannerName, out PityTracker tracker))
                {
                    tracker = new PityTracker();
                    _pity[pool.BannerName] = tracker;
                }
                tracker.PullsSinceLastS++;
                tracker.TotalPulls++;

                // Force A or above for guaranteed pull
                DragonRarity rarity = RollRarity(pool, softPity: true);
                if (rarity < DragonRarity.A)
                {
                    rarity = DragonRarity.A;
                }

                if (rarity >= DragonRarity.S)
                {
                    tracker.PullsSinceLastS = 0;
                }

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
                // Apply soft pity boost to S, SS, SSS
                if (softPity && pool.RarityRates[i].Rarity >= DragonRarity.S)
                {
                    rate *= 3f;
                }
                rates[i] = rate;
                totalWeight += rate;
            }

            float roll = Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < pool.RarityRates.Length; i++)
            {
                cumulative += rates[i];
                if (roll <= cumulative)
                {
                    return pool.RarityRates[i].Rarity;
                }
            }

            // Fallback to lowest rarity
            return pool.RarityRates[0].Rarity;
        }

        private DragonData PickDragonOfRarity(SummonPool pool, DragonRarity rarity)
        {
            // Handle rate-up: 50% chance to return rate-up dragon if rarity matches
            if (pool.HasRateUp && pool.RateUpDragon != null && pool.RateUpDragon.Rarity == rarity)
            {
                if (Random.value < 0.5f)
                {
                    return pool.RateUpDragon;
                }
            }

            // Collect matching dragons with weights
            List<DragonWeight> matching = new List<DragonWeight>();
            int totalWeight = 0;

            foreach (DragonWeight dw in pool.AvailableDragons)
            {
                if (dw.Dragon != null && dw.Dragon.Rarity == rarity)
                {
                    matching.Add(dw);
                    totalWeight += dw.Weight;
                }
            }

            // Weighted random pick from matching dragons
            if (matching.Count > 0 && totalWeight > 0)
            {
                int roll = Random.Range(0, totalWeight);
                int cumulative = 0;
                foreach (DragonWeight dw in matching)
                {
                    cumulative += dw.Weight;
                    if (roll < cumulative)
                    {
                        return dw.Dragon;
                    }
                }
            }

            // Fallback: return any dragon if none match the rarity
            if (pool.AvailableDragons != null && pool.AvailableDragons.Length > 0)
            {
                return pool.AvailableDragons[Random.Range(0, pool.AvailableDragons.Length)].Dragon;
            }

            return null;
        }

        public PityTracker GetPity(string bannerName)
        {
            if (_pity.TryGetValue(bannerName, out PityTracker tracker))
            {
                return tracker;
            }
            return new PityTracker();
        }
    }
}

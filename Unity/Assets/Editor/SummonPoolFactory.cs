#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DragonTD.Dragons;
using DragonTD.Summoning;

namespace DragonTD.Editor
{
    public static class SummonPoolFactory
    {
        private const string ScPath = "Assets/ScriptableObjects";

        [MenuItem("DragonTD/Create Default Summon Pool")]
        public static void CreateDefaultPool()
        {
            var pool = ScriptableObject.CreateInstance<SummonPool>();
            pool.BannerName      = "Standard Banner";
            pool.SummonCostGems  = 300;
            pool.TenPullCostGems = 2700;

            pool.RarityRates = new RarityRate[]
            {
                new RarityRate { Rarity = DragonRarity.Common,    Rate = 0.400f },
                new RarityRate { Rarity = DragonRarity.Uncommon,  Rate = 0.300f },
                new RarityRate { Rarity = DragonRarity.Rare,      Rate = 0.200f },
                new RarityRate { Rarity = DragonRarity.Epic,      Rate = 0.070f },
                new RarityRate { Rarity = DragonRarity.Legendary, Rate = 0.025f },
                new RarityRate { Rarity = DragonRarity.Mythic,    Rate = 0.005f },
            };

            // Build DragonWeight entries from all Phase 1 dragons
            var weights = new System.Collections.Generic.List<DragonWeight>();
            foreach (var d in Phase1DragonData.All)
            {
                string path = $"Assets/ScriptableObjects/Dragons/{GetNameById(d.Id)}.asset";
                var def = AssetDatabase.LoadAssetAtPath<DragonDefinition>(path);
                if (def == null)
                {
                    Debug.LogWarning($"[SummonPoolFactory] Dragon not found at {path} — run Create Phase 1 Dragons first.");
                    continue;
                }
                int weight = d.Rarity switch
                {
                    DragonRarity.Legendary => 5,
                    DragonRarity.Epic      => 15,
                    DragonRarity.Rare      => 30,
                    _                      => 50
                };
                weights.Add(new DragonWeight { Dragon = def, Weight = weight });
            }
            pool.AvailableDragons = weights.ToArray();

            string poolPath = $"{ScPath}/StandardBanner.asset";
            AssetDatabase.CreateAsset(pool, poolPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DragonTD] Summon pool created at {poolPath}");
        }

        [MenuItem("DragonTD/Create DragonRegistry")]
        public static void CreateRegistry()
        {
            var registry = ScriptableObject.CreateInstance<DragonRegistry>();

            var defs = new System.Collections.Generic.List<DragonDefinition>();
            foreach (var d in Phase1DragonData.All)
            {
                string path = $"Assets/ScriptableObjects/Dragons/{GetNameById(d.Id)}.asset";
                var def = AssetDatabase.LoadAssetAtPath<DragonDefinition>(path);
                if (def != null) defs.Add(def);
            }
            registry.allDragons = defs.ToArray();

            string regPath = $"{ScPath}/DragonRegistry.asset";
            AssetDatabase.CreateAsset(registry, regPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DragonTD] DragonRegistry created at {regPath} with {defs.Count} dragons.");
        }

        private static string GetNameById(string id) => id switch
        {
            "voltaris_001"        => "Voltaris",
            "frostfang_002"       => "Frostfang",
            "magmaclaw_003"       => "Magmaclaw",
            "tempest_glacion_004" => "Tempest Glacion",
            "stonehide_005"       => "Stonehide",
            "celestara_006"       => "Celestara",
            "shadowfang_007"      => "Shadowfang",
            _                     => id
        };
    }
}
#endif

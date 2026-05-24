#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using DragonTD.TowerDefense;

namespace DragonTD.Editor
{
    public static class WaveAssetFactory
    {
        private const string BasePath = "Assets/ScriptableObjects/Waves";

        [MenuItem("DragonTD/Create Phase 1 Waves")]
        public static void CreatePhase1Waves()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Waves"));

            // Wave 1 — Tutorial trickle
            MakeWave("Wave_01_Tutorial",
                gold: 50, mana: 20, groupGap: 0f,
                (5, 1.5f));

            // Wave 2 — Sustained push
            MakeWave("Wave_02_Push",
                gold: 70, mana: 25, groupGap: 3f,
                (8, 1.2f));

            // Wave 3 — Two squads
            MakeWave("Wave_03_TwoSquads",
                gold: 90, mana: 30, groupGap: 4f,
                (6, 1.0f), (6, 1.0f));

            // Wave 4 — Mixed (fast + slow)
            MakeWave("Wave_04_Mixed",
                gold: 110, mana: 35, groupGap: 5f,
                (4, 2.0f), (10, 0.7f));

            // Wave 5 — Boss rush
            MakeWave("Wave_05_BossRush",
                gold: 150, mana: 50, groupGap: 4f,
                (12, 0.7f), (6, 1.5f), (3, 2.5f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DragonTD] Phase 1 waves created at {BasePath}.\n" +
                      "Assign enemy prefabs to each EnemyGroup in the Inspector, then add all 5 waves to WaveManager.");
        }

        private static void MakeWave(string assetName, int gold, int mana, float groupGap,
            params (int count, float interval)[] groups)
        {
            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.GoldReward       = gold;
            wave.ManaReward       = mana;
            wave.TimeBetweenGroups = groupGap;
            wave.EnemyGroups      = new EnemySpawnEntry[groups.Length];

            for (int i = 0; i < groups.Length; i++)
            {
                wave.EnemyGroups[i] = new EnemySpawnEntry
                {
                    Count          = groups[i].count,
                    SpawnInterval  = groups[i].interval,
                    EnemyPrefab    = null   // assign in Inspector
                };
            }

            AssetDatabase.CreateAsset(wave, $"{BasePath}/{assetName}.asset");
        }
    }
}
#endif

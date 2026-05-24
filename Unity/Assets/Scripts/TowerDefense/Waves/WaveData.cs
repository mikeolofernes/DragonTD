using UnityEngine;

namespace DragonTD.TowerDefense
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject EnemyPrefab;
        public int Count;
        public float SpawnInterval;
    }

    [CreateAssetMenu(fileName = "NewWave", menuName = "DragonTD/Wave")]
    public class WaveData : ScriptableObject
    {
        public EnemySpawnEntry[] EnemyGroups;
        public float TimeBetweenGroups = 3f;
        public int GoldReward = 50;
        public int ManaReward = 20;
    }
}

using UnityEngine;
using System.Collections;

namespace DragonTD.TowerDefense
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [SerializeField] private WaveData[] _waves;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private Transform[] _waypoints;

        public int TotalWaves => _waves.Length;
        public int ActiveEnemyCount { get; private set; }
        public Transform[] Waypoints => _waypoints;

        public event System.Action OnWaveComplete;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartWave(int waveIndex)
        {
            StartCoroutine(SpawnWave(waveIndex));
        }

        private IEnumerator SpawnWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _waves.Length)
                yield break;

            WaveData wave = _waves[waveIndex];

            foreach (EnemySpawnEntry group in wave.EnemyGroups)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    GameObject enemyGO = Instantiate(group.EnemyPrefab, _spawnPoints[0].position, Quaternion.identity);
                    EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.Initialize(_waypoints);
                        ActiveEnemyCount++;
                    }

                    yield return new WaitForSeconds(group.SpawnInterval);
                }

                yield return new WaitForSeconds(wave.TimeBetweenGroups);
            }
        }

        public void OnEnemyDied()
        {
            ActiveEnemyCount--;
            if (ActiveEnemyCount <= 0)
            {
                ActiveEnemyCount = 0;
                OnWaveComplete?.Invoke();
                GameManager.Instance.OnWaveCleared();
            }
        }
    }
}

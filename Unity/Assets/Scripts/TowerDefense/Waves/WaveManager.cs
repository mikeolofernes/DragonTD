using UnityEngine;
using System.Collections;
using DragonTD.Core;

namespace DragonTD.TowerDefense
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [SerializeField] private WaveData[] _waves;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private GameObject _eliteEnemyPrefab;

        public int TotalWaves => _waves.Length;
        public int ActiveEnemyCount { get; private set; }
        public Transform[] Waypoints => _waypoints;

        public event System.Action OnWaveComplete;

        private WaveData _activeWave;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Wave)
                StartCoroutine(SpawnWave(GameManager.Instance.CurrentWave - 1));
        }

        private IEnumerator SpawnWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _waves.Length) yield break;

            _activeWave = _waves[waveIndex];

            // Pre-count so deaths during spawning don't prematurely trigger wave complete
            int totalToSpawn = 0;
            foreach (EnemySpawnEntry group in _activeWave.EnemyGroups)
                totalToSpawn += group.Count;
            ActiveEnemyCount = totalToSpawn;

            foreach (EnemySpawnEntry group in _activeWave.EnemyGroups)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    if (group.EnemyPrefab == null)
                    {
                        ActiveEnemyCount--;
                    }
                    else
                    {
                        GameObject enemyGO = Instantiate(group.EnemyPrefab, _spawnPoints[0].position, Quaternion.identity);
                        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
                        if (enemy != null)
                        {
                            enemy.Initialize(_waypoints);
                            GameDirector.Instance?.OnEnemySpawned(enemy);
                        }
                        else
                        {
                            ActiveEnemyCount--;
                        }
                    }
                    yield return new WaitForSeconds(group.SpawnInterval);
                }
                yield return new WaitForSeconds(_activeWave.TimeBetweenGroups);
            }
        }

        public void OnEnemyDied()
        {
            ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
            if (ActiveEnemyCount > 0) return;

            OnWaveComplete?.Invoke();

            if (_activeWave != null)
            {
                ResourceManager.Instance.AddGold(_activeWave.GoldReward);
                ResourceManager.Instance.AddMana(_activeWave.ManaReward);
            }

            GameManager.Instance.OnWaveCleared();
        }

        public void SpawnEliteEnemy(float statMultiplier)
        {
            if (_eliteEnemyPrefab == null || _spawnPoints.Length == 0) return;

            GameObject enemyGO = Instantiate(_eliteEnemyPrefab, _spawnPoints[0].position, Quaternion.identity);
            EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
            if (enemy == null) return;

            enemy.Initialize(_waypoints);
            enemy.ApplyDifficultyMultiplier(statMultiplier);
            ActiveEnemyCount++;
        }
    }
}

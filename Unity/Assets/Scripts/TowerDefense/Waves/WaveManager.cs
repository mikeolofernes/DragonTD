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
        public WaveData GetWaveData(int waveNumber)
        {
            int index = waveNumber - 1;
            if (_waves == null || index < 0 || index >= _waves.Length)
                return null;
            return _waves[index];
        }

        public event System.Action OnWaveComplete;

        private WaveData _activeWave;
        private Coroutine _spawnRoutine;
        private bool _isWaveActive;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (ChapterContent.Active != null && ChapterContent.Active.waves != null && ChapterContent.Active.waves.Length > 0)
                _waves = ChapterContent.Active.waves;
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
            {
                if (!_isWaveActive && _spawnRoutine == null)
                    TryStartWave(GameManager.Instance.CurrentWave);
            }
        }

        public bool TryStartWave(int waveNumber)
        {
            if (_isWaveActive || _spawnRoutine != null)
            {
                Debug.LogWarning($"[WaveManager] Cannot start wave {waveNumber}; wave already active.");
                return false;
            }

            int waveIndex = waveNumber - 1;
            if (_waves == null || waveIndex < 0 || waveIndex >= _waves.Length)
            {
                Debug.LogWarning($"[WaveManager] Cannot start wave {waveNumber}; only {_waves?.Length ?? 0} waves are assigned.");
                return false;
            }

            if (_waves[waveIndex] == null)
            {
                Debug.LogWarning($"[WaveManager] Cannot start wave {waveNumber}; wave asset is missing.");
                return false;
            }

            _spawnRoutine = StartCoroutine(SpawnWave(waveIndex));
            Debug.Log($"[WaveManager] Started wave {waveNumber}.");
            return true;
        }

        private IEnumerator SpawnWave(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= _waves.Length)
            {
                _spawnRoutine = null;
                yield break;
            }

            _activeWave = _waves[waveIndex];
            _isWaveActive = true;

            // Pre-count so deaths during spawning don't prematurely trigger wave complete
            int totalToSpawn = 0;
            if (_activeWave != null && _activeWave.EnemyGroups != null)
            {
                foreach (EnemySpawnEntry group in _activeWave.EnemyGroups)
                    if (group.EnemyPrefab != null && group.Count > 0)
                        totalToSpawn += Mathf.Max(0, group.Count + (GameManager.Instance?.CurrentStage?.extraEnemiesPerGroup ?? 0));
            }
            ActiveEnemyCount = totalToSpawn;

            if (ActiveEnemyCount == 0 || _spawnPoints == null || _spawnPoints.Length == 0)
            {
                _spawnRoutine = null;
                yield return null;
                CompleteWave();
                yield break;
            }

            foreach (EnemySpawnEntry group in _activeWave.EnemyGroups)
            {
                StageDefinition stage = GameManager.Instance?.CurrentStage;
                int spawnCount = Mathf.Max(0, group.Count + (stage?.extraEnemiesPerGroup ?? 0));
                float spawnInterval = group.SpawnInterval / Mathf.Max(0.1f, stage?.spawnRateMultiplier ?? 1f);
                for (int i = 0; i < spawnCount; i++)
                {
                    if (group.EnemyPrefab == null)
                    {
                        continue;
                    }
                    else
                    {
                        GameObject enemyGO = Instantiate(group.EnemyPrefab, _spawnPoints[0].position, Quaternion.identity);
                        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
                        if (enemy != null)
                        {
                            enemy.Initialize(_waypoints);
                            enemy.ApplyDifficultyMultiplier(GameManager.Instance?.StageDifficultyMultiplier ?? 1f);
                            GameDirector.Instance?.OnEnemySpawned(enemy);
                        }
                        else
                        {
                            ActiveEnemyCount--;
                        }
                    }
                    yield return new WaitForSeconds(spawnInterval);
                }
                yield return new WaitForSeconds(_activeWave.TimeBetweenGroups);
            }

            _spawnRoutine = null;
        }

        public void OnEnemyDied()
        {
            ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
            if (ActiveEnemyCount > 0) return;

            CompleteWave();
        }

        public void ResetForBattle()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            _activeWave = null;
            _isWaveActive = false;
            ActiveEnemyCount = 0;
        }

        private void CompleteWave()
        {
            if (!_isWaveActive) return;
            _isWaveActive = false;
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            OnWaveComplete?.Invoke();
            AudioManager.Instance?.PlaySfx(SfxKey.WaveEnd);

            if (_activeWave != null)
            {
                float rewardMultiplier = GameManager.Instance?.StageRewardMultiplier ?? 1f;
                int goldReward = Mathf.RoundToInt(_activeWave.GoldReward * Mathf.Max(1f, rewardMultiplier));
                int manaReward = Mathf.RoundToInt(_activeWave.ManaReward * Mathf.Max(1f, rewardMultiplier));
                BattleStatsTracker.Instance?.RecordReward(goldReward, manaReward);
                ResourceManager.Instance.AddGold(goldReward);
                ResourceManager.Instance.AddMana(manaReward);
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

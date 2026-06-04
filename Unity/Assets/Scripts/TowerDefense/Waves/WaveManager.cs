using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DragonTD.Core;

namespace DragonTD.TowerDefense
{
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [SerializeField] private WaveData[] _waves;
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private WaypointPathAuthoring _authoredPath;
        [SerializeField] private GameObject _eliteEnemyPrefab;

        [Header("Path Visual Alignment")]
        [SerializeField] private Vector2 _pathEnemyOffset = Vector2.zero;
        [SerializeField] private Vector2 _laneEnemyOffset = Vector2.zero;
        [SerializeField] private float _spawnedEnemyVisualScale = 0.75f;

        [Header("Lane Defense")]
        [SerializeField] private float _laneLeftEdgeX  = -5.5f;
        [SerializeField] private float _laneRowHeight  = 1f;
        [SerializeField] private int   _laneRowCount   = 8;
        private float _wallWorldX;
        private int[] _laneRows;
        private Transform _runtimeWaypointRoot;

        private bool IsLaneMode => ChapterContent.Active != null &&
                                   ChapterContent.Active.map != null &&
                                   ChapterContent.Active.map.mapType == DragonTD.Core.MapType.LaneDefense;

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
            ResolvePathWaypointsFromActiveMap();
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
            ResolvePathWaypointsFromActiveMap();

            // Pre-count so deaths during spawning don't prematurely trigger wave complete
            int totalToSpawn = 0;
            if (_activeWave != null && _activeWave.EnemyGroups != null)
            {
                foreach (EnemySpawnEntry group in _activeWave.EnemyGroups)
                    if (group.EnemyPrefab != null && group.Count > 0)
                        totalToSpawn += Mathf.Max(0, group.Count + (GameManager.Instance?.CurrentStage?.extraEnemiesPerGroup ?? 0));
            }
            ActiveEnemyCount = totalToSpawn;

            if (ActiveEnemyCount == 0 || (!IsLaneMode && (_spawnPoints == null || _spawnPoints.Length == 0)))
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
                    else if (IsLaneMode)
                    {
                        Vector3 spawnPos = ResolveLaneSpawnPosition();
                        GameObject enemyGO = Instantiate(group.EnemyPrefab, spawnPos, Quaternion.identity);
                        ApplySpawnPresentation(enemyGO);
                        EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
                        if (enemy != null)
                        {
                            enemy.InitializeLane(_wallWorldX);
                            enemy.ApplyDifficultyMultiplier(GameManager.Instance?.StageDifficultyMultiplier ?? 1f);
                            GameDirector.Instance?.OnEnemySpawned(enemy);
                        }
                        else
                        {
                            ActiveEnemyCount--;
                        }
                    }
                    else
                    {
                        Vector3 spawnPosition = ResolvePathSpawnPosition();
                        GameObject enemyGO = Instantiate(group.EnemyPrefab, spawnPosition, Quaternion.identity);
                        ApplySpawnPresentation(enemyGO);
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

        public void ConfigureLane(float wallWorldX, float leftEdgeX, float rowHeight, int rowCount)
        {
            _wallWorldX    = wallWorldX;
            _laneLeftEdgeX = leftEdgeX;
            _laneRowHeight = rowHeight;
            _laneRowCount  = rowCount;
            ResolveLaneRows(ChapterContent.Active?.map);
        }

        private void ResolvePathWaypointsFromActiveMap()
        {
            if (IsLaneMode) return;
            if (TryUseAuthoredPath()) return;

            MapDefinition map = ChapterContent.Active?.map;
            Vector3[] points = map != null ? map.ComputeWaypoints() : DefaultPaintedPathWaypoints();
            if (points == null || points.Length == 0) return;

            if (_runtimeWaypointRoot != null)
                Destroy(_runtimeWaypointRoot.gameObject);

            var parent = new GameObject("RuntimePathWaypoints").transform;
            _runtimeWaypointRoot = parent;
            _waypoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint_{i:00}").transform;
                waypoint.SetParent(parent, false);
                waypoint.position = points[i] + PathEnemyWorldOffset;
                _waypoints[i] = waypoint;
            }

            _spawnPoints = new[] { _waypoints[0] };
        }

        private bool TryUseAuthoredPath()
        {
            if (_authoredPath == null)
                _authoredPath = FindAnyObjectByType<WaypointPathAuthoring>();

            if (_authoredPath != null && !_authoredPath.HasWaypoints)
                _authoredPath.RebuildFromPoints(DefaultPaintedPathWaypoints());

            if (_authoredPath == null || !_authoredPath.HasWaypoints)
                return false;

            Transform[] authoredWaypoints = _authoredPath.GetWaypointTransforms();
            if (authoredWaypoints == null || authoredWaypoints.Length < 2 || authoredWaypoints[0] == null)
                return false;

            if (_runtimeWaypointRoot != null)
            {
                Destroy(_runtimeWaypointRoot.gameObject);
                _runtimeWaypointRoot = null;
            }

            _waypoints = authoredWaypoints;
            _spawnPoints = new[] { _waypoints[0] };
            return true;
        }

        private Vector3 ResolvePathSpawnPosition()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0 && _spawnPoints[0] != null)
                return _spawnPoints[0].position;
            if (_waypoints != null && _waypoints.Length > 0 && _waypoints[0] != null)
                return _waypoints[0].position;
            return Vector3.zero;
        }

        private void ResolveLaneRows(MapDefinition map)
        {
            List<int> rows = map != null ? map.GetPathRows() : null;
            if (rows == null || rows.Count == 0)
            {
                rows = new List<int>();
                for (int row = 0; row < _laneRowCount; row++)
                    rows.Add(row);
            }

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                if (rows[i] < 0 || rows[i] >= _laneRowCount)
                    rows.RemoveAt(i);
            }

            _laneRows = rows.Count > 0 ? rows.ToArray() : new[] { 0 };
        }

        private Vector3 ResolveLaneSpawnPosition()
        {
            if (_laneRows == null || _laneRows.Length == 0)
                ResolveLaneRows(ChapterContent.Active?.map);

            int row = _laneRows[UnityEngine.Random.Range(0, _laneRows.Length)];
            float spawnY = GridManager.Instance != null
                ? GridManager.Instance.GridToWorld(0, row).y
                : MapDefinition.GridToWorld(0, row).y;

            return new Vector3(_laneLeftEdgeX + _laneEnemyOffset.x, spawnY + _laneEnemyOffset.y, 0f);
        }

        private Vector3 PathEnemyWorldOffset => new Vector3(_pathEnemyOffset.x, _pathEnemyOffset.y, 0f);

        private static Vector3[] DefaultPaintedPathWaypoints()
        {
            return new[]
            {
                new Vector3(-9.35f, -0.2f, 0f),
                new Vector3(-4.65f, -0.2f, 0f),
                new Vector3(-4.65f, 2.28f, 0f),
                new Vector3(4.72f, 2.28f, 0f),
                new Vector3(4.72f, -3.02f, 0f),
                new Vector3(8.7f, -3.02f, 0f),
            };
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
            if (_eliteEnemyPrefab == null) return;

            Vector3 spawnPos;
            if (IsLaneMode)
            {
                spawnPos = ResolveLaneSpawnPosition();
            }
            else
            {
                if (_spawnPoints == null || _spawnPoints.Length == 0) return;
                spawnPos = _spawnPoints[0].position;
            }

            GameObject enemyGO = Instantiate(_eliteEnemyPrefab, spawnPos, Quaternion.identity);
            ApplySpawnPresentation(enemyGO);
            EnemyBase enemy = enemyGO.GetComponent<EnemyBase>();
            if (enemy == null) return;

            if (IsLaneMode)
                enemy.InitializeLane(_wallWorldX);
            else
                enemy.Initialize(_waypoints);

            enemy.ApplyDifficultyMultiplier(statMultiplier);
            ActiveEnemyCount++;
        }

        private void ApplySpawnPresentation(GameObject enemyGO)
        {
            if (enemyGO == null) return;

            float scale = Mathf.Max(0.1f, _spawnedEnemyVisualScale);
            enemyGO.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}

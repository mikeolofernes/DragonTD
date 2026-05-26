using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Core
{
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        [SerializeField] private GameDirectorConfig _config;

        private IntensityMeter _intensity;
        private float _difficultyMultiplier = 1f;
        private int _perfectWaveStreak;
        private int _livesLostThisWave;
        private int _previousLives;
        private float _lastEventTime = float.NegativeInfinity;
        private float _highIntensityTimer;

        public float Intensity => _intensity?.Value ?? 0f;
        public float DifficultyMultiplier => _difficultyMultiplier;

        public event System.Action<DirectorEvent> OnDirectorEvent;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogWarning("[GameDirector] No config assigned — director disabled.");
                enabled = false;
                return;
            }
            _intensity = new IntensityMeter(_config);
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnLivesChanged += HandleLivesChanged;
            WaveManager.Instance.OnWaveComplete += HandleWaveComplete;
            _previousLives = GameManager.Instance.Lives;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnLivesChanged -= HandleLivesChanged;
            }
            if (WaveManager.Instance != null)
                WaveManager.Instance.OnWaveComplete -= HandleWaveComplete;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Wave) return;

            bool enemiesActive = WaveManager.Instance != null && WaveManager.Instance.ActiveEnemyCount > 0;
            _intensity.Tick(Time.deltaTime, enemiesActive);

            if (_intensity.Value >= _config.SustainedHighIntensityThreshold)
                _highIntensityTimer += Time.deltaTime;
            else
                _highIntensityTimer = 0f;

            TryTriggerEvent();
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.Wave)
            {
                _livesLostThisWave = 0;
                _previousLives = GameManager.Instance.Lives;
            }
        }

        private void HandleLivesChanged(int lives)
        {
            if (lives < _previousLives)
            {
                _livesLostThisWave += _previousLives - lives;
                _intensity.Spike(_config.LifeLostIntensitySpike);
            }
            _previousLives = lives;
        }

        private void HandleWaveComplete()
        {
            _intensity.Drop(_config.WaveClearedIntensityDrop);
            _highIntensityTimer = 0f;

            if (_livesLostThisWave == 0)
                _perfectWaveStreak++;
            else
                _perfectWaveStreak = 0;

            AdjustDifficulty();
        }

        private void AdjustDifficulty()
        {
            if (_perfectWaveStreak > 0 && _perfectWaveStreak % _config.PerfectWavesPerDifficultyStep == 0)
            {
                _difficultyMultiplier = Mathf.Min(_config.MaxDifficultyMultiplier, _difficultyMultiplier + 0.1f);
                FireEvent(DirectorEventType.DifficultyIncreased);
            }
            else if (_livesLostThisWave >= _config.LivesLostForRelief)
            {
                _difficultyMultiplier = Mathf.Max(_config.MinDifficultyMultiplier, _difficultyMultiplier - 0.15f);
                FireEvent(DirectorEventType.DifficultyDecreased);
            }
        }

        private void TryTriggerEvent()
        {
            if (Time.time - _lastEventTime < _config.MinEventCooldown) return;

            if (_highIntensityTimer >= _config.SustainedHighIntensityDuration)
            {
                TriggerReliefEvent();
                _highIntensityTimer = 0f;
                return;
            }

            if (_intensity.IsHigh && Random.value < _config.EliteSpawnChance * Time.deltaTime)
            {
                TriggerEliteSpawn();
                return;
            }

            if (_intensity.IsLow && _livesLostThisWave >= 2)
                TriggerReliefEvent();
        }

        private void TriggerEliteSpawn()
        {
            _lastEventTime = Time.time;
            WaveManager.Instance?.SpawnEliteEnemy(_config.EliteStatMultiplier);
            FireEvent(DirectorEventType.EliteEnemySpawn);
        }

        private void TriggerReliefEvent()
        {
            _lastEventTime = Time.time;
            ResourceManager.Instance?.AddMana(_config.ReliefManaBonus);
            ResourceManager.Instance?.AddGold(_config.ReliefGoldBonus);
            FireEvent(DirectorEventType.BonusResourceDrop);
        }

        public void OnEnemySpawned(EnemyBase enemy)
        {
            if (_difficultyMultiplier != 1f)
                enemy.ApplyDifficultyMultiplier(_difficultyMultiplier);
        }

        private void FireEvent(DirectorEventType type)
        {
            var evt = new DirectorEvent(type, _intensity.Value, _difficultyMultiplier, GameManager.Instance.CurrentWave);
            OnDirectorEvent?.Invoke(evt);
            Debug.Log(evt.ToString());
        }
    }
}

using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int _startingLives = 20;

        public int Lives { get; private set; }
        public int CurrentWave { get; private set; }
        public GameState State { get; private set; }

        public event System.Action<GameState> OnStateChanged;
        public event System.Action<int> OnLivesChanged;

        private GameState _stateBeforePause;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(State);
        }

        public void StartBattle()
        {
            Lives = _startingLives;
            CurrentWave = 0;
            OnLivesChanged?.Invoke(Lives);
            ResourceManager.Instance?.ResetForBattle();
            SetState(GameState.Setup);
        }

        public void StartNextWave()
        {
            CurrentWave++;
            SetState(GameState.Wave);
        }

        public void LoseLife(int amount = 1)
        {
            Lives -= amount;
            if (Lives <= 0) Lives = 0;
            OnLivesChanged?.Invoke(Lives);
            if (Lives <= 0) SetState(GameState.Defeat);
        }

        public void OnWaveCleared()
        {
            if (CurrentWave >= WaveManager.Instance.TotalWaves)
            {
                SetState(GameState.Victory);
            }
            else
            {
                SetState(GameState.BetweenWaves);
            }
        }

        public void TogglePause()
        {
            if (State == GameState.Paused)
            {
                Time.timeScale = 1f;
                SetState(_stateBeforePause);
            }
            else
            {
                _stateBeforePause = State;
                Time.timeScale = 0f;
                SetState(GameState.Paused);
            }
        }
    }
}

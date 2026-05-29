using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private int _startingLives = 20;
        [SerializeField] private DragonTD.TowerDefense.ChapterContent[] _chapters;

        public int Lives { get; private set; }
        public int CurrentWave { get; private set; }
        public GameState State { get; private set; }
        public bool IsPlanningPhase => State == GameState.Planning || State == GameState.Setup || State == GameState.BetweenWaves;
        public string CurrentStageId { get; private set; } = StageCatalog.DefaultStageId;
        public StageDefinition CurrentStage => StageCatalog.Get(CurrentStageId);
        public float StageDifficultyMultiplier => CurrentStage.difficultyMultiplier;
        public float StageRewardMultiplier => CurrentStage.rewardMultiplier;

        public event System.Action<GameState> OnStateChanged;
        public event System.Action<int> OnLivesChanged;
        public event System.Action<string> OnBattleMessage;

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
            CurrentStageId = PlayerInventory.Instance?.Progression?.CurrentStageId ?? StageCatalog.DefaultStageId;
            ApplyActiveChapter();
            CleanupBattlefield();
            Lives = _startingLives;
            CurrentWave = 0;
            OnLivesChanged?.Invoke(Lives);
            ResourceManager.Instance?.ResetForBattle();
            BattleStatsTracker.Ensure().ResetBattle();
            SetState(GameState.Planning);
        }

        public void SelectStageForNextBattle(string stageId)
        {
            CurrentStageId = StageCatalog.Get(stageId).stageId;
            ApplyActiveChapter();
        }

        private void ApplyActiveChapter()
        {
            DragonTD.TowerDefense.ChapterContent.Active = null;
            if (_chapters == null) return;
            int chapterNum = StageCatalog.Get(CurrentStageId).chapter;
            foreach (var c in _chapters)
            {
                if (c != null && c.chapterNumber == chapterNum)
                {
                    DragonTD.TowerDefense.ChapterContent.Active = c;
                    return;
                }
            }
        }

        public void StartNextWave()
        {
            if (!IsPlanningPhase)
            {
                Debug.Log($"[GameManager] Ignoring next wave request while state is {State}.");
                return;
            }

            int nextWave = CurrentWave + 1;
            if (WaveManager.Instance == null || !WaveManager.Instance.TryStartWave(nextWave))
            {
                Debug.LogWarning($"[GameManager] Could not start wave {nextWave}.");
                return;
            }

            CurrentWave = nextWave;
            BattleStatsTracker.Ensure().BeginWave(CurrentWave);
            SetState(GameState.Wave);
        }

        public void LoseLife(int amount = 1)
        {
            Lives -= amount;
            if (Lives <= 0) Lives = 0;
            OnLivesChanged?.Invoke(Lives);
            if (Lives <= 0 && State != GameState.Defeat)
            {
                GrantBattleRewards(false);
                SetState(GameState.Defeat);
                ShowBattleMessage("Defeat - the base fell");
            }
            else if (Lives <= 5)
            {
                ShowBattleMessage($"Warning: {Lives} lives remaining");
            }
        }

        public void OnWaveCleared()
        {
            string summary = BattleStatsTracker.Ensure().FinishWave(CurrentWave);
            if (CurrentWave >= WaveManager.Instance.TotalWaves)
            {
                Debug.Log("[GameManager] Prototype complete - all configured waves cleared.");
                BattleRewardResult rewards = GrantBattleRewards(true);
                SetState(GameState.Victory);
                string message = string.IsNullOrEmpty(summary)
                    ? "Prototype complete - all configured waves cleared"
                    : summary;
                if (!string.IsNullOrWhiteSpace(rewards?.summary))
                    message = $"{message}\n{rewards.summary}";
                ShowBattleMessage(message);
            }
            else
            {
                SetState(GameState.Planning);
                ShowBattleMessage(string.IsNullOrEmpty(summary)
                    ? $"Wave {CurrentWave} cleared - prepare defenses"
                    : summary);
            }
        }

        public void ShowBattleMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            OnBattleMessage?.Invoke(message);
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

        private void CleanupBattlefield()
        {
            PlacementManager.Instance?.CancelPlacement();
            TowerSelectionManager.Instance?.ClearSelection();
            WaveManager.Instance?.ResetForBattle();
            GridManager.Instance?.ClearOccupancy();

            foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                Destroy(enemy.gameObject);
            foreach (DragonTower tower in FindObjectsByType<DragonTower>(FindObjectsSortMode.None))
                Destroy(tower.gameObject);
            foreach (ProjectileBase projectile in FindObjectsByType<ProjectileBase>(FindObjectsSortMode.None))
                Destroy(projectile.gameObject);
            foreach (DamageIndicator indicator in FindObjectsByType<DamageIndicator>(FindObjectsSortMode.None))
                Destroy(indicator.gameObject);
            foreach (DeathPopEffect effect in FindObjectsByType<DeathPopEffect>(FindObjectsSortMode.None))
                Destroy(effect.gameObject);
            foreach (SkillCastEffect effect in FindObjectsByType<SkillCastEffect>(FindObjectsSortMode.None))
                Destroy(effect.gameObject);
        }

        private BattleRewardResult GrantBattleRewards(bool victory)
        {
            if (PlayerInventory.Instance == null) return null;
            return PlayerInventory.Instance.GrantBattleCompletionRewards(CurrentWave, victory);
        }
    }
}

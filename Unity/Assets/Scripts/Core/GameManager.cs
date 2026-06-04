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
            CurrentStageId = PlayerInventory.Instance?.Progression?.CurrentStageId ?? StageCatalog.DefaultStageId;
            ApplyActiveChapter();
        }

        public void SetState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(State);
            if (newState == GameState.Wave)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.WaveStart);
                AudioManager.Instance?.PlayMusic(MusicKey.Battle);
            }
            else if (newState == GameState.Victory)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.Victory);
                AudioManager.Instance?.PlayMusic(MusicKey.Victory);
            }
            else if (newState == GameState.Defeat)
            {
                AudioManager.Instance?.PlaySfx(SfxKey.Defeat);
                AudioManager.Instance?.StopMusic();
            }
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

            if (ChapterContent.Active?.map?.mapType == DragonTD.Core.MapType.LaneDefense)
                SetupLaneDefense(ChapterContent.Active.map);

            SetState(GameState.Planning);
        }

        private void SetupLaneDefense(DragonTD.TowerDefense.MapDefinition mapDef)
        {
            Vector3 wallTileCenter = GridManager.Instance != null
                ? GridManager.Instance.GridToWorld(mapDef.wallColumn, 0)
                : DragonTD.TowerDefense.MapDefinition.GridToWorld(mapDef.wallColumn, 0);
            Vector3 leftTileCenter = GridManager.Instance != null
                ? GridManager.Instance.GridToWorld(0, 0)
                : DragonTD.TowerDefense.MapDefinition.GridToWorld(0, 0);

            float wallWorldX = wallTileCenter.x;
            int   rows       = DragonTD.TowerDefense.MapDefinition.Rows;

            DragonTD.TowerDefense.WallBase wall = DragonTD.TowerDefense.WallBase.Create(wallWorldX, rows, mapDef.wallHp);
            wall.OnWallDestroyed += () => SetState(GameState.Defeat);

            if (WaveManager.Instance != null)
                WaveManager.Instance.ConfigureLane(wallWorldX, leftTileCenter.x, 1f, rows);
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

            // Destroy any WallBase from a previous LaneDefense battle
            if (DragonTD.TowerDefense.WallBase.Instance != null)
                Destroy(DragonTD.TowerDefense.WallBase.Instance.gameObject);

            foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude))
                Destroy(enemy.gameObject);
            foreach (DragonTower tower in FindObjectsByType<DragonTower>(FindObjectsInactive.Exclude))
                Destroy(tower.gameObject);
            foreach (ProjectileBase projectile in FindObjectsByType<ProjectileBase>(FindObjectsInactive.Exclude))
                Destroy(projectile.gameObject);
            foreach (DamageIndicator indicator in FindObjectsByType<DamageIndicator>(FindObjectsInactive.Exclude))
                Destroy(indicator.gameObject);
            foreach (DeathPopEffect effect in FindObjectsByType<DeathPopEffect>(FindObjectsInactive.Exclude))
                Destroy(effect.gameObject);
            foreach (SkillCastEffect effect in FindObjectsByType<SkillCastEffect>(FindObjectsInactive.Exclude))
                Destroy(effect.gameObject);
        }

        private BattleRewardResult GrantBattleRewards(bool victory)
        {
            if (PlayerInventory.Instance == null) return null;
            return PlayerInventory.Instance.GrantBattleCompletionRewards(CurrentWave, victory);
        }
    }
}

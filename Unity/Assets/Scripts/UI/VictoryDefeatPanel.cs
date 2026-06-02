using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;
using DragonTD.TowerDefense;

namespace DragonTD.UI
{
    public class VictoryDefeatPanel : MonoBehaviour
    {
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _statsText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _quitButton;

        private CanvasGroup _canvasGroup;
        private bool _subscribedToState;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            RuntimeFontScaler.Apply(gameObject);
            Hide();
        }

        private void OnEnable()
        {
            TrySubscribeToState();
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetry);
            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuit);
        }

        private void Start()
        {
            TrySubscribeToState();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && _subscribedToState)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            _subscribedToState = false;
            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(OnRetry);
            if (_quitButton != null)
                _quitButton.onClick.RemoveListener(OnQuit);
        }

        private void TrySubscribeToState()
        {
            if (_subscribedToState || GameManager.Instance == null) return;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            _subscribedToState = true;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Victory && state != GameState.Defeat)
            {
                Hide();
                return;
            }

            Show();
            bool victory = state == GameState.Victory;
            if (_resultText != null)
                _resultText.text = victory ? "PROTOTYPE COMPLETE" : "DEFEAT";
            if (_statsText != null)
            {
                int totalWaves = WaveManager.Instance != null ? WaveManager.Instance.TotalWaves : GameManager.Instance.CurrentWave;
                int wavesCleared = victory ? GameManager.Instance.CurrentWave : Mathf.Max(0, GameManager.Instance.CurrentWave - 1);
                _statsText.text = BattleStatsTracker.Ensure().BuildBattleSummary(
                    victory,
                    wavesCleared,
                    totalWaves,
                    GameManager.Instance.Lives);
                if (!string.IsNullOrWhiteSpace(PlayerInventory.Instance?.LastBattleRewardSummary))
                    _statsText.text = $"{_statsText.text}\n\n{PlayerInventory.Instance.LastBattleRewardSummary}";
                BattleRewardResult reward = PlayerInventory.Instance?.LastBattleRewardResult;
                if (reward != null && !string.IsNullOrWhiteSpace(reward.stageTitle))
                {
                    _statsText.text = $"{_statsText.text}\n\n{reward.stageTitle}\nStars: {reward.starsEarned}/3  Best: {reward.bestStars}/3";
                    if (!string.IsNullOrWhiteSpace(reward.objectiveSummary))
                        _statsText.text = $"{_statsText.text}\n{reward.objectiveSummary}";
                }
            }

            SetButtonLabel(_retryButton, victory ? "Replay" : "Retry");
            SetButtonLabel(_quitButton, "Main Menu");
        }

        private void OnRetry()
        {
            Hide();
            GameManager.Instance.StartBattle();
        }

        private void OnQuit() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

        private void Show()
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void Hide()
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
                text.text = label;
        }
    }
}

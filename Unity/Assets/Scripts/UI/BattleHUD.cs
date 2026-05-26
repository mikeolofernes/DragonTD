using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private Text _livesText;
        [SerializeField] private Text _waveText;
        [SerializeField] private Text _manaText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _nextWaveButton;

        private void Start()
        {
            _pauseButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            _nextWaveButton.onClick.AddListener(() => GameManager.Instance.StartNextWave());
        }

        private void OnEnable()
        {
            if (GameManager.Instance == null || ResourceManager.Instance == null) return;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnLivesChanged += UpdateLivesFromEvent;
            ResourceManager.Instance.OnManaChanged += UpdateMana;
            ResourceManager.Instance.OnGoldChanged += UpdateGold;
            UpdateLives();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnLivesChanged -= UpdateLivesFromEvent;
            }
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnManaChanged -= UpdateMana;
                ResourceManager.Instance.OnGoldChanged -= UpdateGold;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            bool showNextWave = state == GameState.BetweenWaves || state == GameState.Setup;
            if (_nextWaveButton != null) _nextWaveButton.gameObject.SetActive(showNextWave);
            if (_waveText != null) _waveText.text = $"Wave: {GameManager.Instance.CurrentWave}";
        }

        private void UpdateMana(int mana)
        {
            if (_manaText != null) _manaText.text = $"Mana: {mana}";
        }

        private void UpdateGold(int gold)
        {
            if (_goldText != null) _goldText.text = $"Gold: {gold}";
        }

        private void UpdateLives()
        {
            if (_livesText != null) _livesText.text = $"Lives: {GameManager.Instance?.Lives ?? 0}";
        }

        private void UpdateLivesFromEvent(int _) => UpdateLives();
    }
}

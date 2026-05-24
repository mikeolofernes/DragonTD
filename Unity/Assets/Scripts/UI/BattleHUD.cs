using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _livesText;
        [SerializeField] private TextMeshProUGUI _waveText;
        [SerializeField] private TextMeshProUGUI _manaText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _nextWaveButton;

        private void Start()
        {
            _pauseButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            _nextWaveButton.onClick.AddListener(() => GameManager.Instance.StartNextWave());
        }

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            GameManager.Instance.OnLivesChanged += UpdateLivesFromEvent;
            ResourceManager.Instance.OnManaChanged += UpdateMana;
            ResourceManager.Instance.OnGoldChanged += UpdateGold;
            UpdateLives();
        }

        private void OnDisable()
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnLivesChanged -= UpdateLivesFromEvent;
            ResourceManager.Instance.OnManaChanged -= UpdateMana;
            ResourceManager.Instance.OnGoldChanged -= UpdateGold;
        }

        private void HandleStateChanged(GameState state)
        {
            bool isBetweenWaves = state == GameState.BetweenWaves;
            _nextWaveButton.gameObject.SetActive(isBetweenWaves);
            _waveText.text = $"Wave: {GameManager.Instance.CurrentWave}";
        }

        private void UpdateMana(int mana)
        {
            _manaText.text = $"Mana: {mana}";
        }

        private void UpdateGold(int gold)
        {
            _goldText.text = $"Gold: {gold}";
        }

        private void UpdateLives() =>
            _livesText.text = $"Lives: {GameManager.Instance.Lives}";

        private void UpdateLivesFromEvent(int _) => UpdateLives();
    }
}

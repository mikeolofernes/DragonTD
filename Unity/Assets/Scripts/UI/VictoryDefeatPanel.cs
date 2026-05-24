using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class VictoryDefeatPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _quitButton;

        private void OnEnable()
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            _retryButton.onClick.AddListener(OnRetry);
            _quitButton.onClick.AddListener(OnQuit);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Victory && state != GameState.Defeat) return;

            gameObject.SetActive(true);
            _resultText.text = state == GameState.Victory ? "VICTORY!" : "DEFEAT";
            _statsText.text = $"Waves cleared: {GameManager.Instance.CurrentWave}\n" +
                              $"Lives remaining: {GameManager.Instance.Lives}\n" +
                              $"Gold earned: {ResourceManager.Instance.Gold}";
        }

        private void OnRetry()
        {
            gameObject.SetActive(false);
            GameManager.Instance.StartBattle();
        }

        private void OnQuit() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}

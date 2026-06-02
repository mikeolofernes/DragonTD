using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class AccountBuffPanel : MonoBehaviour
    {
        [SerializeField] private Text _summaryText;
        [SerializeField] private Button _damageButton;
        [SerializeField] private Button _attackSpeedButton;
        [SerializeField] private Button _manaButton;
        [SerializeField] private Button _resetSaveButton;

        private bool _subscribed;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            TrySubscribe();
            WireButtons();
            Refresh();
        }

        private void OnDisable()
        {
            if (PlayerInventory.Instance != null && _subscribed)
                PlayerInventory.Instance.OnProgressionChanged -= Refresh;
            _subscribed = false;
            UnwireButtons();
        }

        private void Update()
        {
            TrySubscribe();
            bool show = GameManager.Instance == null || GameManager.Instance.State != GameState.Wave;
            SetVisible(show);
        }

        private void TrySubscribe()
        {
            if (_subscribed || PlayerInventory.Instance == null) return;
            PlayerInventory.Instance.OnProgressionChanged += Refresh;
            _subscribed = true;
        }

        private void WireButtons()
        {
            if (_damageButton != null)
                _damageButton.onClick.AddListener(() => Upgrade(AccountBuffType.Damage));
            if (_attackSpeedButton != null)
                _attackSpeedButton.onClick.AddListener(() => Upgrade(AccountBuffType.AttackSpeed));
            if (_manaButton != null)
                _manaButton.onClick.AddListener(() => Upgrade(AccountBuffType.StartingMana));
            if (_resetSaveButton != null)
                _resetSaveButton.onClick.AddListener(ResetSave);
        }

        private void UnwireButtons()
        {
            if (_damageButton != null) _damageButton.onClick.RemoveAllListeners();
            if (_attackSpeedButton != null) _attackSpeedButton.onClick.RemoveAllListeners();
            if (_manaButton != null) _manaButton.onClick.RemoveAllListeners();
            if (_resetSaveButton != null) _resetSaveButton.onClick.RemoveAllListeners();
        }

        private void Upgrade(AccountBuffType buffType)
        {
            if (PlayerInventory.Instance == null) return;
            if (PlayerInventory.Instance.TryUpgradeAccountBuff(buffType, out string message))
                Refresh();
            GameManager.Instance?.ShowBattleMessage(message);
        }

        private void ResetSave()
        {
            PlayerInventory.Instance?.ResetLocalProgression();
            GameManager.Instance?.ShowBattleMessage("Local progression reset");
            Refresh();
        }

        private void Refresh()
        {
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            if (progression == null) return;

            if (_summaryText != null)
                _summaryText.text = progression.BuildSummary();

            SetButton(_damageButton, AccountBuffType.Damage, progression);
            SetButton(_attackSpeedButton, AccountBuffType.AttackSpeed, progression);
            SetButton(_manaButton, AccountBuffType.StartingMana, progression);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        private static void SetButton(Button button, AccountBuffType buffType, PlayerProgression progression)
        {
            if (button == null || progression == null) return;

            int level = progression.GetBuffLevel(buffType);
            int cost = progression.GetUpgradeCost(buffType);
            bool maxed = level >= PlayerProgression.MaxBuffLevel;
            button.interactable = !maxed && progression.Essence >= cost;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = maxed
                    ? $"{PlayerProgression.Label(buffType)} Max"
                    : $"{PlayerProgression.Label(buffType)} {cost}e";
        }
    }
}

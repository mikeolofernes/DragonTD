using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DragonTD.Core;
using System.Collections.Generic;

namespace DragonTD.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _battleButton;
        [SerializeField] private Button _battleNavButton;
        [SerializeField] private Button _dragonsButton;
        [SerializeField] private Button _profileButton;
        [SerializeField] private Button _clanButton;
        [SerializeField] private Button _storeButton;
        [SerializeField] private Button[] _chestButtons;
        [SerializeField] private Text _currencyText;
        [SerializeField] private Text _chestText;
        [SerializeField] private Text _eventText;
        [SerializeField] private Text _messageText;
        [SerializeField] private GameObject _chestRewardPanel;
        [SerializeField] private Text _chestRewardText;
        [SerializeField] private Button _chestRewardCloseButton;
        [SerializeField] private GameObject _confirmationPanel;
        [SerializeField] private Text _confirmationText;
        [SerializeField] private Button _confirmationYesButton;
        [SerializeField] private Button _confirmationNoButton;
        [SerializeField] private Text _syncStatusText;
        [SerializeField] private Button _quitButton;
        [SerializeField] private ProfileProgressionPanel _profilePanel;
        [SerializeField] private StorePanel _storePanel;
        [SerializeField] private EventsPanel _eventsPanel;
        [SerializeField] private ClanPanel _clanPanel;
        [SerializeField] private LeaderboardPanel _leaderboardPanel;
        [SerializeField] private Button _leaderboardButton;

        private float _nextChestRefreshTime;
        private int _pendingSpeedUpSlot = -1;
        private bool _profilePanelShowingProfile;
        private GameObject _stageSelectPanel;
        private Text _stageSelectText;
        private Button[] _stageButtons;
        private Button _stageStartButton;
        private Button _stageCloseButton;
        private static string _lastShownBattleSummary;
        private bool _stageWarningAcknowledged;

        private void Start()
        {
            EnsureHubControls();
            RuntimeFontScaler.Apply(gameObject);
            if (_battleButton != null)
                _battleButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick); OpenStageSelect(); });
            if (_battleNavButton != null)
                _battleNavButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick); OpenStageSelect(); });
            if (_dragonsButton != null)
                _dragonsButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick); ShowDragons(); });
            if (_profileButton != null)
                _profileButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick); ToggleProfile(); });
            if (_clanButton != null)
                _clanButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick); ToggleClan(); });
            if (_leaderboardButton != null)
                _leaderboardButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick); ToggleLeaderboard(); });
            if (_storeButton != null)
                _storeButton.onClick.AddListener(() => { AudioManager.Instance?.PlaySfx(SfxKey.ButtonClick); ToggleStore(); });
            if (_eventText != null)
            {
                Button eventButton = _eventText.GetComponent<Button>() ?? _eventText.gameObject.AddComponent<Button>();
                eventButton.onClick.AddListener(ToggleEvents);
            }
            if (_chestButtons != null)
            {
                for (int i = 0; i < _chestButtons.Length; i++)
                {
                    int slot = i;
                    if (_chestButtons[i] != null)
                        _chestButtons[i].onClick.AddListener(() => ClickChest(slot));
                }
            }
            if (_quitButton != null)
                _quitButton.onClick.AddListener(Application.Quit);
            if (_chestRewardCloseButton != null)
                _chestRewardCloseButton.onClick.AddListener(() => _chestRewardPanel?.SetActive(false));
            if (_confirmationYesButton != null)
            {
                _confirmationYesButton.onClick.RemoveListener(ConfirmPendingSpeedUp);
                _confirmationYesButton.onClick.AddListener(ConfirmPendingSpeedUp);
            }
            if (_confirmationNoButton != null)
            {
                _confirmationNoButton.onClick.RemoveListener(CancelPendingSpeedUp);
                _confirmationNoButton.onClick.AddListener(CancelPendingSpeedUp);
            }
            if (_stageStartButton != null)
                _stageStartButton.onClick.AddListener(StartSelectedStage);
            if (_stageCloseButton != null)
                _stageCloseButton.onClick.AddListener(CloseStageSelect);
            WireStageButtons();

            if (_profilePanel != null)
                _profilePanel.gameObject.SetActive(false);
            if (_storePanel != null)
                _storePanel.gameObject.SetActive(false);
            if (_eventsPanel != null)
                _eventsPanel.gameObject.SetActive(false);
            if (_clanPanel != null)
                _clanPanel.gameObject.SetActive(false);
            if (_leaderboardPanel != null)
                _leaderboardPanel.gameObject.SetActive(false);
            if (_chestRewardPanel != null)
                _chestRewardPanel.SetActive(false);
            if (_confirmationPanel != null)
                _confirmationPanel.SetActive(false);
            if (_stageSelectPanel != null)
                _stageSelectPanel.SetActive(false);

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnProgressionChanged += RefreshHub;
                PlayerInventory.Instance.OnSyncStatusChanged += HandleSyncStatusChanged;
            }
            RefreshHub();
            ShowLastBattleReturnSummary();

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MainMenu);

            AudioManager.Instance?.PlayMusic(MusicKey.Menu);
        }

        private void Update()
        {
            SetHubChromeVisible(!IsAnyHubPanelOpen());

            if (Time.unscaledTime < _nextChestRefreshTime)
                return;

            _nextChestRefreshTime = Time.unscaledTime + 1f;
            RefreshChestButtons();
        }

        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnProgressionChanged -= RefreshHub;
                PlayerInventory.Instance.OnSyncStatusChanged -= HandleSyncStatusChanged;
            }
        }

        private void ShowDragons()
        {
            if (_profilePanel == null) return;
            HideHubPanels(_profilePanel.gameObject);
            _profilePanelShowingProfile = false;
            _profilePanel.gameObject.SetActive(true);
            _profilePanel.SendMessage("SetDragonsMode", SendMessageOptions.DontRequireReceiver);
            _profilePanel.Refresh();
            ShowMessage("Dragons collection");
        }

        private void OpenStageSelect()
        {
            EnsureStageSelectPanel();
            _stageWarningAcknowledged = false;
            HideHubPanels(_stageSelectPanel);
            if (_stageSelectText != null)
                _stageSelectText.text = BuildSelectedStageText();
            RefreshStageButtons();
            if (_stageSelectPanel != null)
                _stageSelectPanel.SetActive(true);
            ShowMessage("Select a stage");
        }

        private void CloseStageSelect()
        {
            if (_stageSelectPanel != null)
                _stageSelectPanel.SetActive(false);
            HideHubPanels(null);
            ShowMessage("Stage select closed");
        }

        private void StartSelectedStage()
        {
            string stageId = PlayerInventory.Instance?.Progression?.CurrentStageId ?? StageCatalog.DefaultStageId;
            StageDefinition stage = StageCatalog.Get(stageId);
            string warning = PlayerInventory.Instance?.BuildLoadoutCoverageWarning(stage);
            if (!_stageWarningAcknowledged && !string.IsNullOrWhiteSpace(warning))
            {
                _stageWarningAcknowledged = true;
                ShowMessage($"{warning}. Press Start Stage again to continue.");
                if (_stageSelectText != null)
                    _stageSelectText.text = $"{BuildSelectedStageText()}\n\n{warning}\nPress Start Stage again to continue.";
                return;
            }

            GameManager.Instance?.SelectStageForNextBattle(stageId);
            SceneManager.LoadScene("BattleScene");
        }

        private void SelectStage(int index)
        {
            if (index < 0 || index >= StageCatalog.Stages.Length) return;
            PlayerInventory inventory = PlayerInventory.Instance;
            StageDefinition stage = StageCatalog.Stages[index];
            _stageWarningAcknowledged = false;
            if (inventory != null && !inventory.TrySelectStage(stage.stageId, out string message))
            {
                ShowMessage(message);
                return;
            }

            if (_stageSelectText != null)
                _stageSelectText.text = BuildSelectedStageText();
            RefreshStageButtons();
            ShowMessage(stage.Title);
        }

        private void ToggleProfile()
        {
            if (_profilePanel == null) return;
            bool show = !_profilePanel.gameObject.activeSelf || !_profilePanelShowingProfile;
            HideHubPanels(show ? _profilePanel.gameObject : null);
            _profilePanelShowingProfile = show;
            _profilePanel.gameObject.SetActive(show);
            if (show)
                _profilePanel.SendMessage("SetProfileMode", SendMessageOptions.DontRequireReceiver);
            if (show)
                _profilePanel.Refresh();
        }

        private void ToggleStore()
        {
            EnsureStorePanel();
            if (_storePanel == null) return;
            bool show = !_storePanel.gameObject.activeSelf;
            HideHubPanels(show ? _storePanel.gameObject : null);
            _storePanel.gameObject.SetActive(show);
            if (show)
            {
                _storePanel.Refresh();
                ShowMessage("Store opened");
            }
        }

        private void ToggleEvents()
        {
            EnsureEventsPanel();
            if (_eventsPanel == null) return;
            bool show = !_eventsPanel.gameObject.activeSelf;
            HideHubPanels(show ? _eventsPanel.gameObject : null);
            _eventsPanel.gameObject.SetActive(show);
            if (show)
            {
                _eventsPanel.Refresh();
                ShowMessage("Events opened");
            }
        }

        private void ToggleClan()
        {
            EnsureClanPanel();
            if (_clanPanel == null) return;
            bool show = !_clanPanel.gameObject.activeSelf;
            HideHubPanels(show ? _clanPanel.gameObject : null);
            _clanPanel.gameObject.SetActive(show);
            if (show)
                ShowMessage("Clan shell opened");
        }

        private void ToggleLeaderboard()
        {
            EnsureLeaderboardPanel();
            if (_leaderboardPanel == null) return;
            bool show = !_leaderboardPanel.gameObject.activeSelf;
            HideHubPanels(show ? _leaderboardPanel.gameObject : null);
            _leaderboardPanel.gameObject.SetActive(show);
            if (show)
                ShowMessage("Leaderboard opened");
        }

        private void HideHubPanels(GameObject except)
        {
            if (_profilePanel != null && _profilePanel.gameObject != except)
                _profilePanel.gameObject.SetActive(false);
            if (_storePanel != null && _storePanel.gameObject != except)
                _storePanel.gameObject.SetActive(false);
            if (_eventsPanel != null && _eventsPanel.gameObject != except)
                _eventsPanel.gameObject.SetActive(false);
            if (_clanPanel != null && _clanPanel.gameObject != except)
                _clanPanel.gameObject.SetActive(false);
            if (_leaderboardPanel != null && _leaderboardPanel.gameObject != except)
                _leaderboardPanel.gameObject.SetActive(false);
            if (_stageSelectPanel != null && _stageSelectPanel != except)
                _stageSelectPanel.SetActive(false);

            SetHubChromeVisible(except == null);
        }

        private void SetHubChromeVisible(bool visible)
        {
            SetActive(_battleButton, visible);
            SetActive(_eventText, visible);
            SetActive(_chestText, visible);
            SetActive(_messageText, visible);
            SetChildActive("TitleText", visible);
            SetChildActive("SubtitleText", visible);
            if (_chestButtons != null)
            {
                foreach (Button button in _chestButtons)
                    SetActive(button, visible);
            }
        }

        private void SetChildActive(string childName, bool active)
        {
            Transform child = transform.Find(childName);
            if (child != null)
                child.gameObject.SetActive(active);
        }

        private bool IsAnyHubPanelOpen()
        {
            return (_profilePanel != null && _profilePanel.gameObject.activeSelf)
                || (_storePanel != null && _storePanel.gameObject.activeSelf)
                || (_eventsPanel != null && _eventsPanel.gameObject.activeSelf)
                || (_clanPanel != null && _clanPanel.gameObject.activeSelf)
                || (_leaderboardPanel != null && _leaderboardPanel.gameObject.activeSelf)
                || (_stageSelectPanel != null && _stageSelectPanel.activeSelf);
        }

        private void ClickChest(int slot)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            if (inventory.TryOpenChest(slot, out ChestRewardResult reward, out string message))
                ShowChestReward(reward);
            else if (inventory.Progression != null && inventory.Progression.IsChestSlotUnlocking(slot))
            {
                ShowSpeedUpConfirmation(slot);
                return;
            }
            else if (inventory.TryStartChestUnlock(slot, out message))
                ShowChestAction(slot, "Chest Unlock Started", message);

            ShowMessage(message);
            RefreshHub();
        }

        private void RefreshHub()
        {
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            if (progression == null) return;

            if (_currencyText != null)
                _currencyText.text = $"Gold {progression.Gold}    Gems {progression.Gems}";
            if (_chestText != null)
                _chestText.text = "Chest Slots";
            if (_eventText != null)
                _eventText.text = BuildEventHubText(progression);
            if (_syncStatusText != null)
                _syncStatusText.text = $"Save: {PlayerInventory.Instance?.SyncStatus ?? "Local"}";
            RefreshChestButtons();
        }

        private void ShowLastBattleReturnSummary()
        {
            string summary = PlayerInventory.Instance?.LastBattleRewardSummary;
            if (string.IsNullOrWhiteSpace(summary) || summary == _lastShownBattleSummary)
                return;

            _lastShownBattleSummary = summary;
            ShowRewardMessage("Battle Rewards", summary);
        }

        private static string BuildEventHubText(PlayerProgression progression)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Events");
            foreach (PrototypeEventDefinition evt in PrototypeEventCatalog.Events)
                sb.AppendLine($"{evt.displayName}: {progression.BuildEventStatus(evt)}");
            sb.AppendLine("Daily");
            sb.Append(progression.BuildDailyObjectiveSummary());
            return sb.ToString();
        }

        private void RefreshChestButtons()
        {
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            if (progression == null || _chestButtons == null) return;

            for (int i = 0; i < _chestButtons.Length; i++)
            {
                Button button = _chestButtons[i];
                if (button == null) continue;

                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = progression.BuildChestSlotLabel(i);
                    label.fontSize = 17;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 12;
                    label.resizeTextMaxSize = 18;
                }

                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    if (progression.IsChestSlotReady(i))
                        image.color = new Color(1f, 0.82f, 0.18f, 0.96f);
                    else if (progression.IsChestSlotUnlocking(i))
                        image.color = new Color(0.13f, 0.56f, 0.92f, 0.88f);
                    else
                        image.color = new Color(0.08f, 0.28f, 0.55f, 0.82f);
                }
            }
        }

        private void ShowMessage(string message)
        {
            if (_messageText != null)
                _messageText.text = message;
        }

        private void HandleSyncStatusChanged(string status)
        {
            if (_syncStatusText != null)
                _syncStatusText.text = $"Save: {status}";
        }

        private void ShowChestReward(ChestRewardResult reward)
        {
            if (reward == null) return;
            EnsureChestRewardPanel();
            if (_chestRewardText != null)
            {
                _chestRewardText.text =
                    $"{reward.rarity} Chest Opened\n\nRewards\n+{reward.gold} Gold\n+{reward.gems} Gems\n\nSlot {reward.slotIndex + 1} is now empty.";
            }
            if (_chestRewardPanel != null)
                _chestRewardPanel.SetActive(true);
        }

        private void ShowChestAction(int slot, string title, string body)
        {
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            EnsureChestRewardPanel();
            if (_chestRewardText != null && progression != null)
            {
                string rarity = progression.GetChestSlotRarity(slot);
                _chestRewardText.text =
                    $"{title}\n\n{rarity} Chest\n{progression.GetChestSlotRewardPreview(slot)}\n\n{body}";
            }
            if (_chestRewardPanel != null)
                _chestRewardPanel.SetActive(true);
        }

        public void ShowRewardMessage(string title, string body)
        {
            EnsureChestRewardPanel();
            if (_chestRewardText != null)
                _chestRewardText.text = $"{title}\n\n{body}";
            if (_chestRewardPanel != null)
                _chestRewardPanel.SetActive(true);
            ShowMessage(body);
            RefreshHub();
        }

        private void ShowSpeedUpConfirmation(int slot)
        {
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            if (progression == null) return;

            int cost = progression.GetChestSpeedUpGemCost(slot);
            _pendingSpeedUpSlot = slot;
            EnsureConfirmationPanel();
            if (_confirmationText != null)
                _confirmationText.text = $"Spend {cost} Gems to finish this chest now?";
            if (_confirmationPanel != null)
                _confirmationPanel.SetActive(true);
        }

        private void ConfirmPendingSpeedUp()
        {
            if (_pendingSpeedUpSlot < 0)
            {
                CancelPendingSpeedUp();
                return;
            }

            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory != null)
            {
                inventory.TrySpeedUpChestUnlock(_pendingSpeedUpSlot, out string message);
                ShowMessage(message);
                ShowChestAction(_pendingSpeedUpSlot, "Chest Ready", message);
            }

            _pendingSpeedUpSlot = -1;
            if (_confirmationPanel != null)
                _confirmationPanel.SetActive(false);
            RefreshHub();
        }

        private void CancelPendingSpeedUp()
        {
            _pendingSpeedUpSlot = -1;
            if (_confirmationPanel != null)
                _confirmationPanel.SetActive(false);
            ShowMessage("Speed-up cancelled");
        }

        private void EnsureHubControls()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite sprite = CreateRuntimeSprite();

            if (_currencyText == null)
                _currencyText = CreateText("CurrencyText", font, new Vector2(0.5f, 0.94f), new Vector2(560f, 42f), "Gold 0    Gems 0", 24);
            if (_eventText == null)
                _eventText = CreateText("EventsText", font, new Vector2(0.16f, 0.61f), new Vector2(270f, 190f), "Events", 22);
            if (_chestText == null)
                _chestText = CreateText("ChestText", font, new Vector2(0.5f, 0.30f), new Vector2(360f, 34f), "Chest Slots", 22);
            if (_messageText == null)
                _messageText = CreateText("HubMessageText", font, new Vector2(0.5f, 0.38f), new Vector2(720f, 42f), "Welcome back", 20);
            if (_syncStatusText == null)
                _syncStatusText = CreateText("HubSyncStatusText", font, new Vector2(0.22f, 0.94f), new Vector2(260f, 30f), "Save: Local", 15);

            if (_storeButton == null)
                _storeButton = CreateButton("StoreButton", "Store", font, sprite, new Vector2(0.78f, 0.94f), new Vector2(190f, 44f));
            if (_battleButton == null)
                _battleButton = CreateButton("BattleButton", "BATTLE", font, sprite, new Vector2(0.5f, 0.47f), new Vector2(300f, 92f));
            if (_battleNavButton == null)
                _battleNavButton = CreateButton("BattleNavButton", "Battle", font, sprite, new Vector2(0.60f, 0.07f), new Vector2(180f, 58f));
            if (_dragonsButton == null)
                _dragonsButton = CreateButton("DragonsNavButton", "Dragons", font, sprite, new Vector2(0.18f, 0.07f), new Vector2(180f, 58f));
            if (_profileButton == null)
                _profileButton = CreateButton("ProfileNavButton", "Profile", font, sprite, new Vector2(0.38f, 0.07f), new Vector2(180f, 58f));
            if (_clanButton == null)
                _clanButton = CreateButton("ClanNavButton", "Clan", font, sprite, new Vector2(0.82f, 0.07f), new Vector2(180f, 58f));
            if (_leaderboardButton == null)
                _leaderboardButton = CreateButton("LeaderboardButton", "Ranks", font, sprite, new Vector2(0.22f, 0.87f), new Vector2(160f, 44f));
            EnsureChestRewardPanel();

            if (_chestButtons == null || _chestButtons.Length < PlayerProgression.ChestSlotCount)
            {
                var buttons = new List<Button>();
                for (int i = 0; i < PlayerProgression.ChestSlotCount; i++)
                {
                    buttons.Add(CreateButton($"ChestSlotButton{i + 1}", "CHEST", font, sprite,
                        new Vector2(0.20f + i * 0.20f, 0.20f), new Vector2(150f, 88f)));
                }
                _chestButtons = buttons.ToArray();
            }

            PositionText(_currencyText, new Vector2(0.5f, 0.93f), new Vector2(560f, 42f), 24, TextAnchor.MiddleCenter);
            PositionText(_eventText, new Vector2(0.16f, 0.61f), new Vector2(280f, 190f), 19, TextAnchor.MiddleCenter);
            PositionText(_chestText, new Vector2(0.5f, 0.31f), new Vector2(360f, 34f), 22, TextAnchor.MiddleCenter);
            PositionText(_messageText, new Vector2(0.5f, 0.39f), new Vector2(720f, 42f), 20, TextAnchor.MiddleCenter);
            PositionText(_syncStatusText, new Vector2(0.22f, 0.94f), new Vector2(260f, 30f), 15, TextAnchor.MiddleCenter);

            PositionButton(_battleButton, new Vector2(0.5f, 0.52f), new Vector2(300f, 92f), "BATTLE");
            PositionButton(_dragonsButton, new Vector2(0.18f, 0.07f), new Vector2(180f, 58f), "Dragons");
            PositionButton(_profileButton, new Vector2(0.38f, 0.07f), new Vector2(180f, 58f), "Profile");
            PositionButton(_battleNavButton, new Vector2(0.60f, 0.07f), new Vector2(180f, 58f), "Battle");
            PositionButton(_clanButton, new Vector2(0.82f, 0.07f), new Vector2(180f, 58f), "Clan");
            PositionButton(_storeButton, new Vector2(0.78f, 0.94f), new Vector2(190f, 44f), "Store");
            PositionButton(_leaderboardButton, new Vector2(0.22f, 0.87f), new Vector2(160f, 44f), "Ranks");
            if (_chestButtons != null)
            {
                for (int i = 0; i < _chestButtons.Length; i++)
                    PositionButton(_chestButtons[i], new Vector2(0.20f + i * 0.20f, 0.20f), new Vector2(150f, 88f), "CHEST");
            }
            EnsureStorePanel();
            EnsureEventsPanel();
            EnsureClanPanel();
            EnsureLeaderboardPanel();
            EnsureConfirmationPanel();
            EnsureStageSelectPanel();
            if (_quitButton != null)
                _quitButton.gameObject.SetActive(false);
        }

        private void EnsureChestRewardPanel()
        {
            if (_chestRewardPanel == null)
            {
                Transform found = transform.Find("ChestRewardPanel");
                if (found != null)
                    _chestRewardPanel = found.gameObject;
            }

            if (_chestRewardPanel == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                Sprite sprite = CreateRuntimeSprite();
                _chestRewardPanel = new GameObject("ChestRewardPanel");
                _chestRewardPanel.transform.SetParent(transform, false);
                RectTransform rt = _chestRewardPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.32f, 0.36f);
                rt.anchorMax = new Vector2(0.68f, 0.68f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Image image = _chestRewardPanel.AddComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(0.03f, 0.09f, 0.16f, 0.96f);

                _chestRewardText = CreateText("RewardText", font, new Vector2(0.5f, 0.62f), new Vector2(520f, 160f), string.Empty, 28);
                _chestRewardText.transform.SetParent(_chestRewardPanel.transform, false);
                _chestRewardCloseButton = CreateButton("RewardCloseButton", "Close", font, sprite, new Vector2(0.5f, 0.18f), new Vector2(180f, 54f));
                _chestRewardCloseButton.transform.SetParent(_chestRewardPanel.transform, false);
            }

            if (_chestRewardText == null && _chestRewardPanel != null)
                _chestRewardText = _chestRewardPanel.GetComponentInChildren<Text>();
            if (_chestRewardCloseButton == null && _chestRewardPanel != null)
                _chestRewardCloseButton = _chestRewardPanel.GetComponentInChildren<Button>();
        }

        private void EnsureConfirmationPanel()
        {
            if (_confirmationPanel == null)
            {
                Transform found = transform.Find("ConfirmationPanel");
                if (found != null)
                    _confirmationPanel = found.gameObject;
            }

            if (_confirmationPanel == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                Sprite sprite = CreateRuntimeSprite();
                _confirmationPanel = new GameObject("ConfirmationPanel");
                _confirmationPanel.transform.SetParent(transform, false);
                RectTransform rt = _confirmationPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.34f, 0.39f);
                rt.anchorMax = new Vector2(0.66f, 0.64f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Image image = _confirmationPanel.AddComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(0.03f, 0.08f, 0.13f, 0.97f);

                _confirmationText = CreateText("ConfirmationText", font, new Vector2(0.5f, 0.64f), new Vector2(440f, 90f), "Confirm", 22);
                _confirmationText.transform.SetParent(_confirmationPanel.transform, false);
                _confirmationYesButton = CreateButton("ConfirmationYesButton", "Spend Gems", font, sprite, new Vector2(0.35f, 0.22f), new Vector2(160f, 46f));
                _confirmationYesButton.transform.SetParent(_confirmationPanel.transform, false);
                _confirmationNoButton = CreateButton("ConfirmationNoButton", "Cancel", font, sprite, new Vector2(0.65f, 0.22f), new Vector2(140f, 46f));
                _confirmationNoButton.transform.SetParent(_confirmationPanel.transform, false);
            }

            if (_confirmationText == null && _confirmationPanel != null)
                _confirmationText = _confirmationPanel.transform.Find("ConfirmationText")?.GetComponent<Text>();
            Button[] buttons = _confirmationPanel != null ? _confirmationPanel.GetComponentsInChildren<Button>() : null;
            if ((_confirmationYesButton == null || _confirmationNoButton == null) && buttons != null && buttons.Length >= 2)
            {
                _confirmationYesButton = buttons[0];
                _confirmationNoButton = buttons[1];
            }
        }

        private void EnsureStageSelectPanel()
        {
            if (_stageSelectPanel == null)
            {
                Transform found = transform.Find("StageSelectPanel");
                if (found != null)
                    _stageSelectPanel = found.gameObject;
            }

            if (_stageSelectPanel == null)
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                Sprite sprite = CreateRuntimeSprite();
                _stageSelectPanel = new GameObject("StageSelectPanel");
                _stageSelectPanel.transform.SetParent(transform, false);
                RectTransform rt = _stageSelectPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.30f, 0.30f);
                rt.anchorMax = new Vector2(0.70f, 0.78f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Image image = _stageSelectPanel.AddComponent<Image>();
                image.sprite = sprite;
                image.color = new Color(0.03f, 0.09f, 0.16f, 0.97f);

                _stageSelectText = CreateText("StageSelectText", font, new Vector2(0.5f, 0.63f), new Vector2(520f, 190f), string.Empty, 24);
                _stageSelectText.transform.SetParent(_stageSelectPanel.transform, false);
                _stageButtons = new Button[StageCatalog.Stages.Length];
                for (int i = 0; i < _stageButtons.Length; i++)
                {
                    int col = i % 4;
                    int row = i / 4;
                    float x = 0.14f + col * 0.22f;   // 4 cols: 0.14, 0.36, 0.58, 0.80
                    float y = 0.62f - row * 0.20f;    // rows drop: 0.62, 0.42, 0.22
                    _stageButtons[i] = CreateButton($"StageButton{i + 1}", StageCatalog.Stages[i].stageNumber, font, sprite,
                        new Vector2(x, y), new Vector2(138f, 68f));
                    _stageButtons[i].transform.SetParent(_stageSelectPanel.transform, false);
                }
                _stageStartButton = CreateButton("StageStartButton", "Start Stage", font, sprite, new Vector2(0.37f, 0.18f), new Vector2(180f, 52f));
                _stageStartButton.transform.SetParent(_stageSelectPanel.transform, false);
                _stageCloseButton = CreateButton("StageCloseButton", "Close", font, sprite, new Vector2(0.64f, 0.18f), new Vector2(150f, 52f));
                _stageCloseButton.transform.SetParent(_stageSelectPanel.transform, false);
            }

            if (_stageSelectText == null && _stageSelectPanel != null)
                _stageSelectText = _stageSelectPanel.transform.Find("StageSelectText")?.GetComponent<Text>();
            if ((_stageButtons == null || _stageButtons.Length < StageCatalog.Stages.Length) && _stageSelectPanel != null)
            {
                var stageButtonList = new System.Collections.Generic.List<Button>();
                for (int i = 0; i < StageCatalog.Stages.Length; i++)
                {
                    Button found = _stageSelectPanel.transform.Find($"StageButton{i + 1}")?.GetComponent<Button>();
                    if (found != null)
                        stageButtonList.Add(found);
                }
                _stageButtons = stageButtonList.ToArray();
            }
            Button[] buttons = _stageSelectPanel != null ? _stageSelectPanel.GetComponentsInChildren<Button>() : null;
            if ((_stageStartButton == null || _stageCloseButton == null) && buttons != null && buttons.Length >= 2)
            {
                _stageStartButton = _stageSelectPanel.transform.Find("StageStartButton")?.GetComponent<Button>() ?? buttons[0];
                _stageCloseButton = _stageSelectPanel.transform.Find("StageCloseButton")?.GetComponent<Button>() ?? buttons[1];
            }
        }

        private void WireStageButtons()
        {
            if (_stageButtons == null) return;
            for (int i = 0; i < _stageButtons.Length; i++)
            {
                int index = i;
                if (_stageButtons[i] != null)
                {
                    _stageButtons[i].onClick.RemoveAllListeners();
                    _stageButtons[i].onClick.AddListener(() => SelectStage(index));
                }
            }
        }

        private void RefreshStageButtons()
        {
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            if (_stageButtons == null || progression == null) return;

            for (int i = 0; i < _stageButtons.Length && i < StageCatalog.Stages.Length; i++)
            {
                StageDefinition stage = StageCatalog.Stages[i];
                bool unlocked = progression.IsStageUnlocked(stage.stageId);
                bool cleared = progression.IsStageCleared(stage.stageId);
                bool selected = progression.CurrentStageId == stage.stageId;
                int stars = progression.GetBestStageStars(stage.stageId);
                _stageButtons[i].interactable = unlocked;

                Text label = _stageButtons[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    string status = !unlocked ? "Locked" : selected ? "Current" : cleared ? "Cleared" : "Open";
                    label.text = $"{stage.stageNumber}\n{stage.displayName}\n{status}  {stars}/3";
                }

                Image image = _stageButtons[i].GetComponent<Image>();
                if (image != null)
                    image.color = !unlocked ? new Color(0.16f, 0.18f, 0.22f, 0.9f) :
                        selected ? new Color(0.95f, 0.62f, 0.16f, 0.95f) :
                        cleared ? new Color(0.12f, 0.45f, 0.3f, 0.9f) :
                        new Color(0.08f, 0.36f, 0.7f, 0.92f);
            }
        }

        private static string BuildSelectedStageText()
        {
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            StageDefinition stage = StageCatalog.Get(progression?.CurrentStageId);
            string clearState = progression != null && progression.IsStageCleared(stage.stageId) ? "Cleared" : "Uncleared";
            int stars = progression != null ? progression.GetBestStageStars(stage.stageId) : 0;
            string warning = PlayerInventory.Instance?.BuildLoadoutCoverageWarning(stage);
            return $"Chapter 1\n{stage.Title}\n{clearState}\n\n" +
                   $"Enemy Theme: {stage.enemyTheme}\n" +
                   $"Recommended Lv {stage.recommendedLevel}  Difficulty x{stage.difficultyMultiplier:0.##}\n" +
                   $"Rewards x{stage.rewardMultiplier:0.##}  Chests: {stage.chestPreview}\n" +
                   $"Best Stars: {stars}/3\n{stage.ObjectiveText}" +
                   (string.IsNullOrWhiteSpace(warning) ? string.Empty : $"\n{warning}");
        }

        private void EnsureStorePanel()
        {
            if (_storePanel != null) return;
            Transform found = transform.Find("StorePanel");
            if (found != null)
                _storePanel = found.GetComponent<StorePanel>();
            if (_storePanel != null) return;

            var go = new GameObject("StorePanel");
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.16f, 0.14f);
            rt.anchorMax = new Vector2(0.84f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _storePanel = go.AddComponent<StorePanel>();
            go.SetActive(false);
        }

        private void EnsureEventsPanel()
        {
            if (_eventsPanel != null) return;
            Transform found = transform.Find("EventsPanel");
            if (found != null)
                _eventsPanel = found.GetComponent<EventsPanel>();
            if (_eventsPanel != null) return;

            var go = new GameObject("EventsPanel");
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.13f, 0.12f);
            rt.anchorMax = new Vector2(0.87f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _eventsPanel = go.AddComponent<EventsPanel>();
            go.SetActive(false);
        }

        private void EnsureClanPanel()
        {
            if (_clanPanel != null) return;
            Transform found = transform.Find("ClanPanel");
            if (found != null)
                _clanPanel = found.GetComponent<ClanPanel>();
            if (_clanPanel != null) return;

            var go = new GameObject("ClanPanel");
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.2f);
            rt.anchorMax = new Vector2(0.8f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _clanPanel = go.AddComponent<ClanPanel>();
            go.SetActive(false);
        }

        private void EnsureLeaderboardPanel()
        {
            if (_leaderboardPanel != null) return;
            Transform found = transform.Find("LeaderboardPanel");
            if (found != null)
                _leaderboardPanel = found.GetComponent<LeaderboardPanel>();
            if (_leaderboardPanel != null) return;

            var go = new GameObject("LeaderboardPanel");
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.18f);
            rt.anchorMax = new Vector2(0.8f, 0.84f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _leaderboardPanel = go.AddComponent<LeaderboardPanel>();
            go.SetActive(false);
        }

        private Text CreateText(string name, Font font, Vector2 anchor, Vector2 size, string value, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private Button CreateButton(string name, string label, Font font, Sprite sprite, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = name.Contains("Battle") ? new Color(1f, 0.78f, 0.08f, 0.96f) : new Color(0.08f, 0.45f, 0.9f, 0.88f);
            Button button = go.AddComponent<Button>();

            Text text = CreateText("Text", font, new Vector2(0.5f, 0.5f), Vector2.zero, label, name.Contains("Battle") ? 34 : 20);
            text.transform.SetParent(go.transform, false);
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            return button;
        }

        private static void PositionButton(Button button, Vector2 anchor, Vector2 size, string label)
        {
            if (button == null) return;
            RectTransform rt = button.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = size;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
                text.fontSize = label == "BATTLE" ? 34 : 20;
            }
        }

        private static void PositionText(Text text, Vector2 anchor, Vector2 size, int fontSize, TextAnchor alignment)
        {
            if (text == null) return;
            RectTransform rt = text.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = size;
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 13;
            text.resizeTextMaxSize = fontSize;
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

        private static Sprite CreateRuntimeSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
    }
}

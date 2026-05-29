using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;
using DragonTD.Dragons;

namespace DragonTD.UI
{
    public class ProfileProgressionPanel : MonoBehaviour
    {
        public enum PanelMode
        {
            Profile,
            Dragons
        }

        private enum DeckTab
        {
            Dragons,
            Skills,
            Parts,
            Items
        }

        [SerializeField] private Text _accountText;
        [SerializeField] private Text _syncStatusText;
        [SerializeField] private Text _summonText;
        [SerializeField] private Text _dragonListText;
        [SerializeField] private Transform _dragonButtonContainer;
        [SerializeField] private Text _dragonDetailText;
        [SerializeField] private Image _dragonPortrait;
        [SerializeField] private Text _dragonArtCaption;
        [SerializeField] private Button _equipButton;
        [SerializeField] private Button _summonButton;
        [SerializeField] private Button _levelUpButton;
        [SerializeField] private Button _trainBondButton;
        [SerializeField] private Button _evolveButton;
        private Button[] _roleFilterButtons;
        private Button[] _presetButtons;
        [SerializeField] private Button _damageButton;
        [SerializeField] private Button _attackSpeedButton;
        [SerializeField] private Button _manaButton;
        [SerializeField] private Button _resetSaveButton;
        [SerializeField] private Button _closeButton;
        private GameObject _summonPanel;
        private Text _summonPanelText;
        private Button _summonConfirmButton;
        private Button _summonCancelButton;
        private SummonResultPanel _tenPullResultPanel;
        private Text _deckTitleText;
        private Transform _deckSlotContainer;
        private Button[] _deckTabButtons;

        private int _selectedIndex;
        private PanelMode _panelMode = PanelMode.Profile;
        private DragonRoleTag? _roleFilter;
        private DeckTab _activeDeckTab = DeckTab.Dragons;

        public PanelMode CurrentMode => _panelMode;

        public void SetProfileMode()
        {
            SetPanelMode(PanelMode.Profile);
        }

        public void SetDragonsMode()
        {
            SetPanelMode(PanelMode.Dragons);
        }

        private void OnEnable()
        {
            DestroyLegacyButtons();
            EnsureSyncStatusText();
            EnsureDragonMenuControls();
            EnsureSummonControls();
            EnsureSummonPanel();
            EnsureDragonActionControls();
            EnsureRoleFilterControls();
            EnsureDragonButtonContainer();
            EnsureBattleDeckControls();
            RuntimeFontScaler.Apply(gameObject);
            WireButtons();
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += Refresh;
                PlayerInventory.Instance.OnProgressionChanged += Refresh;
                PlayerInventory.Instance.OnSyncStatusChanged += HandleSyncStatusChanged;
                PlayerInventory.Instance.OnLoadoutChanged += Refresh;
                PlayerInventory.Instance.OnSummonResult += HandleSummonResult;
            }
            Refresh();
        }

        private void OnDisable()
        {
            UnwireButtons();
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= Refresh;
                PlayerInventory.Instance.OnProgressionChanged -= Refresh;
                PlayerInventory.Instance.OnSyncStatusChanged -= HandleSyncStatusChanged;
                PlayerInventory.Instance.OnLoadoutChanged -= Refresh;
                PlayerInventory.Instance.OnSummonResult -= HandleSummonResult;
            }
        }

        public void Refresh()
        {
            EnsureSyncStatusText();
            ApplyLayout();
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            if (_accountText != null)
                _accountText.text = inventory.Progression.BuildSummary();
            if (_syncStatusText != null)
                _syncStatusText.text = $"Save: {inventory.SyncStatus}";
            if (_summonText != null)
                _summonText.text = BuildSummonText(inventory);

            int count = inventory.OwnedDragons.Count;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, count - 1));

            if (_dragonListText != null)
                _dragonListText.text = BuildDeckSummary(inventory);
            RefreshDeckSlots(inventory);
            RefreshDragonButtons(inventory);

            if (_dragonDetailText != null)
                _dragonDetailText.text = count == 0
                    ? "No dragons owned"
                    : BuildDragonDetail(inventory.OwnedDragons[_selectedIndex], inventory.Progression, inventory);

            UpdatePortrait(count == 0 ? null : inventory.OwnedDragons[_selectedIndex]);
            UpdateEquipButton(count == 0 ? null : inventory.OwnedDragons[_selectedIndex], inventory);
            UpdateDragonActionButtons(count == 0 ? null : inventory.OwnedDragons[_selectedIndex], inventory);
            UpdateDeckTabButtons();

            SetUpgradeButton(_damageButton, AccountBuffType.Damage, inventory.Progression);
            SetUpgradeButton(_attackSpeedButton, AccountBuffType.AttackSpeed, inventory.Progression);
            SetUpgradeButton(_manaButton, AccountBuffType.StartingMana, inventory.Progression);

            ApplyModeVisibility();
        }

        public void SetPanelMode(PanelMode mode)
        {
            _panelMode = mode;
            if (isActiveAndEnabled)
                Refresh();
        }

        private void WireButtons()
        {
            if (_summonButton != null) _summonButton.onClick.AddListener(SummonDragon);
            if (_summonConfirmButton != null) _summonConfirmButton.onClick.AddListener(ConfirmSummonDragon);
            if (_summonCancelButton != null) _summonCancelButton.onClick.AddListener(HideSummonPanel);
            if (_levelUpButton != null) _levelUpButton.onClick.AddListener(LevelUpSelectedDragon);
            if (_trainBondButton != null) _trainBondButton.onClick.AddListener(TrainSelectedDragonBond);
            if (_evolveButton != null) _evolveButton.onClick.AddListener(EvolveSelectedDragon);
            WireDeckTabButtons();
            WireRoleFilterButtons();
            WirePresetButtons();
            if (_damageButton != null) _damageButton.onClick.AddListener(() => Upgrade(AccountBuffType.Damage));
            if (_attackSpeedButton != null) _attackSpeedButton.onClick.AddListener(() => Upgrade(AccountBuffType.AttackSpeed));
            if (_manaButton != null) _manaButton.onClick.AddListener(() => Upgrade(AccountBuffType.StartingMana));
            if (_resetSaveButton != null) _resetSaveButton.onClick.AddListener(ResetSave);
            if (_closeButton != null) _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void UnwireButtons()
        {
            if (_summonButton != null) _summonButton.onClick.RemoveAllListeners();
            if (_summonConfirmButton != null) _summonConfirmButton.onClick.RemoveAllListeners();
            if (_summonCancelButton != null) _summonCancelButton.onClick.RemoveAllListeners();
            if (_levelUpButton != null) _levelUpButton.onClick.RemoveAllListeners();
            if (_trainBondButton != null) _trainBondButton.onClick.RemoveAllListeners();
            if (_evolveButton != null) _evolveButton.onClick.RemoveAllListeners();
            UnwireButtons(_deckTabButtons);
            UnwireButtons(_roleFilterButtons);
            UnwireButtons(_presetButtons);
            if (_damageButton != null) _damageButton.onClick.RemoveAllListeners();
            if (_attackSpeedButton != null) _attackSpeedButton.onClick.RemoveAllListeners();
            if (_manaButton != null) _manaButton.onClick.RemoveAllListeners();
            if (_resetSaveButton != null) _resetSaveButton.onClick.RemoveAllListeners();
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
        }

        private static void UnwireButtons(Button[] buttons)
        {
            if (buttons == null) return;
            foreach (Button button in buttons)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }
        }

        private void WireRoleFilterButtons()
        {
            if (_roleFilterButtons == null || _roleFilterButtons.Length == 0) return;
            _roleFilterButtons[0]?.onClick.AddListener(() => SetRoleFilter(null));
            int index = 1;
            foreach (DragonRoleTag role in System.Enum.GetValues(typeof(DragonRoleTag)))
            {
                int buttonIndex = index;
                DragonRoleTag selectedRole = role;
                if (buttonIndex < _roleFilterButtons.Length && _roleFilterButtons[buttonIndex] != null)
                    _roleFilterButtons[buttonIndex].onClick.AddListener(() => SetRoleFilter(selectedRole));
                index++;
            }
        }

        private void WirePresetButtons()
        {
            if (_presetButtons == null || _presetButtons.Length < 4) return;
            _presetButtons[0]?.onClick.AddListener(() => ApplyPreset(LoadoutPresetType.Balanced));
            _presetButtons[1]?.onClick.AddListener(() => ApplyPreset(LoadoutPresetType.Boss));
            _presetButtons[2]?.onClick.AddListener(() => ApplyPreset(LoadoutPresetType.FastEnemies));
            _presetButtons[3]?.onClick.AddListener(() => ApplyPreset(LoadoutPresetType.ShieldBreak));
        }

        private void WireDeckTabButtons()
        {
            if (_deckTabButtons == null || _deckTabButtons.Length < 4) return;
            _deckTabButtons[0]?.onClick.AddListener(() => SetDeckTab(DeckTab.Dragons));
            _deckTabButtons[1]?.onClick.AddListener(() => SetDeckTab(DeckTab.Skills));
            _deckTabButtons[2]?.onClick.AddListener(() => SetDeckTab(DeckTab.Parts));
            _deckTabButtons[3]?.onClick.AddListener(() => SetDeckTab(DeckTab.Items));
        }

        private void Upgrade(AccountBuffType buffType)
        {
            if (PlayerInventory.Instance == null) return;
            PlayerInventory.Instance.TryUpgradeAccountBuff(buffType, out _);
            Refresh();
        }

        private void ToggleSelectedEquip()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null || inventory.OwnedDragons.Count == 0) return;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, inventory.OwnedDragons.Count - 1);
            inventory.TryToggleEquipDragon(inventory.OwnedDragons[_selectedIndex], out _);
            Refresh();
        }

        private void LevelUpSelectedDragon()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null || inventory.OwnedDragons.Count == 0) return;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, inventory.OwnedDragons.Count - 1);
            inventory.TryLevelUpDragon(inventory.OwnedDragons[_selectedIndex], out _);
            Refresh();
        }

        private void TrainSelectedDragonBond()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null || inventory.OwnedDragons.Count == 0) return;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, inventory.OwnedDragons.Count - 1);
            inventory.TryTrainDragonBond(inventory.OwnedDragons[_selectedIndex], out _);
            Refresh();
        }

        private void EvolveSelectedDragon()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null || inventory.OwnedDragons.Count == 0) return;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, inventory.OwnedDragons.Count - 1);
            inventory.TryEvolveDragon(inventory.OwnedDragons[_selectedIndex], out _);
            Refresh();
        }

        private void SetRoleFilter(DragonRoleTag? role)
        {
            _roleFilter = role;
            _activeDeckTab = DeckTab.Dragons;
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory != null && inventory.OwnedDragons.Count > 0)
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, inventory.OwnedDragons.Count - 1);
            if (inventory != null && inventory.OwnedDragons.Count > 0 && _roleFilter.HasValue && !DragonRoleUtility.HasRole(inventory.OwnedDragons[_selectedIndex], _roleFilter.Value))
            {
                for (int i = 0; i < inventory.OwnedDragons.Count; i++)
                {
                    if (DragonRoleUtility.HasRole(inventory.OwnedDragons[i], _roleFilter.Value))
                    {
                        _selectedIndex = i;
                        break;
                    }
                }
            }
            Refresh();
        }

        private void SetDeckTab(DeckTab tab)
        {
            _activeDeckTab = tab;
            Refresh();
        }

        private void ApplyPreset(LoadoutPresetType preset)
        {
            PlayerInventory.Instance?.TryApplyLoadoutPreset(preset, out _);
            Refresh();
        }

        private void SummonDragon()
        {
            ShowSummonPanel();
        }

        private void ConfirmSummonDragon()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;
            int beforeCount = inventory.OwnedDragons.Count;
            inventory.TrySummonDragon(out _);
            if (inventory.OwnedDragons.Count > beforeCount)
                _selectedIndex = inventory.OwnedDragons.Count - 1;
            UpdateSummonPanelText();
            Refresh();
        }

        private void ShowSummonPanel()
        {
            EnsureSummonPanel();
            UpdateSummonPanelText();
            if (_summonPanel != null)
                _summonPanel.SetActive(true);
        }

        private void HideSummonPanel()
        {
            if (_summonPanel != null)
                _summonPanel.SetActive(false);
        }

        private void HandleSyncStatusChanged(string status)
        {
            EnsureSyncStatusText();
            if (_syncStatusText != null)
                _syncStatusText.text = $"Save: {status}";
        }

        private void HandleSummonResult(string _)
        {
            Refresh();
        }

        private void EnsureSyncStatusText()
        {
            if (_syncStatusText != null || _accountText == null) return;

            var go = new GameObject("SyncStatusText");
            go.transform.SetParent(_accountText.transform.parent, false);
            RectTransform sourceRt = _accountText.GetComponent<RectTransform>();
            RectTransform rt = go.AddComponent<RectTransform>();
            if (sourceRt != null)
            {
                rt.anchorMin = sourceRt.anchorMin;
                rt.anchorMax = sourceRt.anchorMax;
                rt.pivot = sourceRt.pivot;
                rt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -126f);
                rt.sizeDelta = new Vector2(sourceRt.sizeDelta.x, 28f);
            }

            _syncStatusText = go.AddComponent<Text>();
            _syncStatusText.font = _accountText.font;
            _syncStatusText.fontSize = Mathf.Max(15, _accountText.fontSize - 1);
            _syncStatusText.color = new Color(0.78f, 0.9f, 1f, 1f);
            _syncStatusText.alignment = TextAnchor.UpperLeft;
            _syncStatusText.text = "Save: Local";
            RuntimeFontScaler.Apply(go);
        }

        private void EnsureDragonMenuControls()
        {
            if (_dragonDetailText == null) return;

            Transform parent = _dragonDetailText.transform.parent;
            Font font = _dragonDetailText.font;

            if (_dragonPortrait == null)
            {
                var portraitGO = new GameObject("SelectedDragonPortrait");
                portraitGO.transform.SetParent(parent, false);
                RectTransform rt = portraitGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.52f, 0.56f);
                rt.anchorMax = new Vector2(0.52f, 0.56f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(128f, 128f);
                _dragonPortrait = portraitGO.AddComponent<Image>();
                _dragonPortrait.color = new Color(0.22f, 0.32f, 0.42f, 0.9f);
                _dragonPortrait.preserveAspect = true;
            }

            if (_dragonArtCaption == null)
            {
                var captionGO = new GameObject("DragonArtCaption");
                captionGO.transform.SetParent(parent, false);
                RectTransform rt = captionGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.52f, 0.43f);
                rt.anchorMax = new Vector2(0.52f, 0.43f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(260f, 54f);
                _dragonArtCaption = captionGO.AddComponent<Text>();
                _dragonArtCaption.font = font;
                _dragonArtCaption.fontSize = 18;
                _dragonArtCaption.alignment = TextAnchor.MiddleCenter;
                _dragonArtCaption.color = new Color(1f, 0.92f, 0.64f, 1f);
            }

            ApplyLayout();
        }

        private void EnsureSummonControls()
        {
            if (_accountText == null) return;

            Transform parent = _accountText.transform.parent;
            Font font = _accountText.font;

            if (_summonText == null)
            {
                var go = new GameObject("SummonText");
                go.transform.SetParent(parent, false);
                RectTransform rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(36f, -232f);
                rt.sizeDelta = new Vector2(560f, 58f);
                _summonText = go.AddComponent<Text>();
                _summonText.font = font;
                _summonText.fontSize = 18;
                _summonText.color = new Color(0.9f, 1f, 0.86f, 1f);
                _summonText.alignment = TextAnchor.UpperLeft;
            }

            if (_summonButton == null)
            {
                _summonButton = CreateRuntimeButton(parent, "SummonDragonButton", "Summon Dragon", font,
                    new Vector2(0.22f, 0.58f), new Vector2(240f, 46f));
            }

            RectTransform listRt = _dragonListText != null ? _dragonListText.GetComponent<RectTransform>() : null;
            if (listRt != null && listRt.anchoredPosition.y > -285f)
            {
                listRt.anchoredPosition = new Vector2(listRt.anchoredPosition.x, -304f);
                listRt.sizeDelta = new Vector2(listRt.sizeDelta.x, 260f);
            }

            EnsureDragonButtonContainer();
            ApplyLayout();
        }

        private void EnsureSummonPanel()
        {
            if (_summonPanel != null || _accountText == null) return;

            Transform parent = _accountText.transform.parent;
            Font font = _accountText.font;
            _summonPanel = new GameObject("SummonResultPanel");
            _summonPanel.transform.SetParent(parent, false);
            RectTransform rt = _summonPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.30f, 0.30f);
            rt.anchorMax = new Vector2(0.70f, 0.76f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image image = _summonPanel.AddComponent<Image>();
            image.color = new Color(0.03f, 0.08f, 0.15f, 0.98f);

            var textGO = new GameObject("SummonResultText");
            textGO.transform.SetParent(_summonPanel.transform, false);
            RectTransform textRt = textGO.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.08f, 0.34f);
            textRt.anchorMax = new Vector2(0.92f, 0.92f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _summonPanelText = textGO.AddComponent<Text>();
            _summonPanelText.font = font;
            _summonPanelText.fontSize = 20;
            _summonPanelText.alignment = TextAnchor.MiddleCenter;
            _summonPanelText.color = Color.white;

            _summonConfirmButton = CreateRuntimeButton(_summonPanel.transform, "ConfirmSummonButton", "Use Ticket", font,
                new Vector2(0.35f, 0.18f), new Vector2(170f, 42f));
            _summonCancelButton = CreateRuntimeButton(_summonPanel.transform, "CancelSummonButton", "Close", font,
                new Vector2(0.84f, 0.18f), new Vector2(140f, 42f));

            var gemPullButton = CreateRuntimeButton(_summonPanel.transform, "GemSinglePullButton", "300 Gems", font,
                new Vector2(0.50f, 0.18f), new Vector2(140f, 42f));
            gemPullButton.onClick.AddListener(() =>
            {
                PlayerInventory.Instance?.TrySummonDragonWithGems(out _);
                UpdateSummonPanelText();
                Refresh();
            });

            var tenPullButton = CreateRuntimeButton(_summonPanel.transform, "TenPullButton", "2700\n(10x)", font,
                new Vector2(0.65f, 0.18f), new Vector2(130f, 42f));
            tenPullButton.onClick.AddListener(() =>
            {
                PlayerInventory inv = PlayerInventory.Instance;
                if (inv != null && inv.TryTenPullWithGems(out _))
                {
                    EnsureTenPullResultPanel();
                    _tenPullResultPanel.Show(inv.LastTenPullResults);
                }
                UpdateSummonPanelText();
                Refresh();
            });

            _summonPanel.SetActive(false);
        }

        private void EnsureTenPullResultPanel()
        {
            if (_tenPullResultPanel != null) return;
            Font font = _dragonDetailText != null ? _dragonDetailText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("TenPullResultPanelHost");
            go.transform.SetParent(transform, false);
            _tenPullResultPanel = go.AddComponent<SummonResultPanel>();
            _tenPullResultPanel.Initialize(transform, font);
        }

        private void UpdateSummonPanelText()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (_summonPanelText != null && inventory != null)
            {
                int pullsSince = inventory.Progression.GachaPullsSinceLastEpic;
                int totalPulls = inventory.Progression.GachaTotalPulls;
                string pityInfo = pullsSince >= 50
                    ? $"Soft pity active! ({pullsSince}/100)"
                    : $"Pity: {pullsSince}/100 pulls since Epic+";
                _summonPanelText.text =
                    $"Dragon Summon\n\n" +
                    $"Tickets: {inventory.Progression.SummonTickets}  |  Gems: {inventory.Progression.Gems}\n" +
                    $"1 Ticket or 300 Gems per pull  |  10-Pull: 2700 Gems\n" +
                    $"{pityInfo}\n" +
                    $"Total pulls: {totalPulls}\n" +
                    $"{(string.IsNullOrWhiteSpace(inventory.LastSummonSummary) ? "Use a ticket or gems to summon." : inventory.LastSummonSummary)}";
            }

            if (_summonConfirmButton != null && inventory != null)
                _summonConfirmButton.interactable = inventory.Progression.SummonTickets > 0;
        }

        private void EnsureDragonActionControls()
        {
            if (_dragonDetailText == null) return;

            Transform parent = _dragonDetailText.transform.parent;
            Font font = _dragonDetailText.font;
            if (_levelUpButton == null)
                _levelUpButton = CreateRuntimeButton(parent, "LevelUpDragonButton", "Level Up", font,
                    new Vector2(0.57f, 0.15f), new Vector2(150f, 38f));
            if (_trainBondButton == null)
                _trainBondButton = CreateRuntimeButton(parent, "TrainBondButton", "Train Bond", font,
                    new Vector2(0.70f, 0.15f), new Vector2(170f, 38f));
            if (_evolveButton == null)
                _evolveButton = CreateRuntimeButton(parent, "EvolveDragonButton", "Evolve", font,
                    new Vector2(0.84f, 0.15f), new Vector2(150f, 38f));

            ApplyLayout();
        }

        private void EnsureRoleFilterControls()
        {
            if (_accountText == null) return;
            if (_roleFilterButtons != null && _roleFilterButtons.Length >= 7) return;

            Transform parent = _accountText.transform.parent;
            Font font = _accountText.font;
            _roleFilterButtons = new Button[7];
            _roleFilterButtons[0] = CreateRuntimeButton(parent, "RoleFilterAllButton", "All", font, new Vector2(0.08f, 0.75f), new Vector2(70f, 32f));
            int index = 1;
            foreach (DragonRoleTag role in System.Enum.GetValues(typeof(DragonRoleTag)))
            {
                _roleFilterButtons[index] = CreateRuntimeButton(parent, $"RoleFilter{role}Button", DragonRoleUtility.Label(role), font,
                    new Vector2(0.08f + index * 0.075f, 0.75f), new Vector2(92f, 32f));
                index++;
            }
            ApplyLayout();
        }

        private void EnsurePresetControls()
        {
            if (_accountText == null) return;
            if (_presetButtons != null && _presetButtons.Length >= 4) return;

            Transform parent = _accountText.transform.parent;
            Font font = _accountText.font;
            _presetButtons = new Button[4];
            _presetButtons[0] = CreateRuntimeButton(parent, "PresetBalancedButton", "Balanced", font, new Vector2(0.14f, 0.23f), new Vector2(120f, 34f));
            _presetButtons[1] = CreateRuntimeButton(parent, "PresetBossButton", "Boss", font, new Vector2(0.25f, 0.23f), new Vector2(110f, 34f));
            _presetButtons[2] = CreateRuntimeButton(parent, "PresetFastButton", "Fast", font, new Vector2(0.36f, 0.23f), new Vector2(110f, 34f));
            _presetButtons[3] = CreateRuntimeButton(parent, "PresetShieldButton", "Shield", font, new Vector2(0.47f, 0.23f), new Vector2(120f, 34f));
            ApplyLayout();
        }

        private void EnsureDragonButtonContainer()
        {
            if (_dragonButtonContainer != null || _accountText == null) return;

            var go = new GameObject("DragonButtonContainer");
            go.transform.SetParent(_accountText.transform.parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(36f, -382f);
            rt.sizeDelta = new Vector2(560f, 210f);
            _dragonButtonContainer = go.transform;
            ApplyLayout();
        }

        private void EnsureBattleDeckControls()
        {
            if (_accountText == null) return;

            Transform parent = _accountText.transform.parent;
            Font font = _accountText.font;

            if (_deckTitleText == null)
                _deckTitleText = CreateRuntimeText(parent, "BattleDeckTitleText", "BATTLE DECK", font, 36, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.72f, 1f));

            if (_deckSlotContainer == null)
            {
                var slotsGO = new GameObject("EquippedDragonDeckSlots");
                slotsGO.transform.SetParent(parent, false);
                slotsGO.AddComponent<RectTransform>();
                _deckSlotContainer = slotsGO.transform;
            }

            if (_deckTabButtons == null || _deckTabButtons.Length < 4)
            {
                _deckTabButtons = new Button[4];
                _deckTabButtons[0] = CreateRuntimeButton(parent, "DeckTabDragonsButton", "Dragons", font, new Vector2(0.20f, 0.48f), new Vector2(160f, 44f));
                _deckTabButtons[1] = CreateRuntimeButton(parent, "DeckTabSkillsButton", "Skills", font, new Vector2(0.38f, 0.48f), new Vector2(160f, 44f));
                _deckTabButtons[2] = CreateRuntimeButton(parent, "DeckTabPartsButton", "Parts", font, new Vector2(0.56f, 0.48f), new Vector2(160f, 44f));
                _deckTabButtons[3] = CreateRuntimeButton(parent, "DeckTabItemsButton", "Items", font, new Vector2(0.74f, 0.48f), new Vector2(160f, 44f));
            }

            ApplyLayout();
        }

        private void ApplyLayout()
        {
            bool dragonsMode = _panelMode == PanelMode.Dragons;
            SetTextRect(_accountText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -78f), dragonsMode ? new Vector2(430f, 70f) : new Vector2(500f, 230f), TextAnchor.UpperLeft, dragonsMode ? 14 : 17);
            SetTextRect(_syncStatusText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, dragonsMode ? -72f : -314f), new Vector2(250f, 26f), TextAnchor.UpperLeft, 14);
            SetTextRect(_deckTitleText, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(520f, 58f), TextAnchor.MiddleCenter, 34);
            SetTextRect(_summonText, new Vector2(0.06f, 0.76f), new Vector2(0.28f, 0.76f), Vector2.zero, new Vector2(0f, 58f), TextAnchor.UpperLeft, 15);
            SetTextRect(_dragonListText, new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.50f), Vector2.zero, new Vector2(0f, 30f), TextAnchor.MiddleLeft, 16);
            SetRect(_deckSlotContainer as RectTransform, new Vector2(0.07f, 0.58f), new Vector2(0.93f, 0.74f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            SetRect(_dragonButtonContainer as RectTransform, new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.44f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            SetRect(_dragonPortrait != null ? _dragonPortrait.rectTransform : null, new Vector2(0.69f, 0.68f), new Vector2(0.69f, 0.68f), Vector2.zero, new Vector2(150f, 150f));
            SetTextRect(_dragonArtCaption, new Vector2(0.69f, 0.52f), new Vector2(0.69f, 0.52f), Vector2.zero, new Vector2(300f, 48f), TextAnchor.MiddleCenter, 16);
            SetTextRect(_dragonDetailText, new Vector2(0.08f, 0.155f), new Vector2(0.49f, 0.155f), Vector2.zero, new Vector2(0f, 88f), TextAnchor.UpperLeft, 14);

            SetButtonRect(_summonButton, new Vector2(0.84f, 0.76f), new Vector2(170f, 40f));
            SetButtonRect(_levelUpButton, new Vector2(0.54f, 0.145f), new Vector2(130f, 34f));
            SetButtonRect(_trainBondButton, new Vector2(0.68f, 0.145f), new Vector2(150f, 34f));
            SetButtonRect(_evolveButton, new Vector2(0.82f, 0.145f), new Vector2(130f, 34f));
            SetButtonArray(_deckTabButtons, new Vector2(0.20f, 0.485f), new Vector2(0.18f, 0f), new Vector2(160f, 42f));
            SetButtonArray(_roleFilterButtons, new Vector2(0.10f, 0.465f), new Vector2(0.11f, 0f), new Vector2(92f, 30f));
            SetButtonArray(_presetButtons, new Vector2(0.16f, 0.535f), new Vector2(0.15f, 0f), new Vector2(128f, 32f));

            SetButtonRect(_damageButton, new Vector2(0.28f, 0.14f), new Vector2(260f, 40f));
            SetButtonRect(_attackSpeedButton, new Vector2(0.50f, 0.14f), new Vector2(260f, 40f));
            SetButtonRect(_manaButton, new Vector2(0.72f, 0.14f), new Vector2(260f, 40f));
            SetButtonRect(_resetSaveButton, new Vector2(0.42f, 0.055f), new Vector2(170f, 38f));
            SetButtonRect(_closeButton, new Vector2(0.58f, 0.055f), new Vector2(170f, 38f));
            ApplyModeVisibility();
        }

        private void ApplyModeVisibility()
        {
            bool dragonsMode = _panelMode == PanelMode.Dragons;
            bool dragonTab = dragonsMode && _activeDeckTab == DeckTab.Dragons;
            bool profileMode = !dragonsMode;

            SetActive(_accountText, profileMode);
            SetActive(_syncStatusText, profileMode);
            SetActive(_summonText, dragonsMode);
            SetActive(_summonButton, dragonsMode);
            SetActive(_deckTitleText, dragonsMode);
            SetActive(_deckSlotContainer, dragonsMode);
            SetActive(_deckTabButtons, dragonsMode);
            SetStaticProfileHeaderVisible(profileMode);
            SetActive(_dragonListText, dragonsMode);
            SetActive(_dragonButtonContainer, dragonsMode);
            SetActive(_dragonDetailText, dragonTab);
            SetActive(_dragonPortrait, false);
            SetActive(_dragonArtCaption, false);
            SetActive(_levelUpButton, dragonTab);
            SetActive(_trainBondButton, dragonTab);
            SetActive(_evolveButton, dragonTab);
            SetActive(_roleFilterButtons, false);
            SetActive(_presetButtons, false);
            SetActive(_equipButton, false);

            SetActive(_damageButton, profileMode);
            SetActive(_attackSpeedButton, profileMode);
            SetActive(_manaButton, profileMode);
            SetActive(_resetSaveButton, profileMode);
            SetActive(_closeButton, true);
            if (_summonPanel != null && !dragonsMode)
                _summonPanel.SetActive(false);
        }

        private void SetStaticProfileHeaderVisible(bool visible)
        {
            SetChildActive("ProfileTitleText", visible);
            SetChildActive("ProfileSubtitleText", visible);
            SetChildActive("SubtitleText", visible);
        }

        private void DestroyLegacyButtons()
        {
            foreach (string name in new[] { "PreviousDragonButton", "NextDragonButton", "EquipDragonButton" })
            {
                Transform child = transform.Find(name);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        private void SetChildActive(string childName, bool active)
        {
            Transform child = transform.Find(childName);
            if (child != null)
                child.gameObject.SetActive(active);
        }

        private static void SetTextRect(Text text, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, int fontSize)
        {
            if (text == null) return;
            Vector2 pivot = alignment == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            SetRect(text.rectTransform, anchorMin, anchorMax, anchoredPosition, size, pivot);
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.resizeTextForBestFit = false;
        }

        private static void SetButtonRect(Button button, Vector2 anchor, Vector2 size)
        {
            if (button == null) return;
            SetRect(button.GetComponent<RectTransform>(), anchor, anchor, Vector2.zero, size, new Vector2(0.5f, 0.5f));
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 18;
            }
        }

        private static void SetButtonArray(Button[] buttons, Vector2 startAnchor, Vector2 step, Vector2 size)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
                SetButtonRect(buttons[i], startAnchor + step * i, i == 0 ? new Vector2(72f, size.y) : size);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

        private static void SetActive(Button[] buttons, bool active)
        {
            if (buttons == null) return;
            foreach (Button button in buttons)
            {
                if (button != null)
                    button.gameObject.SetActive(active);
            }
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            SetRect(rt, anchorMin, anchorMax, anchoredPosition, size, new Vector2(0.5f, 0.5f));
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
        }

        private static Button CreateRuntimeButton(Transform parent, string name, string label, Font font, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.4f, 0.95f);
            Button button = go.AddComponent<Button>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            RectTransform textRt = textGO.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            Text text = textGO.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return button;
        }

        private static Text CreateRuntimeText(Transform parent, string name, string label, Font font, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 60f);

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = label;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private void RefreshDragonButtons(PlayerInventory inventory)
        {
            if (_dragonButtonContainer == null || inventory == null) return;

            foreach (Transform child in _dragonButtonContainer)
                Destroy(child.gameObject);

            Font font = _dragonDetailText != null
                ? _dragonDetailText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (_activeDeckTab != DeckTab.Dragons)
            {
                CreatePlaceholderInventoryCard(_dragonButtonContainer, font, _activeDeckTab);
                return;
            }

            int card = 0;
            for (int i = 0; i < inventory.OwnedDragons.Count && card < 12; i++)
            {
                DragonInstance dragon = inventory.OwnedDragons[i];
                if (dragon?.Definition == null) continue;
                if (_roleFilter.HasValue && !DragonRoleUtility.HasRole(dragon, _roleFilter.Value)) continue;
                int index = i;
                Button button = CreateDragonCardButton(_dragonButtonContainer, font, inventory, dragon, i, card, index == _selectedIndex);
                button.onClick.AddListener(() => SelectDragon(index));
                card++;
            }
        }

        private void RefreshDeckSlots(PlayerInventory inventory)
        {
            if (_deckSlotContainer == null || inventory == null) return;

            foreach (Transform child in _deckSlotContainer)
                Destroy(child.gameObject);

            Font font = _dragonDetailText != null
                ? _dragonDetailText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var equipped = inventory.GetBattleDragons();
            for (int i = 0; i < PlayerInventory.MaxEquippedDragons; i++)
            {
                DragonInstance dragon = i < equipped.Count ? equipped[i] : null;
                int slotIndex = i;
                Button button = CreateDeckSlotButton(_deckSlotContainer, font, inventory, dragon, slotIndex);
                if (dragon?.Definition != null)
                    button.onClick.AddListener(() => TapDeckSlot(dragon));
            }
        }

        private Button CreateDeckSlotButton(Transform parent, Font font, PlayerInventory inventory, DragonInstance dragon, int slot)
        {
            var go = new GameObject($"EquippedDragonSlot{slot + 1}");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            int column = slot % 6;
            rt.anchorMin = new Vector2(column / 6f + 0.008f, 0.02f);
            rt.anchorMax = new Vector2((column + 1) / 6f - 0.008f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = dragon?.Definition == null
                ? new Color(0.06f, 0.12f, 0.18f, 0.86f)
                : Color.Lerp(GetRarityColor(dragon), new Color(0.02f, 0.05f, 0.08f, 1f), 0.34f);
            Button button = go.AddComponent<Button>();
            var dropHandler = go.AddComponent<DeckSlotDropHandler>();
            dropHandler.EquippedDragonId = dragon?.Definition?.dragonId ?? string.Empty;

            Sprite slotPortrait = dragon?.Definition?.visualData?.portrait;
            if (slotPortrait != null)
            {
                var pGO = new GameObject("Portrait");
                pGO.transform.SetParent(go.transform, false);
                var pRt = pGO.AddComponent<RectTransform>();
                pRt.anchorMin = Vector2.zero;
                pRt.anchorMax = Vector2.one;
                pRt.offsetMin = Vector2.zero;
                pRt.offsetMax = Vector2.zero;
                var pImg = pGO.AddComponent<Image>();
                pImg.sprite = slotPortrait;
                pImg.preserveAspect = true;
            }

            string label = dragon?.Definition == null
                ? $"Slot {slot + 1}\nEmpty\nEquip Dragon"
                : $"[{slot + 1}] {dragon.Definition.displayName}\nLv {dragon.Level}  {dragon.Definition.rarity}\n{dragon.Definition.element}\n{ShortRoleText(dragon)}";
            AddCardText(go.transform, font, label, 13, TextAnchor.MiddleCenter);
            return button;
        }

        private Button CreateDragonCardButton(Transform parent, Font font, PlayerInventory inventory, DragonInstance dragon, int ownedIndex, int cardIndex, bool selected)
        {
            var go = new GameObject($"DragonCollectionCard{cardIndex + 1}");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            int columns = 4;
            int column = cardIndex % columns;
            int row = cardIndex / columns;
            rt.anchorMin = new Vector2(column / (float)columns + 0.01f, 1f - (row + 1) / 3f + 0.02f);
            rt.anchorMax = new Vector2((column + 1) / (float)columns - 0.01f, 1f - row / 3f - 0.02f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image = go.AddComponent<Image>();
            Color rarity = GetRarityColor(dragon);
            image.color = selected ? new Color(0.95f, 0.66f, 0.16f, 0.96f) : Color.Lerp(rarity, new Color(0.03f, 0.07f, 0.12f, 1f), 0.22f);
            Button button = go.AddComponent<Button>();
            go.AddComponent<CanvasGroup>();
            var dragHandler = go.AddComponent<DragCardHandler>();
            dragHandler.DragonId = dragon?.Definition?.dragonId ?? string.Empty;

            Sprite portrait = dragon?.Definition?.visualData?.portrait;
            if (portrait != null)
            {
                var pGO = new GameObject("Portrait");
                pGO.transform.SetParent(go.transform, false);
                var pRt = pGO.AddComponent<RectTransform>();
                pRt.anchorMin = Vector2.zero;
                pRt.anchorMax = Vector2.one;
                pRt.offsetMin = Vector2.zero;
                pRt.offsetMax = Vector2.zero;
                var pImg = pGO.AddComponent<Image>();
                pImg.sprite = portrait;
                pImg.preserveAspect = true;
            }

            string equipped = inventory.IsEquipped(dragon) ? "EQUIPPED" : "OWNED";
            string label =
                $"{equipped}\n" +
                $"{ownedIndex + 1}. {dragon.Definition.displayName}\n" +
                $"Lv {dragon.Level}  {dragon.Definition.rarity}\n" +
                $"{dragon.Definition.element}  {ShortRoleText(dragon)}";
            AddCardText(go.transform, font, label, 13, TextAnchor.MiddleCenter);
            return button;
        }

        private void CreatePlaceholderInventoryCard(Transform parent, Font font, DeckTab tab)
        {
            var go = new GameObject($"{tab}PlaceholderCard");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.14f, 0.18f);
            rt.anchorMax = new Vector2(0.86f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.05f, 0.13f, 0.22f, 0.92f);
            string title = tab switch
            {
                DeckTab.Skills => "Battle Skills",
                DeckTab.Parts => "Dragon Parts",
                DeckTab.Items => "Items",
                _ => "Collection"
            };
            string body = tab switch
            {
                DeckTab.Skills => "Extra battle skills will appear here.",
                DeckTab.Parts => "Dragon equipment and stat parts will appear here.",
                DeckTab.Items => "Level-up items, summon tickets, and materials will appear here.",
                _ => "No entries"
            };
            AddCardText(go.transform, font, $"{title}\n\n{body}", 20, TextAnchor.MiddleCenter);
        }

        private static void AddCardText(Transform parent, Font font, string label, int fontSize, TextAnchor alignment)
        {
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(parent, false);
            RectTransform textRt = textGO.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 6f);
            textRt.offsetMax = new Vector2(-8f, -6f);
            Text text = textGO.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = label;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
        }

        private void SelectDragon(int index)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null || inventory.OwnedDragons.Count == 0) return;
            _selectedIndex = Mathf.Clamp(index, 0, inventory.OwnedDragons.Count - 1);
            Refresh();
        }

        private void TapDeckSlot(DragonInstance dragon)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null || dragon?.Definition == null) return;
            // Set selection index directly without triggering a Refresh —
            // TryToggleEquipDragon fires OnLoadoutChanged which calls Refresh via the event subscription.
            for (int i = 0; i < inventory.OwnedDragons.Count; i++)
            {
                if (inventory.OwnedDragons[i] == dragon || inventory.OwnedDragons[i]?.Definition?.dragonId == dragon.Definition.dragonId)
                {
                    _selectedIndex = i;
                    _activeDeckTab = DeckTab.Dragons;
                    break;
                }
            }
            inventory.TryToggleEquipDragon(dragon, out _);
            // Refresh() will be called via OnLoadoutChanged event
        }

        private void SelectOwnedDragon(DragonInstance selected)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null || selected?.Definition == null) return;
            for (int i = 0; i < inventory.OwnedDragons.Count; i++)
            {
                DragonInstance candidate = inventory.OwnedDragons[i];
                if (candidate == selected || candidate?.Definition?.dragonId == selected.Definition.dragonId)
                {
                    _selectedIndex = i;
                    _activeDeckTab = DeckTab.Dragons;
                    Refresh();
                    return;
                }
            }
        }

        private void UpdateDeckTabButtons()
        {
            if (_deckTabButtons == null) return;
            for (int i = 0; i < _deckTabButtons.Length; i++)
            {
                Button button = _deckTabButtons[i];
                if (button == null) continue;
                bool active = (int)_activeDeckTab == i;
                Image image = button.GetComponent<Image>();
                if (image != null)
                    image.color = active ? new Color(0.86f, 0.34f, 0.88f, 0.96f) : new Color(0.12f, 0.28f, 0.56f, 0.94f);
            }
        }

        private void ResetSave()
        {
            PlayerInventory.Instance?.ResetLocalProgression();
            _selectedIndex = 0;
            Refresh();
        }

        private static string BuildDragonList(PlayerInventory inventory)
        {
            if (inventory.OwnedDragons.Count == 0) return "No dragons owned";
            return $"Battle Loadout: {inventory.EquippedDragonIds.Count}/{PlayerInventory.MaxEquippedDragons}  Coverage: {inventory.BuildLoadoutCoverageReport()}";
        }

        private string BuildDeckSummary(PlayerInventory inventory)
        {
            if (_activeDeckTab == DeckTab.Dragons)
                return BuildDragonList(inventory);

            return _activeDeckTab switch
            {
                DeckTab.Skills => "Skills: user battle skills will be equipped here in a later pass.",
                DeckTab.Parts => "Parts: dragon equipment slots will be equipped here in a later pass.",
                DeckTab.Items => "Items: level-up items, summon tickets, and materials will be shown here.",
                _ => "Collection"
            };
        }

        private static string BuildDragonRowLabel(PlayerInventory inventory, DragonInstance dragon, int index)
        {
            string equipped = inventory.IsEquipped(dragon) ? "[E]" : "[ ]";
            return $"{equipped} {index + 1}. {dragon.Definition.displayName}  {dragon.Definition.rarity}  {DragonRoleUtility.BuildRoleText(dragon)}";
        }

        private static string ShortRoleText(DragonInstance dragon)
        {
            string roles = DragonRoleUtility.BuildRoleText(dragon);
            if (string.IsNullOrWhiteSpace(roles))
                return "Flexible";
            return roles.Length <= 26 ? roles : roles.Substring(0, 23) + "...";
        }

        private static string BuildSummonText(PlayerInventory inventory)
        {
            string result = string.IsNullOrWhiteSpace(inventory.LastSummonSummary)
                ? "Use 1 Dragon Summon Ticket to add a dragon to your collection."
                : inventory.LastSummonSummary;
            return $"Summon Tickets: {inventory.Progression.SummonTickets}\n{result}";
        }

        private static string BuildDragonDetail(DragonInstance dragon, PlayerProgression progression, PlayerInventory inventory)
        {
            if (dragon?.Definition == null) return "No dragon selected";

            float baseAttack = dragon.Definition.baseStats.attack;
            float boostedAttack = dragon.Attack;
            float baseSpeed = dragon.Definition.baseStats.attackSpeed;
            float boostedSpeed = dragon.AttackSpeed;
            bool equipped = inventory != null && inventory.IsEquipped(dragon);
            string activeSkill = dragon.Definition.ActiveSkill != null ? dragon.Definition.ActiveSkill.displayName : "None";
            string compare = BuildCompareText(dragon, inventory);
            return $"{dragon.Definition.displayName} {(equipped ? "[Equipped]" : "[Reserve]")}\n" +
                   $"{dragon.Definition.rarity} {dragon.Definition.element} {dragon.Definition.dragonClass}\n" +
                   $"Roles: {DragonRoleUtility.BuildRoleText(dragon)}  Range: {dragon.Definition.baseStats.range:0.#}\n" +
                   $"Level {dragon.Level}  Skill Lv {dragon.SkillLevel}  Evolution {dragon.EvolutionStage}\n" +
                   $"Bond {dragon.BondLevel}  XP {dragon.BondXp:0}/{dragon.BondXpThreshold:0}  Battles {dragon.TotalBattles}\n\n" +
                   $"Active Skill: {activeSkill}\n" +
                   $"Attack: {baseAttack:0} -> {boostedAttack:0}\n" +
                   $"Attack Speed: {baseSpeed:0.##} -> {boostedSpeed:0.##}\n" +
                   $"HP: {dragon.Definition.baseStats.hp:0}  Armor: {dragon.Definition.baseStats.armor:0}\n\n" +
                   $"{compare}\n\n" +
                   $"Account Buffs\n" +
                   $"Damage +{(progression.DamageMultiplier - 1f) * 100f:0.#}%\n" +
                   $"Speed +{(progression.AttackSpeedMultiplier - 1f) * 100f:0.#}%\n" +
                   $"Starting Mana +{progression.StartingManaBonus} MP";
        }

        private static string BuildCompareText(DragonInstance dragon, PlayerInventory inventory)
        {
            if (dragon?.Definition == null || inventory == null)
                return "Compare: unavailable";

            var equipped = inventory.GetBattleDragons();
            if (equipped.Count == 0)
                return "Compare: no equipped dragons";

            float attack = 0f;
            float speed = 0f;
            float range = 0f;
            int count = 0;
            foreach (DragonInstance equippedDragon in equipped)
            {
                if (equippedDragon?.Definition == null || equippedDragon == dragon) continue;
                attack += equippedDragon.Attack;
                speed += equippedDragon.AttackSpeed;
                range += equippedDragon.Range;
                count++;
            }

            if (count == 0)
                return "Compare: selected dragon is the only equipped dragon";

            attack /= count;
            speed /= count;
            range /= count;
            return $"Compare vs equipped avg: ATK {dragon.Attack - attack:+0;-0;0}, SPD {dragon.AttackSpeed - speed:+0.##;-0.##;0}, RNG {dragon.Range - range:+0.#;-0.#;0}";
        }

        private void UpdatePortrait(DragonInstance dragon)
        {
            if (_dragonPortrait == null) return;
            Sprite portrait = null;
            if (dragon?.Definition?.visualData != null)
                portrait = dragon.Definition.visualData.portrait;
            if (portrait != null)
            {
                _dragonPortrait.sprite = portrait;
                _dragonPortrait.color = Color.white;
            }
            else
            {
                _dragonPortrait.sprite = null;
                _dragonPortrait.color = GetRarityColor(dragon);
            }

            if (_dragonArtCaption != null)
            {
                _dragonArtCaption.text = dragon?.Definition == null
                    ? "No dragon selected"
                    : $"{dragon.Definition.displayName}\n{dragon.Definition.rarity} {dragon.Definition.element}";
            }
        }

        private static Color GetRarityColor(DragonInstance dragon)
        {
            if (dragon?.Definition == null)
                return new Color(0.22f, 0.32f, 0.42f, 0.9f);

            return dragon.Definition.rarity switch
            {
                DragonRarity.Common => new Color(0.36f, 0.48f, 0.56f, 0.95f),
                DragonRarity.Uncommon => new Color(0.20f, 0.55f, 0.32f, 0.95f),
                DragonRarity.Rare => new Color(0.12f, 0.40f, 0.82f, 0.95f),
                DragonRarity.Epic => new Color(0.46f, 0.22f, 0.72f, 0.95f),
                DragonRarity.Legendary => new Color(0.86f, 0.52f, 0.12f, 0.95f),
                DragonRarity.Mythic => new Color(0.82f, 0.18f, 0.36f, 0.95f),
                DragonRarity.Ancient => new Color(0.9f, 0.82f, 0.48f, 0.95f),
                _ => new Color(0.22f, 0.32f, 0.42f, 0.9f)
            };
        }

        private void UpdateEquipButton(DragonInstance dragon, PlayerInventory inventory)
        {
            if (_equipButton == null) return;
            bool hasDragon = dragon?.Definition != null && inventory != null;
            _equipButton.gameObject.SetActive(hasDragon);
            if (!hasDragon) return;

            bool equipped = inventory.IsEquipped(dragon);
            bool full = !equipped && inventory.EquippedDragonIds.Count >= PlayerInventory.MaxEquippedDragons;
            _equipButton.interactable = !full || equipped;

            Text label = _equipButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = equipped
                    ? "Unequip From Battle"
                    : full
                        ? "Loadout Full"
                        : $"Equip To Battle ({inventory.EquippedDragonIds.Count}/{PlayerInventory.MaxEquippedDragons})";
        }

        private void UpdateDragonActionButtons(DragonInstance dragon, PlayerInventory inventory)
        {
            bool hasDragon = dragon?.Definition != null && inventory?.Progression != null;
            SetDragonActionButton(_levelUpButton, hasDragon,
                hasDragon ? $"Level Up ({PlayerInventory.GetDragonLevelUpGoldCost(dragon)}g)" : "Level Up",
                hasDragon && dragon.Level < 20 && inventory.Progression.Gold >= PlayerInventory.GetDragonLevelUpGoldCost(dragon));

            int maxBond = hasDragon ? dragon.Definition.bondData?.bondLevels?.Length ?? 7 : 7;
            SetDragonActionButton(_trainBondButton, hasDragon,
                hasDragon ? $"Train Bond ({PlayerInventory.GetDragonBondTrainingEssenceCost(dragon)}e)" : "Train Bond",
                hasDragon && dragon.BondLevel < maxBond && inventory.Progression.Essence >= PlayerInventory.GetDragonBondTrainingEssenceCost(dragon));

            int stage = hasDragon ? (int)dragon.EvolutionStage : 0;
            int maxStage = System.Enum.GetValues(typeof(DragonEvolutionStage)).Length - 1;
            SetDragonActionButton(_evolveButton, hasDragon,
                hasDragon ? $"Evolve ({PlayerInventory.GetDragonEvolutionEssenceCost(dragon)}e)" : "Evolve",
                hasDragon && stage < maxStage && dragon.BondLevel >= 5 && inventory.Progression.Essence >= PlayerInventory.GetDragonEvolutionEssenceCost(dragon));
        }

        private static void SetDragonActionButton(Button button, bool visible, string label, bool interactable)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible);
            button.interactable = interactable;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
                text.text = label;
        }

        private void Update()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (_summonButton == null || inventory == null) return;
            _summonButton.interactable = inventory.Progression.SummonTickets > 0;
            Text label = _summonButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = inventory.Progression.SummonTickets > 0 ? "Summon Dragon" : "Need Ticket";
        }

        private static void SetUpgradeButton(Button button, AccountBuffType buffType, PlayerProgression progression)
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
                    : $"{PlayerProgression.Label(buffType)} Lv {level + 1} ({cost}e)";
        }
    }
}

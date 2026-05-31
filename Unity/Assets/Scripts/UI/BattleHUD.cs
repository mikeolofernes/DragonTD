using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using DragonTD.Core;
using DragonTD.TowerDefense;

namespace DragonTD.UI
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private Text _livesText;
        [SerializeField] private Text _waveText;
        [SerializeField] private Text _manaText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _wavePreviewText;
        [SerializeField] private Text _waveSummaryText;
        [SerializeField] private Text _selectedTowerText;
        [SerializeField] private Text _skillCooldownText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _nextWaveButton;
        [SerializeField] private Button _skillButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _mergeButton;
        [SerializeField] private Button _sellButton;

        private bool _subscribedToGameState;
        private bool _subscribedToResources;
        private bool _subscribedToSelection;
        private bool _subscribedToStats;
        private bool _subscribedToWall;
        private DragonTower _selectedTower;
        private Text _wallHpText;

        private void Awake()
        {
            EnsureActionControls();
            EnsureWavePreviewPanel();
            EnsureWaveSummaryPanel();
            RuntimeFontScaler.Apply(gameObject);
        }

        private void Start()
        {
            EnsureActionControls();
            EnsureWavePreviewPanel();
            EnsureWaveSummaryPanel();
            RuntimeFontScaler.Apply(gameObject);
            TrySubscribe();
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
            if (_nextWaveButton != null)
                _nextWaveButton.onClick.AddListener(() => GameManager.Instance.StartNextWave());
            if (_skillButton != null)
                _skillButton.onClick.AddListener(OnSkillButtonClicked);
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
            if (_mergeButton != null)
                _mergeButton.onClick.AddListener(OnMergeButtonClicked);
            if (_sellButton != null)
                _sellButton.onClick.AddListener(OnSellButtonClicked);
        }

        private void OnEnable()
        {
            TrySubscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && _subscribedToGameState)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnLivesChanged -= UpdateLivesFromEvent;
                GameManager.Instance.OnBattleMessage -= ShowBattleMessage;
            }
            if (ResourceManager.Instance != null && _subscribedToResources)
            {
                ResourceManager.Instance.OnManaChanged -= UpdateMana;
                ResourceManager.Instance.OnGoldChanged -= UpdateGold;
            }
            if (TowerSelectionManager.Instance != null && _subscribedToSelection)
            {
                TowerSelectionManager.Instance.OnSelectedTowerChanged -= HandleSelectedTowerChanged;
                TowerSelectionManager.Instance.OnTargetingChanged -= HandleTargetingChanged;
            }
            if (BattleStatsTracker.Instance != null && _subscribedToStats)
            {
                BattleStatsTracker.Instance.OnWaveSummary -= HandleWaveSummary;
            }
            if (WallBase.Instance != null && _subscribedToWall)
            {
                WallBase.Instance.OnHpChanged -= UpdateWallHp;
            }
            _subscribedToGameState = false;
            _subscribedToResources = false;
            _subscribedToSelection = false;
            _subscribedToStats = false;
            _subscribedToWall = false;
        }

        private void Update()
        {
            TrySubscribe();
            UpdateSelectedTowerPanel();
            UpdateWavePreview();
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateNextWaveButton(state);
            if (_waveText != null) _waveText.text = $"Wave: {GameManager.Instance.CurrentWave}";
            UpdateStatus(state);
            UpdateWavePreview();
            RefreshAll();
        }

        private void TrySubscribe()
        {
            if (!_subscribedToGameState && GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                GameManager.Instance.OnLivesChanged += UpdateLivesFromEvent;
                GameManager.Instance.OnBattleMessage += ShowBattleMessage;
                _subscribedToGameState = true;
            }

            if (!_subscribedToResources && ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnManaChanged += UpdateMana;
                ResourceManager.Instance.OnGoldChanged += UpdateGold;
                _subscribedToResources = true;
            }

            if (!_subscribedToSelection && TowerSelectionManager.Instance != null)
            {
                TowerSelectionManager.Instance.OnSelectedTowerChanged += HandleSelectedTowerChanged;
                TowerSelectionManager.Instance.OnTargetingChanged += HandleTargetingChanged;
                _selectedTower = TowerSelectionManager.Instance.SelectedTower;
                _subscribedToSelection = true;
            }

            if (!_subscribedToStats && BattleStatsTracker.Instance != null)
            {
                BattleStatsTracker.Instance.OnWaveSummary += HandleWaveSummary;
                _subscribedToStats = true;
                HandleWaveSummary(BattleStatsTracker.Instance.LatestSummary);
            }

            if (!_subscribedToWall && WallBase.Instance != null)
            {
                WallBase.Instance.OnHpChanged += UpdateWallHp;
                _subscribedToWall = true;
                UpdateWallHp(WallBase.Instance.HpPercent);
            }
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
            bool laneMode = WallBase.Instance != null;
            if (_livesText != null) _livesText.gameObject.SetActive(!laneMode);
            if (_wallHpText != null) _wallHpText.gameObject.SetActive(laneMode);
            if (!laneMode && _livesText != null)
                _livesText.text = $"Lives: {GameManager.Instance?.Lives ?? 0}";
        }

        private void UpdateLivesFromEvent(int _) => UpdateLives();

        private void UpdateWallHp(float hpPercent)
        {
            EnsureWallHpText();
            if (_wallHpText == null || WallBase.Instance == null) return;
            int cur = Mathf.CeilToInt(WallBase.Instance.CurrentHp);
            int max = Mathf.CeilToInt(WallBase.Instance.MaxHp);
            _wallHpText.text = $"Wall: {cur}/{max}";
            _wallHpText.gameObject.SetActive(true);
            if (_livesText != null) _livesText.gameObject.SetActive(false);
        }

        private void EnsureWallHpText()
        {
            if (_wallHpText != null) return;
            if (_livesText == null) return;
            var go = new GameObject("WallHpText");
            go.transform.SetParent(_livesText.transform.parent, false);
            var rt = go.AddComponent<RectTransform>();
            var src = _livesText.GetComponent<RectTransform>();
            rt.anchorMin        = src.anchorMin;
            rt.anchorMax        = src.anchorMax;
            rt.anchoredPosition = src.anchoredPosition;
            rt.sizeDelta        = src.sizeDelta;
            _wallHpText             = go.AddComponent<Text>();
            _wallHpText.font        = _livesText.font;
            _wallHpText.fontSize    = _livesText.fontSize;
            _wallHpText.color       = new Color(1f, 0.55f, 0.1f, 1f);
            _wallHpText.alignment   = _livesText.alignment;
        }

        private void RefreshAll()
        {
            UpdateLives();
            if (_waveText != null) _waveText.text = $"Wave: {GameManager.Instance?.CurrentWave ?? 0}";
            if (GameManager.Instance != null)
            {
                UpdateNextWaveButton(GameManager.Instance.State);
                UpdateStatus(GameManager.Instance.State);
            }
            if (ResourceManager.Instance != null)
            {
                UpdateMana(ResourceManager.Instance.Mana);
                UpdateGold(ResourceManager.Instance.Gold);
            }
            UpdateSelectedTowerPanel();
            UpdateWavePreview();
            UpdateWaveSummaryVisibility();
        }

        private void UpdateNextWaveButton(GameState state)
        {
            bool showNextWave = GameManager.Instance != null && GameManager.Instance.IsPlanningPhase;
            if (_nextWaveButton != null)
            {
                _nextWaveButton.gameObject.SetActive(showNextWave);
                Text label = _nextWaveButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = GameManager.Instance != null && GameManager.Instance.CurrentWave == 0
                        ? "Start Wave"
                        : "Start Next";
            }
            if (_wavePreviewText != null)
                _wavePreviewText.transform.parent.gameObject.SetActive(showNextWave);
            UpdateWaveSummaryVisibility();
        }

        private void UpdateStatus(GameState state)
        {
            if (_statusText == null) return;

            _statusText.text = state switch
            {
                GameState.Planning => GameManager.Instance != null && GameManager.Instance.CurrentWave == 0
                    ? "Planning - build defenses, then start wave 1"
                    : "Planning - upgrade, fuse, build, then start next wave",
                GameState.Setup => "Planning - build defenses, then start the wave",
                GameState.Wave => "Wave in progress",
                GameState.BetweenWaves => "Wave cleared - place more dragons",
                GameState.Victory => "Prototype complete - all configured waves cleared",
                GameState.Defeat => "Defeat",
                GameState.Paused => "Paused",
                _ => ""
            };
        }

        private void ShowBattleMessage(string message)
        {
            if (_statusText != null)
                _statusText.text = FirstLine(message);

            if (_waveSummaryText != null && !string.IsNullOrEmpty(message) && message.Contains("\n"))
            {
                _waveSummaryText.text = message;
                _waveSummaryText.transform.parent.gameObject.SetActive(true);
            }
            else if (_waveSummaryText != null && GameManager.Instance != null && GameManager.Instance.State == GameState.Wave)
            {
                _waveSummaryText.transform.parent.gameObject.SetActive(false);
            }
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int lineBreak = text.IndexOf('\n');
            return lineBreak >= 0 ? text.Substring(0, lineBreak) : text;
        }

        private void HandleWaveSummary(string summary)
        {
            if (_waveSummaryText == null) return;

            bool hasSummary = !string.IsNullOrWhiteSpace(summary);
            _waveSummaryText.text = hasSummary ? summary : "Wave summary appears here";
            UpdateWaveSummaryVisibility();
        }

        private void UpdateWaveSummaryVisibility()
        {
            if (_waveSummaryText == null || GameManager.Instance == null) return;

            bool show = !string.IsNullOrWhiteSpace(BattleStatsTracker.Instance?.LatestSummary) &&
                        GameManager.Instance.State != GameState.Wave &&
                        (GameManager.Instance.State == GameState.Victory ||
                         GameManager.Instance.State == GameState.Defeat ||
                         (GameManager.Instance.IsPlanningPhase && GameManager.Instance.CurrentWave > 0));
            _waveSummaryText.transform.parent.gameObject.SetActive(show);
        }

        private void UpdateWavePreview()
        {
            if (_wavePreviewText == null || GameManager.Instance == null || WaveManager.Instance == null) return;

            bool showPreview = GameManager.Instance.IsPlanningPhase;
            _wavePreviewText.transform.parent.gameObject.SetActive(showPreview);
            if (!showPreview) return;

            int nextWave = GameManager.Instance.CurrentWave + 1;
            WaveData wave = WaveManager.Instance.GetWaveData(nextWave);
            if (wave == null)
            {
                _wavePreviewText.text = "No upcoming wave";
                return;
            }

            _wavePreviewText.text = BuildWavePreviewText(nextWave, wave);
        }

        private string BuildWavePreviewText(int waveNumber, WaveData wave)
        {
            var counts = new Dictionary<EnemyTrait, int>();
            int total = 0;
            if (wave.EnemyGroups != null)
            {
                foreach (EnemySpawnEntry group in wave.EnemyGroups)
                {
                    if (group == null || group.EnemyPrefab == null || group.Count <= 0) continue;
                    EnemyBase enemy = group.EnemyPrefab.GetComponent<EnemyBase>();
                    EnemyTrait trait = enemy != null && enemy.Data != null ? enemy.Data.Trait : EnemyTrait.None;
                    counts.TryGetValue(trait, out int current);
                    counts[trait] = current + group.Count;
                    total += group.Count;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Wave {waveNumber} Preview");
            sb.AppendLine($"{total} enemies  |  +{wave.GoldReward}g  +{wave.ManaReward}MP");
            sb.AppendLine();
            foreach (var pair in counts)
                sb.AppendLine($"{TraitLabel(pair.Key)} x{pair.Value}");
            sb.AppendLine();
            string warning = WaveWarning(counts);
            if (!string.IsNullOrEmpty(warning))
                sb.AppendLine(warning);
            sb.Append(CounterHints(counts));
            return sb.ToString();
        }

        private static string TraitLabel(EnemyTrait trait) => trait switch
        {
            EnemyTrait.Runner => "RUN Runner",
            EnemyTrait.Brute => "ARMOR Brute",
            EnemyTrait.Shielded => "SHIELD Shielded",
            EnemyTrait.Regenerating => "REGEN Regenerating",
            EnemyTrait.Flying => "FLY Flying",
            _ => "Basic"
        };

        private static string WaveWarning(Dictionary<EnemyTrait, int> counts)
        {
            if (counts.ContainsKey(EnemyTrait.Regenerating) && counts.ContainsKey(EnemyTrait.Flying))
                return "Warning: regen and flying mixed pressure";
            if (counts.ContainsKey(EnemyTrait.Shielded))
                return "Warning: shields incoming";
            if (counts.ContainsKey(EnemyTrait.Runner))
                return "Warning: runners incoming";
            return string.Empty;
        }

        private static string CounterHints(Dictionary<EnemyTrait, int> counts)
        {
            var hints = new List<string>();
            if (counts.ContainsKey(EnemyTrait.Runner)) hints.Add("Runners: frost/slow");
            if (counts.ContainsKey(EnemyTrait.Shielded)) hints.Add("Shields: lightning or skills");
            if (counts.ContainsKey(EnemyTrait.Regenerating)) hints.Add("Regen: fire/burn");
            if (counts.ContainsKey(EnemyTrait.Flying)) hints.Add("Flying: high ground/range");
            if (counts.ContainsKey(EnemyTrait.Brute)) hints.Add("Brutes: armor breakers/skills");
            return hints.Count == 0 ? "Counters: balanced damage" : "Counters:\n" + string.Join("\n", hints);
        }

        private void HandleSelectedTowerChanged(DragonTower tower)
        {
            _selectedTower = tower;
            UpdateSelectedTowerPanel();
        }

        private void HandleTargetingChanged(bool isTargeting)
        {
            if (_skillButton != null)
                _skillButton.GetComponentInChildren<Text>().text = isTargeting ? "Cancel" : SkillButtonLabel();
            UpdateSelectedTowerPanel();
        }

        private void OnSkillButtonClicked()
        {
            TowerSelectionManager manager = TowerSelectionManager.Ensure();
            if (manager.IsTargetingSkill)
                manager.CancelSkillTargeting();
            else
                manager.BeginSkillTargeting();
        }

        private void OnUpgradeButtonClicked()
        {
            TowerSelectionManager.Ensure().UpgradeSelectedTower();
            UpdateSelectedTowerPanel();
        }

        private void OnMergeButtonClicked()
        {
            TowerSelectionManager manager = TowerSelectionManager.Ensure();
            if (manager.IsTargetingMerge)
                manager.CancelSkillTargeting();
            else
                manager.BeginMergeTargeting();
            UpdateSelectedTowerPanel();
        }

        private void OnSellButtonClicked()
        {
            TowerSelectionManager.Ensure().SellSelectedTower();
            UpdateSelectedTowerPanel();
        }

        private void UpdateSelectedTowerPanel()
        {
            bool hasTower = _selectedTower != null;

            if (_selectedTowerText != null)
            {
                _selectedTowerText.text = hasTower
                    ? $"{_selectedTower.DisplayName} {_selectedTower.TierLabel}"
                    : "No tower selected";
            }

            if (_skillCooldownText != null)
            {
                if (!hasTower || _selectedTower.ActiveSkill == null)
                {
                    _skillCooldownText.text = "Skill: -";
                }
                else
                {
                    float remaining = _selectedTower.ActiveSkillCooldownRemaining;
                    string state = remaining <= 0f ? "Ready" : $"{Mathf.CeilToInt(remaining)}s";
                    _skillCooldownText.text = $"{_selectedTower.ActiveSkill.displayName}: {state}";
                }
            }

            if (_skillButton != null)
            {
                bool skillAllowed = GameManager.Instance != null && GameManager.Instance.State == GameState.Wave;
                _skillButton.interactable = hasTower && _selectedTower.ActiveSkill != null && skillAllowed;
                if (!TowerSelectionManager.Ensure().IsTargetingSkill)
                    _skillButton.GetComponentInChildren<Text>().text = SkillButtonLabel();
            }

            if (_upgradeButton != null)
            {
                bool planning = GameManager.Instance != null && GameManager.Instance.IsPlanningPhase;
                _upgradeButton.interactable = hasTower && !_selectedTower.IsMaxUpgrade && planning;
                Text label = _upgradeButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = hasTower && !_selectedTower.IsMaxUpgrade
                        ? $"Upgrade { _selectedTower.UpgradeCost }g"
                        : "Max Lv";
            }

            if (_mergeButton != null)
            {
                bool planning = GameManager.Instance != null && GameManager.Instance.IsPlanningPhase;
                bool canTryMerge = hasTower && !_selectedTower.IsFused;
                bool hasMergeCandidate = canTryMerge && TowerSelectionManager.Ensure().HasMergeCandidate(_selectedTower);
                _mergeButton.interactable = canTryMerge && planning;
                Text label = _mergeButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (TowerSelectionManager.Ensure().IsTargetingMerge)
                        label.text = "Cancel";
                    else if (hasTower && !_selectedTower.IsReadyToFuse && !_selectedTower.IsFused)
                        label.text = "Need Lv3";
                    else
                        label.text = hasMergeCandidate ? "Fuse" : "No Match";
                }
            }

            if (_sellButton != null)
                _sellButton.interactable = hasTower && GameManager.Instance != null && GameManager.Instance.IsPlanningPhase;
        }

        private void EnsureActionControls()
        {
            transform.SetAsLastSibling();

            if (_selectedTowerText != null && _skillCooldownText != null &&
                _skillButton != null && _upgradeButton != null && _mergeButton != null && _sellButton != null)
            {
                RepositionActionControls();
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite sprite = RuntimeWhiteSprite();

            if (_selectedTowerText != null && _skillCooldownText != null &&
                _skillButton != null && _upgradeButton != null && _sellButton != null)
            {
                Transform existingParent = _sellButton.transform.parent != null ? _sellButton.transform.parent : parent;
                if (_mergeButton == null)
                    _mergeButton = CreatePanelButton(existingParent, "MergeButton", "Fuse", font, sprite, new Vector2(-72f, 168f), new Vector2(124f, 34f));
                RepositionActionControls();
                return;
            }

            var panel = new GameObject("TowerActionPanel");
            panel.transform.SetParent(parent, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(0f, 154f);
            panelRt.sizeDelta = new Vector2(420f, 96f);

            var bg = panel.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color = new Color(0f, 0f, 0f, 0.62f);

            if (_selectedTowerText == null)
                _selectedTowerText = CreatePanelText(panel.transform, "SelectedTowerText", font, "No tower selected", 18, new Vector2(0f, -12f), new Vector2(400f, 26f));
            if (_skillCooldownText == null)
                _skillCooldownText = CreatePanelText(panel.transform, "SkillCooldownText", font, "Skill: -", 15, new Vector2(0f, -38f), new Vector2(400f, 22f));
            if (_skillButton == null)
                _skillButton = CreatePanelButton(panel.transform, "SkillButton", "Skill", font, sprite, new Vector2(-72f, -72f), new Vector2(124f, 34f));
            if (_upgradeButton == null)
                _upgradeButton = CreatePanelButton(panel.transform, "UpgradeButton", "Upgrade", font, sprite, new Vector2(72f, -72f), new Vector2(138f, 34f));
            if (_mergeButton == null)
                _mergeButton = CreatePanelButton(panel.transform, "MergeButton", "Fuse", font, sprite, new Vector2(-72f, -112f), new Vector2(124f, 32f));
            if (_sellButton == null)
                _sellButton = CreatePanelButton(panel.transform, "SellButton", "Sell", font, sprite, new Vector2(72f, -112f), new Vector2(112f, 32f));

            RepositionActionControls();
        }

        private void EnsureWavePreviewPanel()
        {
            if (_wavePreviewText != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite sprite = RuntimeWhiteSprite();

            var panel = new GameObject("WavePreviewPanel");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-12f, 38f);
            rt.sizeDelta = new Vector2(300f, 270f);

            var bg = panel.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color = new Color(0f, 0f, 0f, 0.68f);

            _wavePreviewText = CreatePanelText(panel.transform, "WavePreviewText", font, "Wave Preview", 15, new Vector2(0f, -12f), new Vector2(278f, 246f));
            RectTransform textRt = _wavePreviewText.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 1f);
            textRt.anchorMax = new Vector2(0.5f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            _wavePreviewText.alignment = TextAnchor.UpperLeft;
            _wavePreviewText.color = new Color(0.92f, 0.96f, 1f, 1f);
        }

        private void EnsureWaveSummaryPanel()
        {
            if (_waveSummaryText != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite sprite = RuntimeWhiteSprite();

            var panel = new GameObject("WaveSummaryPanel");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(12f, -40f);
            rt.sizeDelta = new Vector2(300f, 136f);

            var bg = panel.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color = new Color(0f, 0f, 0f, 0.66f);

            _waveSummaryText = CreatePanelText(panel.transform, "WaveSummaryText", font, "Wave summary appears here", 15, new Vector2(0f, -10f), new Vector2(278f, 112f));
            RectTransform textRt = _waveSummaryText.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 1f);
            textRt.anchorMax = new Vector2(0.5f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            _waveSummaryText.alignment = TextAnchor.UpperLeft;
            _waveSummaryText.color = new Color(0.9f, 1f, 0.86f, 1f);
            panel.SetActive(false);
        }

        private void RepositionActionControls()
        {
            PositionText(_selectedTowerText, new Vector2(0f, 274f), new Vector2(400f, 28f), 18);
            PositionText(_skillCooldownText, new Vector2(0f, 246f), new Vector2(400f, 24f), 15);
            PositionButton(_skillButton, new Vector2(-72f, 208f), new Vector2(124f, 38f));
            PositionButton(_upgradeButton, new Vector2(72f, 208f), new Vector2(138f, 38f));
            PositionButton(_mergeButton, new Vector2(-72f, 168f), new Vector2(124f, 34f));
            PositionButton(_sellButton, new Vector2(72f, 168f), new Vector2(112f, 34f));
        }

        private static void PositionText(Text text, Vector2 position, Vector2 dimensions, int fontSize)
        {
            if (text == null) return;
            RectTransform rt = text.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = Mathf.CeilToInt(fontSize * RuntimeFontScaler.DefaultScale) + 1;
        }

        private static void PositionButton(Button button, Vector2 position, Vector2 dimensions)
        {
            if (button == null) return;
            RectTransform rt = button.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
        }

        private static Text CreatePanelText(Transform parent, string name, Font font, string text, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;

            var label = go.AddComponent<Text>();
            label.font = font;
            label.fontSize = Mathf.CeilToInt(size * RuntimeFontScaler.DefaultScale) + 1;
            label.text = text;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            return label;
        }

        private static Button CreatePanelButton(Transform parent, string name, string text, Font font, Sprite sprite, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.16f, 0.24f, 0.45f, 0.96f);
            var button = go.AddComponent<Button>();

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            var label = labelGo.AddComponent<Text>();
            label.font = font;
            label.fontSize = 19;
            label.text = text;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private string SkillButtonLabel()
        {
            if (_selectedTower == null || _selectedTower.ActiveSkill == null)
                return "Skill";
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Wave)
                return "Wave Only";

            string label = _selectedTower.ActiveSkill.displayName;
            return label.Length > 12 ? label.Substring(0, 12) : label;
        }

        private static Sprite RuntimeWhiteSprite()
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class EventsPanel : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _detailText;
        [SerializeField] private Button[] _eventButtons;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _closeButton;

        private int _selectedIndex;
        private string _lastClaimMessage;

        private void Awake()
        {
            EnsureControls();
        }

        private void OnEnable()
        {
            WireButtons();
            Refresh();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        public void Refresh()
        {
            EnsureControls();
            PlayerProgression progression = PlayerInventory.Instance?.Progression;
            PrototypeEventDefinition[] events = PrototypeEventCatalog.Events;
            DailyObjectiveDefinition[] objectives = DailyObjectiveCatalog.Objectives;
            int totalItems = events.Length + objectives.Length;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, totalItems - 1));

            for (int i = 0; i < _eventButtons.Length && i < totalItems; i++)
            {
                bool isEvent = i < events.Length;
                Text label = _eventButtons[i]?.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (isEvent)
                    {
                        string status = progression != null ? progression.BuildEventStatus(events[i]) : "Unavailable";
                        label.text = $"{events[i].displayName}\n{events[i].RewardText}\n{status}";
                        label.color = events[i].locked || !events[i].claimable ? new Color(0.68f, 0.78f, 0.9f, 1f) : Color.white;
                    }
                    else
                    {
                        DailyObjectiveDefinition objective = objectives[i - events.Length];
                        string status = progression != null ? progression.BuildDailyObjectiveStatus(objective) : "Unavailable";
                        label.text = $"{objective.displayName}\n{objective.RewardText}\n{status}";
                        label.color = Color.white;
                    }
                }

                Image image = _eventButtons[i]?.GetComponent<Image>();
                if (image != null)
                {
                    if (isEvent)
                    {
                        if (events[i].locked)
                            image.color = new Color(0.18f, 0.22f, 0.28f, 0.9f);
                        else if (!events[i].claimable)
                            image.color = new Color(0.10f, 0.22f, 0.38f, 0.9f);
                        else if (progression != null && progression.IsEventClaimedToday(events[i].eventId))
                            image.color = new Color(0.16f, 0.34f, 0.24f, 0.9f);
                        else
                            image.color = new Color(0.08f, 0.36f, 0.7f, 0.92f);
                    }
                    else
                    {
                        DailyObjectiveDefinition objective = objectives[i - events.Length];
                        string status = progression != null ? progression.BuildDailyObjectiveStatus(objective) : "Unavailable";
                        image.color = status == "Claimed" ? new Color(0.16f, 0.34f, 0.24f, 0.9f) :
                            status == "Ready" ? new Color(0.95f, 0.62f, 0.16f, 0.95f) :
                            new Color(0.08f, 0.32f, 0.6f, 0.92f);
                    }
                }
            }

            if (_detailText != null)
            {
                _detailText.text = string.IsNullOrWhiteSpace(_lastClaimMessage) ? BuildSelectedDetail(progression, events, objectives) : _lastClaimMessage;
            }

            if (_claimButton != null)
            {
                bool selectedEvent = _selectedIndex < events.Length;
                bool canClaim = selectedEvent
                    ? progression != null && events[_selectedIndex].claimable && !progression.IsEventClaimedToday(events[_selectedIndex].eventId)
                    : progression != null && progression.BuildDailyObjectiveStatus(objectives[_selectedIndex - events.Length]) == "Ready";
                _claimButton.interactable = canClaim;
                Text label = _claimButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (selectedEvent)
                    {
                        PrototypeEventDefinition selected = events[_selectedIndex];
                        label.text = selected.locked ? "Locked" :
                            !selected.claimable ? "Preview Only" :
                            progression != null && progression.IsEventClaimedToday(selected.eventId) ? "Claimed" : "Claim";
                    }
                    else
                    {
                        string status = progression != null ? progression.BuildDailyObjectiveStatus(objectives[_selectedIndex - events.Length]) : "Unavailable";
                        label.text = status == "Claimed" ? "Claimed" : status == "Ready" ? "Claim Daily" : "In Progress";
                    }
                }
            }
        }

        private void WireButtons()
        {
            if (_eventButtons != null)
            {
                for (int i = 0; i < _eventButtons.Length; i++)
                {
                    int index = i;
                    if (_eventButtons[i] != null)
                        _eventButtons[i].onClick.AddListener(() => SelectEvent(index));
                }
            }

            if (_claimButton != null)
                _claimButton.onClick.AddListener(ClaimSelected);
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void UnwireButtons()
        {
            if (_eventButtons != null)
            {
                foreach (Button button in _eventButtons)
                {
                    if (button != null)
                        button.onClick.RemoveAllListeners();
                }
            }

            if (_claimButton != null)
                _claimButton.onClick.RemoveAllListeners();
            if (_closeButton != null)
                _closeButton.onClick.RemoveAllListeners();
        }

        private void SelectEvent(int index)
        {
            _selectedIndex = Mathf.Clamp(index, 0, PrototypeEventCatalog.Events.Length + DailyObjectiveCatalog.Objectives.Length - 1);
            _lastClaimMessage = null;
            Refresh();
        }

        private void ClaimSelected()
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;
            PrototypeEventDefinition[] events = PrototypeEventCatalog.Events;
            bool isEvent = _selectedIndex < events.Length;
            bool claimed;
            string title;
            string rewardText;
            string message;
            if (isEvent)
            {
                PrototypeEventDefinition selected = events[_selectedIndex];
                claimed = inventory.TryClaimEventReward(selected, out message);
                title = selected.displayName;
                rewardText = selected.RewardText;
            }
            else
            {
                DailyObjectiveDefinition objective = DailyObjectiveCatalog.Objectives[_selectedIndex - events.Length];
                claimed = inventory.TryClaimDailyObjective(objective.objectiveId, out message);
                title = objective.displayName;
                rewardText = objective.RewardText;
            }
            _lastClaimMessage = message;
            Refresh();
            if (claimed)
                GetComponentInParent<MainMenuController>()?.ShowRewardMessage(title, rewardText);
        }

        private string BuildSelectedDetail(PlayerProgression progression, PrototypeEventDefinition[] events, DailyObjectiveDefinition[] objectives)
        {
            if (_selectedIndex < events.Length)
            {
                PrototypeEventDefinition selected = events[_selectedIndex];
                string status = progression != null ? progression.BuildEventStatus(selected) : "Unavailable";
                return $"{selected.displayName}\n\n{selected.description}\n\nReward: {selected.RewardText}\nStatus: {status}";
            }

            DailyObjectiveDefinition objective = objectives[_selectedIndex - events.Length];
            string objectiveStatus = progression != null ? progression.BuildDailyObjectiveStatus(objective) : "Unavailable";
            return $"{objective.displayName}\n\n{objective.description}\n\nReward: {objective.RewardText}\nProgress: {objectiveStatus}";
        }

        private void EnsureControls()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite sprite = CreateRuntimeSprite();

            Image image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.02f, 0.08f, 0.13f, 0.96f);

            RectTransform rootRt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            if (rootRt.sizeDelta == Vector2.zero)
            {
                rootRt.anchorMin = new Vector2(0.13f, 0.12f);
                rootRt.anchorMax = new Vector2(0.87f, 0.88f);
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
            }

            if (_titleText == null)
                _titleText = CreateText("EventsTitleText", font, new Vector2(0.5f, 0.9f), new Vector2(520f, 48f), "Events", 34);
            if (_detailText == null)
                _detailText = CreateText("EventsDetailText", font, new Vector2(0.62f, 0.52f), new Vector2(560f, 260f), "Select an event", 22);

            int itemCount = PrototypeEventCatalog.Events.Length + DailyObjectiveCatalog.Objectives.Length;
            if (_eventButtons == null || _eventButtons.Length < itemCount)
            {
                _eventButtons = new Button[itemCount];
                for (int i = 0; i < _eventButtons.Length; i++)
                {
                    string label = i < PrototypeEventCatalog.Events.Length
                        ? PrototypeEventCatalog.Events[i].displayName
                        : DailyObjectiveCatalog.Objectives[i - PrototypeEventCatalog.Events.Length].displayName;
                    int column = i / 3;
                    int row = i % 3;
                    _eventButtons[i] = CreateButton($"EventButton{i + 1}", label, font, sprite,
                        new Vector2(0.20f + column * 0.21f, 0.68f - row * 0.18f), new Vector2(230f, 106f));
                }
            }

            if (_claimButton == null)
                _claimButton = CreateButton("ClaimEventButton", "Claim", font, sprite, new Vector2(0.62f, 0.24f), new Vector2(220f, 54f));
            if (_closeButton == null)
                _closeButton = CreateButton("EventsCloseButton", "Close", font, sprite, new Vector2(0.5f, 0.08f), new Vector2(180f, 44f));
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
            image.color = new Color(0.08f, 0.36f, 0.7f, 0.92f);
            Button button = go.AddComponent<Button>();

            Text text = CreateText("Text", font, new Vector2(0.5f, 0.5f), Vector2.zero, label, 19);
            text.transform.SetParent(go.transform, false);
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 0f);
            textRt.offsetMax = new Vector2(-8f, 0f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 13;
            text.resizeTextMaxSize = 20;
            return button;
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

using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class StorePanel : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _currencyText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button[] _packButtons;
        [SerializeField] private Button _closeButton;

        private bool _purchaseInProgress;
        private int _pendingPackIndex = -1;

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
            if (_currencyText != null)
                _currencyText.text = progression == null ? "Gems: 0" : $"Gems: {progression.Gems}";
            if (_messageText != null && string.IsNullOrWhiteSpace(_messageText.text))
                _messageText.text = "Purchases validate receipts before Gems are granted.";
        }

        private void WireButtons()
        {
            if (_packButtons != null)
            {
                for (int i = 0; i < _packButtons.Length && i < GemStoreCatalog.Packs.Length; i++)
                {
                    Text label = _packButtons[i]?.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.text = _purchaseInProgress && i == _pendingPackIndex
                            ? "Validating..."
                            : PackLabel(GemStoreCatalog.Packs[i]);
                    }

                    int index = i;
                    if (_packButtons[i] != null)
                        _packButtons[i].onClick.AddListener(() => Purchase(index));
                }
            }

            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void UnwireButtons()
        {
            if (_packButtons != null)
            {
                foreach (Button button in _packButtons)
                {
                    if (button != null)
                        button.onClick.RemoveAllListeners();
                }
            }

            if (_closeButton != null)
                _closeButton.onClick.RemoveAllListeners();
        }

        private async void Purchase(int index)
        {
            if (_purchaseInProgress || index < 0 || index >= GemStoreCatalog.Packs.Length) return;
            GemPackDefinition pack = GemStoreCatalog.Packs[index];
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null)
            {
                if (_messageText != null)
                    _messageText.text = "Inventory is not ready";
                return;
            }

            _purchaseInProgress = true;
            _pendingPackIndex = index;
            SetPackButtonsInteractable(false);
            Refresh();
            if (_messageText != null)
                _messageText.text = $"Purchase pending\nValidating receipt for {pack.displayName}";

            StorePurchaseResult result = await inventory.PurchaseGemPackAsync(pack.productId);
            if (_messageText != null)
                _messageText.text = result != null && result.success && result.validated
                    ? $"Purchase validated\n+{result.gemsGranted} Gems"
                    : result?.message ?? "Purchase failed";

            _purchaseInProgress = false;
            _pendingPackIndex = -1;
            SetPackButtonsInteractable(true);
            Refresh();
        }

        private void EnsureControls()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite sprite = CreateRuntimeSprite();

            Image panelImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            panelImage.sprite = sprite;
            panelImage.color = new Color(0.02f, 0.06f, 0.12f, 0.94f);

            RectTransform rootRt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            if (rootRt.sizeDelta == Vector2.zero)
            {
                rootRt.anchorMin = new Vector2(0.16f, 0.14f);
                rootRt.anchorMax = new Vector2(0.84f, 0.88f);
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
            }

            if (_titleText == null)
                _titleText = CreateText("StoreTitleText", font, new Vector2(0.5f, 0.9f), new Vector2(520f, 48f), "Gem Store", 34, TextAnchor.MiddleCenter);
            if (_currencyText == null)
                _currencyText = CreateText("StoreCurrencyText", font, new Vector2(0.5f, 0.82f), new Vector2(520f, 36f), "Gems: 0", 22, TextAnchor.MiddleCenter);
            if (_messageText == null)
                _messageText = CreateText("StoreMessageText", font, new Vector2(0.5f, 0.18f), new Vector2(720f, 70f), "", 18, TextAnchor.MiddleCenter);

            if (_packButtons == null || _packButtons.Length < GemStoreCatalog.Packs.Length)
            {
                _packButtons = new Button[GemStoreCatalog.Packs.Length];
                for (int i = 0; i < GemStoreCatalog.Packs.Length; i++)
                {
                    float y = 0.7f - i * 0.11f;
                    _packButtons[i] = CreateButton($"GemPackButton{i + 1}", PackLabel(GemStoreCatalog.Packs[i]), font, sprite,
                        new Vector2(0.5f, y), new Vector2(620f, 54f));
                }
            }

            if (_closeButton == null)
                _closeButton = CreateButton("StoreCloseButton", "Close", font, sprite, new Vector2(0.5f, 0.08f), new Vector2(180f, 42f));
        }

        private void SetPackButtonsInteractable(bool interactable)
        {
            if (_packButtons == null) return;
            foreach (Button button in _packButtons)
            {
                if (button != null)
                    button.interactable = interactable;
            }
        }

        private static string PackLabel(GemPackDefinition pack) =>
            $"{pack.displayName}   +{pack.gems} Gems   {pack.price}";

        private Text CreateText(string name, Font font, Vector2 anchor, Vector2 size, string value, int fontSize, TextAnchor alignment)
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
            text.alignment = alignment;
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
            image.color = new Color(0.12f, 0.25f, 0.52f, 0.96f);
            Button button = go.AddComponent<Button>();

            Text text = CreateText("Text", font, new Vector2(0.5f, 0.5f), Vector2.zero, label, 19, TextAnchor.MiddleCenter);
            text.transform.SetParent(go.transform, false);
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 0f);
            textRt.offsetMax = new Vector2(-10f, 0f);
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

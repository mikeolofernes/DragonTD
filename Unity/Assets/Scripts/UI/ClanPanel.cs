using UnityEngine;
using UnityEngine.UI;

namespace DragonTD.UI
{
    public class ClanPanel : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _closeButton;

        private void Awake()
        {
            EnsureControls();
        }

        private void OnEnable()
        {
            EnsureControls();
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveAllListeners();
        }

        private void EnsureControls()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            Sprite sprite = CreateRuntimeSprite();

            Image image = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.04f, 0.07f, 0.12f, 0.96f);

            RectTransform rootRt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            if (rootRt.sizeDelta == Vector2.zero)
            {
                rootRt.anchorMin = new Vector2(0.2f, 0.2f);
                rootRt.anchorMax = new Vector2(0.8f, 0.82f);
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
            }

            if (_titleText == null)
                _titleText = CreateText("ClanTitleText", font, new Vector2(0.5f, 0.84f), new Vector2(520f, 56f), "Clan", 34);
            if (_bodyText == null)
                _bodyText = CreateText("ClanBodyText", font, new Vector2(0.5f, 0.52f), new Vector2(720f, 250f),
                    "Clan features are locked for this prototype.\n\nPlanned shell:\nCreate or join clan\nMember list\nClan Raid event\nShared rewards\n\nRequires authenticated account persistence and social backend.", 22);
            if (_closeButton == null)
                _closeButton = CreateButton("ClanCloseButton", "Close", font, sprite, new Vector2(0.5f, 0.14f), new Vector2(180f, 48f));
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
            Text text = CreateText("Text", font, new Vector2(0.5f, 0.5f), Vector2.zero, label, 20);
            text.transform.SetParent(go.transform, false);
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
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

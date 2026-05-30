using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class LeaderboardPanel : MonoBehaviour
    {
        private Text _titleText;
        private Text _bodyText;
        private Button _refreshButton;
        private Button _closeButton;

        private void Awake() => EnsureControls();

        private void OnEnable()
        {
            EnsureControls();
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            if (_refreshButton != null) _refreshButton.onClick.AddListener(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
            if (_refreshButton != null) _refreshButton.onClick.RemoveAllListeners();
        }

        private void Close() => gameObject.SetActive(false);

        private async void Refresh()
        {
            PlayerInventory inv = PlayerInventory.Instance;
            if (inv == null) { SetBody("Inventory not ready."); return; }
            if (!inv.IsNakamaConnected) { SetBody("Sign in to Nakama to view scores."); return; }
            SetBody("Loading...");
            List<(string name, long score)> rows = await inv.GetNakamaLeaderboardAsync(10);
            if (rows == null || rows.Count == 0) { SetBody("No scores yet. Win a battle to submit one."); return; }
            var sb = new System.Text.StringBuilder();
            int rank = 1;
            foreach (var r in rows)
                sb.AppendLine($"{rank++}.  {r.name}    {r.score}");
            SetBody(sb.ToString());
        }

        private void SetBody(string text) { if (_bodyText != null) _bodyText.text = text; }

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
                rootRt.anchorMin = new Vector2(0.2f, 0.18f);
                rootRt.anchorMax = new Vector2(0.8f, 0.84f);
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
            }

            if (_titleText == null)
                _titleText = CreateText("LbTitle", font, new Vector2(0.5f, 0.9f), new Vector2(520f, 56f), "Leaderboard", 34);
            if (_bodyText == null)
            {
                _bodyText = CreateText("LbBody", font, new Vector2(0.5f, 0.55f), new Vector2(720f, 360f), "Loading...", 22);
                _bodyText.alignment = TextAnchor.UpperLeft;
            }
            if (_refreshButton == null)
                _refreshButton = CreateButton("LbRefresh", "Refresh", font, sprite, new Vector2(0.38f, 0.12f), new Vector2(180f, 48f));
            if (_closeButton == null)
                _closeButton = CreateButton("LbClose", "Close", font, sprite, new Vector2(0.62f, 0.12f), new Vector2(180f, 48f));
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

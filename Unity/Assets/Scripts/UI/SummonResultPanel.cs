using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonTD.Core;
using DragonTD.Dragons;

namespace DragonTD.UI
{
    // Modal showing the 10 dragons from a 10-pull. Built at runtime under the given parent canvas.
    public class SummonResultPanel : MonoBehaviour
    {
        private GameObject _root;
        private Transform _cellContainer;
        private Font _font;

        public void Initialize(Transform parent, Font font)
        {
            _font = font;
            _root = new GameObject("SummonResultPanel");
            _root.transform.SetParent(parent, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.18f);
            rt.anchorMax = new Vector2(0.85f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.12f, 0.98f);

            var title = CreateText(_root.transform, "Title", "10-Pull Results",
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), 26, new Color(1f, 0.92f, 0.6f, 1f));
            title.alignment = TextAnchor.MiddleCenter;

            var containerGO = new GameObject("Cells");
            containerGO.transform.SetParent(_root.transform, false);
            var crt = containerGO.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.04f, 0.16f);
            crt.anchorMax = new Vector2(0.96f, 0.86f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            _cellContainer = containerGO.transform;

            var okButton = CreateButton(_root.transform, "OkButton", "OK",
                new Vector2(0.4f, 0.03f), new Vector2(0.6f, 0.13f));
            okButton.onClick.AddListener(() => _root.SetActive(false));

            _root.SetActive(false);
        }

        public void Show(List<DragonDefinition> results)
        {
            if (_root == null) return;
            foreach (Transform child in _cellContainer)
                Destroy(child.gameObject);

            int count = results != null ? results.Count : 0;
            for (int i = 0; i < 10; i++)
            {
                DragonDefinition def = i < count ? results[i] : null;
                CreateCell(i, def);
            }
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
        }

        private void CreateCell(int index, DragonDefinition def)
        {
            int col = index % 5;
            int row = index / 5;
            var cellGO = new GameObject($"Cell{index}");
            cellGO.transform.SetParent(_cellContainer, false);
            var rt = cellGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(col / 5f + 0.01f, 1f - (row + 1) / 2f + 0.02f);
            rt.anchorMax = new Vector2((col + 1) / 5f - 0.01f, 1f - row / 2f - 0.02f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = cellGO.AddComponent<Image>();
            img.color = def != null ? RarityColor(def.rarity) : new Color(0.1f, 0.1f, 0.14f, 1f);

            string label = def != null ? $"{def.displayName}\n{def.rarity}" : "—";
            var txt = CreateText(cellGO.transform, "Label", label,
                Vector2.zero, Vector2.one, 13, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
        }

        private static Color RarityColor(DragonRarity rarity) => rarity switch
        {
            DragonRarity.Common    => new Color(0.42f, 0.45f, 0.5f, 0.95f),
            DragonRarity.Uncommon  => new Color(0.2f, 0.6f, 0.32f, 0.95f),
            DragonRarity.Rare      => new Color(0.18f, 0.45f, 0.85f, 0.95f),
            DragonRarity.Epic      => new Color(0.55f, 0.28f, 0.78f, 0.95f),
            DragonRarity.Legendary => new Color(0.9f, 0.65f, 0.12f, 0.95f),
            DragonRarity.Mythic    => new Color(0.85f, 0.2f, 0.25f, 0.95f),
            _                      => new Color(0.3f, 0.3f, 0.34f, 0.95f)
        };

        private Text CreateText(Transform parent, string name, string content,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = content;
            return t;
        }

        private Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.55f, 0.3f, 1f);
            var btn = go.AddComponent<Button>();
            CreateText(go.transform, "Label", label, Vector2.zero, Vector2.one, 20, Color.white);
            return btn;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace DragonTD.UI
{
    public class ResponsiveBattleUILayout : MonoBehaviour
    {
        private Vector2Int _lastScreenSize;

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void Update()
        {
            if (_lastScreenSize.x == Screen.width && _lastScreenSize.y == Screen.height)
                return;

            ApplyLayout();
        }

        public void ApplyLayout()
        {
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            bool portrait = Screen.height > Screen.width;
            bool compact = portrait || Screen.width < 1000;
            ApplyBattleHudLayout(compact, portrait);
            ApplyDragonPanelLayout(compact, portrait);
            ApplyVictoryPanelLayout(compact, portrait);
            ApplyAccountBuffLayout(compact, portrait);
        }

        private void ApplyBattleHudLayout(bool compact, bool portrait)
        {
            Transform hud = FindDescendant(transform, "BattleHUD");
            if (hud == null) return;

            Position(FindDescendant(hud, "StatsPanel") as RectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -8f), compact ? new Vector2(172f, 142f) : new Vector2(190f, 160f));

            Position(FindDescendant(hud, "StatusText") as RectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                portrait ? new Vector2(0f, -56f) : new Vector2(0f, -18f),
                compact ? new Vector2(420f, 34f) : new Vector2(520f, 34f));

            PositionButton(FindDescendant(hud, "PauseButton") as RectTransform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-10f, -10f), compact ? new Vector2(104f, 48f) : new Vector2(110f, 44f));

            PositionButton(FindDescendant(hud, "NextWaveButton") as RectTransform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                compact ? new Vector2(-10f, -64f) : new Vector2(-130f, -10f),
                compact ? new Vector2(116f, 48f) : new Vector2(120f, 44f));

            Position(FindDescendant(hud, "WavePreviewPanel") as RectTransform,
                portrait ? new Vector2(1f, 1f) : new Vector2(1f, 0.5f),
                portrait ? new Vector2(1f, 1f) : new Vector2(1f, 0.5f),
                portrait ? new Vector2(1f, 1f) : new Vector2(1f, 0.5f),
                portrait ? new Vector2(-10f, -124f) : new Vector2(-12f, 38f),
                compact ? new Vector2(260f, 218f) : new Vector2(300f, 270f));

            Position(FindDescendant(hud, "WaveSummaryPanel") as RectTransform,
                portrait ? new Vector2(0f, 1f) : new Vector2(0f, 0.5f),
                portrait ? new Vector2(0f, 1f) : new Vector2(0f, 0.5f),
                portrait ? new Vector2(0f, 1f) : new Vector2(0f, 0.5f),
                portrait ? new Vector2(10f, -156f) : new Vector2(12f, -40f),
                compact ? new Vector2(260f, 128f) : new Vector2(300f, 136f));

            float actionY = portrait ? 224f : 208f;
            Position(FindDescendant(hud, "SelectedTowerText") as RectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, actionY + 66f), compact ? new Vector2(340f, 28f) : new Vector2(400f, 28f));
            Position(FindDescendant(hud, "SkillCooldownText") as RectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, actionY + 38f), compact ? new Vector2(340f, 24f) : new Vector2(400f, 24f));

            Vector2 buttonSize = compact ? new Vector2(132f, 46f) : new Vector2(124f, 38f);
            PositionButton(FindDescendant(hud, "SkillButton") as RectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-74f, actionY), buttonSize);
            PositionButton(FindDescendant(hud, "UpgradeButton") as RectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(74f, actionY), compact ? new Vector2(138f, 46f) : new Vector2(138f, 38f));
            PositionButton(FindDescendant(hud, "MergeButton") as RectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-74f, actionY - 50f), compact ? new Vector2(132f, 44f) : new Vector2(124f, 34f));
            PositionButton(FindDescendant(hud, "SellButton") as RectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(74f, actionY - 50f), compact ? new Vector2(116f, 44f) : new Vector2(112f, 34f));
        }

        private void ApplyDragonPanelLayout(bool compact, bool portrait)
        {
            Transform panel = FindDescendant(transform, "DragonCollectionPanel");
            if (panel == null) return;

            float panelHeight = portrait ? 178f : compact ? 158f : 145f;
            Position(panel as RectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, panelHeight));

            Transform container = FindDescendant(panel, "CardContainer");
            if (container is RectTransform containerRt)
            {
                containerRt.anchorMin = Vector2.zero;
                containerRt.anchorMax = Vector2.one;
                containerRt.offsetMin = new Vector2(10f, 9f);
                containerRt.offsetMax = new Vector2(-10f, -9f);
            }

            HorizontalLayoutGroup layout = container != null ? container.GetComponent<HorizontalLayoutGroup>() : null;
            if (layout != null)
            {
                layout.spacing = compact ? 8f : 6f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            if (container == null) return;
            Vector2 cardSize = portrait ? new Vector2(112f, 148f) : compact ? new Vector2(104f, 136f) : new Vector2(100f, 130f);
            foreach (Transform child in container)
            {
                if (child is RectTransform childRt)
                    childRt.sizeDelta = cardSize;
                LayoutElement element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = cardSize.x;
                element.preferredHeight = cardSize.y;
            }
        }

        private void ApplyVictoryPanelLayout(bool compact, bool portrait)
        {
            RectTransform panel = FindDescendant(transform, "VictoryDefeatPanel") as RectTransform;
            if (panel == null) return;

            panel.anchorMin = portrait ? new Vector2(0.06f, 0.18f) : new Vector2(0.2f, 0.18f);
            panel.anchorMax = portrait ? new Vector2(0.94f, 0.78f) : new Vector2(0.8f, 0.82f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            Text stats = FindDescendant(panel, "StatsText")?.GetComponent<Text>();
            if (stats != null)
                stats.fontSize = compact ? 19 : 21;
        }

        private void ApplyAccountBuffLayout(bool compact, bool portrait)
        {
            RectTransform panel = FindDescendant(transform, "AccountBuffPanel") as RectTransform;
            if (panel == null) return;

            Position(panel,
                portrait ? new Vector2(0f, 1f) : new Vector2(1f, 0.5f),
                portrait ? new Vector2(0f, 1f) : new Vector2(1f, 0.5f),
                portrait ? new Vector2(0f, 1f) : new Vector2(1f, 0.5f),
                portrait ? new Vector2(10f, -292f) : new Vector2(-12f, -236f),
                compact ? new Vector2(260f, 204f) : new Vector2(300f, 204f));

            foreach (string buttonName in new[] { "DamageBuffButton", "AttackSpeedBuffButton", "ManaBuffButton" })
            {
                RectTransform button = FindDescendant(panel, buttonName) as RectTransform;
                if (button != null)
                    button.sizeDelta = new Vector2(-24f, compact ? 36f : 30f);
            }
        }

        private static void PositionButton(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            Position(rt, anchorMin, anchorMax, pivot, position, size);
            Text label = rt != null ? rt.GetComponentInChildren<Text>() : null;
            if (label != null)
                label.fontSize = Mathf.Max(18, Mathf.RoundToInt(size.y * 0.46f));
        }

        private static void Position(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            foreach (Transform child in root)
            {
                Transform match = FindDescendant(child, name);
                if (match != null) return match;
            }

            return null;
        }
    }
}

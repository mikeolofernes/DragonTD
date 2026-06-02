using UnityEngine;
using UnityEngine.UI;

namespace DragonTD.UI
{
    public static class RuntimeFontScaler
    {
        public const float DefaultScale = 1.18f;

        public static void Apply(GameObject root, float scale = DefaultScale, int extraPixels = 1)
        {
            if (root == null) return;

            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text == null) continue;

                var marker = text.GetComponent<RuntimeFontScaleMarker>();
                if (marker == null)
                    marker = text.gameObject.AddComponent<RuntimeFontScaleMarker>();

                if (marker.originalFontSize <= 0)
                    marker.originalFontSize = text.fontSize;

                text.fontSize = Mathf.Max(text.fontSize, Mathf.CeilToInt(marker.originalFontSize * scale) + extraPixels);
            }
        }
    }
}

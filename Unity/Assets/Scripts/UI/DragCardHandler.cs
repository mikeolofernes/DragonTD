using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonTD.UI
{
    public class DragCardHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string DragonId { get; set; }

        private CanvasGroup _canvasGroup;
        private GameObject _ghost;
        private Canvas _rootCanvas;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(DragonId)) return;
            _rootCanvas = FindRootCanvas();
            if (_rootCanvas == null) return;
            _canvasGroup.alpha = 0.4f;
            _canvasGroup.blocksRaycasts = false;
            _ghost = BuildGhost(_rootCanvas);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost == null || _rootCanvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_rootCanvas.transform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local);
            ((RectTransform)_ghost.transform).anchoredPosition = local;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            if (_ghost != null)
                Destroy(_ghost);
            _ghost = null;
        }

        private void OnDisable()
        {
            if (_ghost != null) { Destroy(_ghost); _ghost = null; }
            if (_canvasGroup != null) { _canvasGroup.alpha = 1f; _canvasGroup.blocksRaycasts = true; }
        }

        private Canvas FindRootCanvas()
        {
            Canvas c = GetComponentInParent<Canvas>();
            while (c != null && !c.isRootCanvas)
                c = c.transform.parent?.GetComponentInParent<Canvas>();
            return c;
        }

        private GameObject BuildGhost(Canvas rootCanvas)
        {
            var go = new GameObject("DragGhost");
            go.transform.SetParent(rootCanvas.transform, false);
            go.transform.SetAsLastSibling();

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = ((RectTransform)transform).rect.size;

            Image sourceImg = GetComponent<Image>();
            Image img = go.AddComponent<Image>();
            if (sourceImg != null)
            {
                img.sprite = sourceImg.sprite;
                Color c = sourceImg.color;
                img.color = new Color(c.r, c.g, c.b, 0.65f);
            }

            Text sourceText = GetComponentInChildren<Text>();
            if (sourceText != null)
            {
                var tGo = new GameObject("GhostText");
                tGo.transform.SetParent(go.transform, false);
                var tRt = tGo.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;
                var t = tGo.AddComponent<Text>();
                t.font = sourceText.font;
                t.fontSize = sourceText.fontSize;
                t.alignment = sourceText.alignment;
                t.color = new Color(1f, 1f, 1f, 0.85f);
                t.text = sourceText.text;
                t.resizeTextForBestFit = sourceText.resizeTextForBestFit;
                t.resizeTextMinSize = sourceText.resizeTextMinSize;
                t.resizeTextMaxSize = sourceText.resizeTextMaxSize;
            }

            var cg = go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            return go;
        }
    }
}

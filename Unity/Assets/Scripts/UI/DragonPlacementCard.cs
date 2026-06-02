using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DragonTD.Core;
using DragonTD.Dragons;
using DragonTD.TowerDefense;

namespace DragonTD.UI
{
    public class DragonPlacementCard : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _manaCostText;
        [SerializeField] private Text _detailsText;
        [SerializeField] private Button _selectButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private DragonInstance _dragon;
        private readonly Dictionary<Graphic, float> _baseGraphicAlpha = new();
        private bool _subscribedToGameState;

        private void Awake()
        {
            EnsureCanvasGroup();
            RuntimeFontScaler.Apply(gameObject, 1.22f, 2);
        }

        private void OnEnable()
        {
            TrySubscribeToGameState();
            RefreshInteractable();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && _subscribedToGameState)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            _subscribedToGameState = false;
        }

        private void Update()
        {
            TrySubscribeToGameState();
        }

        public void Setup(DragonInstance dragon)
        {
            _dragon = dragon;
            EnsureDetailsText();
            if (_nameText != null)
                _nameText.text = dragon.Definition.displayName;
            if (_manaCostText != null)
                _manaCostText.text = $"{dragon.Definition.manaCost} MP";
            if (_detailsText != null)
            {
                string skill = dragon.Definition.ActiveSkill != null
                    ? dragon.Definition.ActiveSkill.displayName
                    : "No active";
                _detailsText.text = $"{dragon.Definition.dragonClass}  R{dragon.Definition.baseStats.range:0.#}\n{skill}";
            }
            RuntimeFontScaler.Apply(gameObject, 1.22f, 2);
            if (_portrait != null && dragon.Definition.visualData.portrait != null)
            {
                _portrait.sprite = dragon.Definition.visualData.portrait;
                _portrait.color = Color.white;
            }
            RefreshInteractable();
        }

        private void Start()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(OnCardClicked);
            RefreshInteractable();
        }

        private void EnsureDetailsText()
        {
            if (_detailsText != null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var go = new GameObject("DetailsText");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.03f, 0.01f);
            rt.anchorMax = new Vector2(0.97f, 0.18f);
            rt.sizeDelta = Vector2.zero;

            _detailsText = go.AddComponent<Text>();
            _detailsText.font = font;
            _detailsText.fontSize = 12;
            _detailsText.color = new Color(0.84f, 0.9f, 1f, 1f);
            _detailsText.alignment = TextAnchor.MiddleCenter;
        }

        private void OnCardClicked()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlanningPhase)
                return;
            if (_dragon != null)
                PlacementManager.Instance.BeginPlacement(_dragon);
        }

        private void HandleStateChanged(GameState _) => RefreshInteractable();

        private void RefreshInteractable()
        {
            bool canPlace = GameManager.Instance == null || GameManager.Instance.IsPlanningPhase;
            if (_selectButton != null)
                _selectButton.interactable = canPlace;

            EnsureCanvasGroup();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = canPlace;
            }

            ApplyVisualState(canPlace);
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }

        private void TrySubscribeToGameState()
        {
            if (_subscribedToGameState || GameManager.Instance == null) return;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            _subscribedToGameState = true;
        }

        private void ApplyVisualState(bool canPlace)
        {
            float multiplier = canPlace ? 1f : 0.38f;
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (!_baseGraphicAlpha.ContainsKey(graphic))
                    _baseGraphicAlpha[graphic] = graphic.color.a;

                Color color = graphic.color;
                color.a = _baseGraphicAlpha[graphic] * multiplier;
                graphic.color = color;
            }
        }
    }
}

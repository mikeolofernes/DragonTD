using UnityEngine;
using UnityEngine.UI;
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

        private DragonInstance _dragon;

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
            if (_portrait != null && dragon.Definition.visualData.portrait != null)
            {
                _portrait.sprite = dragon.Definition.visualData.portrait;
                _portrait.color = Color.white;
            }
        }

        private void Start()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(OnCardClicked);
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
            _detailsText.fontSize = 9;
            _detailsText.color = new Color(0.84f, 0.9f, 1f, 1f);
            _detailsText.alignment = TextAnchor.MiddleCenter;
        }

        private void OnCardClicked()
        {
            if (_dragon != null)
                PlacementManager.Instance.BeginPlacement(_dragon);
        }
    }
}

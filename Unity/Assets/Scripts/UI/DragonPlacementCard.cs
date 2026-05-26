using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonTD.Dragons;
using DragonTD.TowerDefense;

namespace DragonTD.UI
{
    public class DragonPlacementCard : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _manaCostText;
        [SerializeField] private Button _selectButton;

        private DragonInstance _dragon;

        public void Setup(DragonInstance dragon)
        {
            _dragon = dragon;
            _nameText.text = dragon.Definition.displayName;
            _manaCostText.text = dragon.Definition.manaCost.ToString();
            if (dragon.Definition.visualData.portrait != null)
                _portrait.sprite = dragon.Definition.visualData.portrait;
        }

        private void Start() =>
            _selectButton.onClick.AddListener(OnCardClicked);

        private void OnCardClicked()
        {
            if (_dragon != null)
                PlacementManager.Instance.BeginPlacement(_dragon);
        }
    }
}

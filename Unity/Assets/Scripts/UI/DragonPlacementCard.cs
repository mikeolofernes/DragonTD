using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonTD.Dragons;
using DragonTD.TowerDefense;

namespace DragonTD.UI
{
    // HUD card for a single owned dragon. Click to enter placement mode.
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
            _nameText.text = dragon.Data.DragonName;
            _manaCostText.text = dragon.Data.ManaCost.ToString();
            if (dragon.Data.Portrait != null) _portrait.sprite = dragon.Data.Portrait;
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

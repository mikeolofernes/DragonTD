using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonTD.Summoning;
using DragonTD.Dragons;

namespace DragonTD.UI
{
    public class SummonPanel : MonoBehaviour
    {
        [SerializeField] private SummonPool _currentPool;
        [SerializeField] private TextMeshProUGUI _bannerNameText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _singlePullButton;
        [SerializeField] private Button _tenPullButton;
        [SerializeField] private Transform _resultContainer;
        [SerializeField] private GameObject _dragonResultCardPrefab;

        private GachaSystem _gacha = new GachaSystem();
        private List<DragonInstance> _playerInventory;

        private void Start()
        {
            _singlePullButton.onClick.AddListener(OnSinglePull);
            _tenPullButton.onClick.AddListener(OnTenPull);

            if (_currentPool != null)
            {
                RefreshDisplay();
            }
        }

        public void SetPool(SummonPool pool)
        {
            _currentPool = pool;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            _bannerNameText.text = _currentPool.BannerName;
            _costText.text = $"{_currentPool.SummonCostGems} Gems  |  10x {_currentPool.TenPullCostGems} Gems";
        }

        private void OnSinglePull()
        {
            // Stub: always succeed for prototype (no gem deduction)
            DragonData result = _gacha.SinglePull(_currentPool);
            DragonInstance instance = new DragonInstance(result);

            _playerInventory?.Add(instance);

            ShowResults(new DragonData[] { result });
        }

        private void OnTenPull()
        {
            // Stub: always succeed for prototype (no gem deduction)
            DragonData[] results = _gacha.TenPull(_currentPool);

            foreach (DragonData data in results)
            {
                DragonInstance instance = new DragonInstance(data);
                _playerInventory?.Add(instance);
            }

            ShowResults(results);
        }

        private void ShowResults(DragonData[] results)
        {
            // Clear existing result cards
            foreach (Transform child in _resultContainer)
            {
                Destroy(child.gameObject);
            }

            // Instantiate a card for each result
            foreach (DragonData data in results)
            {
                GameObject card = Instantiate(_dragonResultCardPrefab, _resultContainer);
                TextMeshProUGUI label = card.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = data.DragonName + "\n" + data.Rarity;
                }
            }
        }
    }
}

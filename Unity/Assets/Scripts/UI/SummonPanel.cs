using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonTD.Summoning;
using DragonTD.Dragons;
using DragonTD.Core;

namespace DragonTD.UI
{
    public class SummonPanel : MonoBehaviour
    {
        [SerializeField] private SummonPool _currentPool;
        [SerializeField] private Text _bannerNameText;
        [SerializeField] private Text _costText;
        [SerializeField] private Button _singlePullButton;
        [SerializeField] private Button _tenPullButton;
        [SerializeField] private Transform _resultContainer;
        [SerializeField] private GameObject _dragonResultCardPrefab;

        private GachaSystem _gacha = new GachaSystem();

        private void Start()
        {
            _singlePullButton.onClick.AddListener(OnSinglePull);
            _tenPullButton.onClick.AddListener(OnTenPull);

            if (_currentPool != null)
                RefreshDisplay();
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
            if (_currentPool == null) return;
            DragonDefinition result = _gacha.SinglePull(_currentPool);
            if (result == null) return;

            PlayerInventory.Instance?.AddDragon(result);
            ShowResults(new DragonDefinition[] { result });
        }

        private void OnTenPull()
        {
            if (_currentPool == null) return;
            DragonDefinition[] results = _gacha.TenPull(_currentPool);

            foreach (DragonDefinition def in results)
            {
                if (def != null)
                    PlayerInventory.Instance?.AddDragon(def);
            }

            ShowResults(results);
        }

        private void ShowResults(DragonDefinition[] results)
        {
            foreach (Transform child in _resultContainer)
                Destroy(child.gameObject);

            foreach (DragonDefinition def in results)
            {
                if (def == null) continue;
                GameObject card = Instantiate(_dragonResultCardPrefab, _resultContainer);
                Text label = card.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"{def.displayName}\n{def.rarity}";
            }
        }
    }
}

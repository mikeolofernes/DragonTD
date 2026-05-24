using UnityEngine;

namespace DragonTD.Core
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private int _startingMana = 100;
        [SerializeField] private int _startingGems = 1500;

        public int Mana { get; private set; }
        public int Gold { get; private set; }
        public int Gems { get; private set; }

        public event System.Action<int> OnManaChanged;
        public event System.Action<int> OnGoldChanged;
        public event System.Action<int> OnGemsChanged;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool TrySpendMana(int amount)
        {
            if (Mana < amount)
                return false;

            Mana -= amount;
            OnManaChanged?.Invoke(Mana);
            return true;
        }

        public void AddMana(int amount)
        {
            Mana += amount;
            OnManaChanged?.Invoke(Mana);
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public void AddGems(int amount)
        {
            Gems += amount;
            OnGemsChanged?.Invoke(Gems);
        }

        public bool TrySpendGems(int amount)
        {
            if (Gems < amount) return false;
            Gems -= amount;
            OnGemsChanged?.Invoke(Gems);
            return true;
        }

        public void ResetForBattle()
        {
            Mana = _startingMana;
            Gold = 0;
            if (Gems == 0) Gems = _startingGems;
            OnManaChanged?.Invoke(Mana);
            OnGoldChanged?.Invoke(Gold);
        }
    }
}

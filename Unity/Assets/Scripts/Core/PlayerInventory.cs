using UnityEngine;
using System.Collections.Generic;
using DragonTD.Dragons;

namespace DragonTD.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private DragonDefinition[] _starterDragons;

        public List<DragonInstance> OwnedDragons { get; private set; } = new List<DragonInstance>();

        public event System.Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            foreach (DragonDefinition def in _starterDragons)
                AddDragon(def);
        }

        public void AddDragon(DragonDefinition def)
        {
            OwnedDragons.Add(new DragonInstance { Definition = def });
            OnInventoryChanged?.Invoke();
        }
    }
}

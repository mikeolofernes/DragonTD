using UnityEngine;
using System.Collections.Generic;
using DragonTD.Dragons;

namespace DragonTD.Core
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [SerializeField] private DragonData[] _starterDragons;

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
            foreach (DragonData data in _starterDragons)
                AddDragon(data);
        }

        public void AddDragon(DragonData data)
        {
            OwnedDragons.Add(new DragonInstance { Data = data });
            OnInventoryChanged?.Invoke();
        }
    }
}

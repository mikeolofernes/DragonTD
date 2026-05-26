using UnityEngine;
using System.Collections.Generic;

namespace DragonTD.Dragons
{
    [CreateAssetMenu(fileName = "DragonRegistry", menuName = "Dragon Dominion/Dragon Registry")]
    public class DragonRegistry : ScriptableObject
    {
        public DragonDefinition[] allDragons;

        private Dictionary<string, DragonDefinition> _lookup;

        public void Initialize()
        {
            _lookup = new Dictionary<string, DragonDefinition>(allDragons.Length);
            foreach (var d in allDragons)
            {
                if (string.IsNullOrEmpty(d.dragonId))
                {
                    Debug.LogError($"[DragonRegistry] Dragon '{d.displayName}' has no dragonId.");
                    continue;
                }
                if (_lookup.ContainsKey(d.dragonId))
                    Debug.LogError($"[DragonRegistry] Duplicate dragonId: {d.dragonId}");
                else
                    _lookup[d.dragonId] = d;
            }
        }

        public DragonDefinition Get(string dragonId)
        {
            if (_lookup == null) Initialize();
            if (_lookup.TryGetValue(dragonId, out var def)) return def;
            Debug.LogError($"[DragonRegistry] Dragon not found: {dragonId}");
            return null;
        }

        public DragonDefinition[] GetByElement(DragonElement element) =>
            System.Array.FindAll(allDragons, d => d.element == element);

        public DragonDefinition[] GetByRarity(DragonRarity rarity) =>
            System.Array.FindAll(allDragons, d => d.rarity == rarity);

        public DragonDefinition[] GetByClass(DragonClass dragonClass) =>
            System.Array.FindAll(allDragons, d => d.dragonClass == dragonClass);

        public bool CanFuse(string dragonAId, string dragonBId, out FusionEntry entry)
        {
            entry = null;
            var dragonA = Get(dragonAId);
            if (dragonA == null) return false;
            foreach (var e in dragonA.fusionTable.fusionEntries)
            {
                if (e.partnerDragonId == dragonBId) { entry = e; return true; }
            }
            return false;
        }
    }
}

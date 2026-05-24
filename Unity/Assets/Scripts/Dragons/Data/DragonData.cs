using UnityEngine;

namespace DragonTD.Dragons
{
    [CreateAssetMenu(fileName = "NewDragon", menuName = "DragonTD/Dragon")]
    public class DragonData : ScriptableObject
    {
        public int DragonId;
        public string DragonName;
        [TextArea] public string Lore;
        public DragonRarity Rarity;
        public DragonElement Element;
        public DragonRole Role;

        // Base stats
        public float BaseHp;
        public float BaseAttack;
        public float BaseDefense;
        public float BaseSpeed;
        public float BaseRange;

        public int ManaCost;

        // Abilities
        public AbilityData NormalAttack;
        public AbilityData ActiveSkill;
        public AbilityData PassiveSkill;
        public AbilityData UltimateSkill;

        // Visuals
        public Sprite Portrait;
        public GameObject Prefab;

        // Progression
        public DragonData[] FusionCompatibleWith;
        public DragonData EvolvesInto;
    }
}

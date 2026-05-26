using UnityEngine;

namespace DragonTD.Dragons
{
    [CreateAssetMenu(fileName = "New Dragon", menuName = "Dragon Dominion/Dragon Definition")]
    public class DragonDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique key. Format: name_001 e.g. voltaris_001")]
        public string dragonId;
        public string displayName;
        public DragonClass dragonClass;
        public DragonElement element;
        public DragonRarity rarity;

        [Header("Base Stats")]
        public DragonBaseStats baseStats;
        public DragonStatGrowth growth;

        [Header("Skills")]
        public DragonSkillSet skillSet;

        [Header("Evolution")]
        public DragonEvolutionPath evolutionPath;

        [Header("Fusion")]
        public DragonFusionTable fusionTable;

        [Header("Bond")]
        public DragonBondData bondData;

        [Header("Personality")]
        public DragonPersonality personality;

        [Header("Visuals & Audio")]
        public DragonVisualData visualData;

        [Header("Tower Defense")]
        [Tooltip("Mana cost to place this dragon on the battlefield.")]
        public int manaCost = 80;

        // Convenience accessors used by TD combat scripts
        public SkillDefinition NormalAttack => skillSet.normalAttack;
        public SkillDefinition ActiveSkill  => skillSet.activeSkill;
    }
}

using UnityEngine;

namespace DragonTD.Dragons
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Dragon Dominion/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string skillId;           // Format: effect_name_001
        public string displayName;
        [TextArea] public string description;

        [Header("Behavior")]
        public SkillType skillType;
        public TargetType targetType;
        public float baseDamage;
        public float manaCost;
        public float cooldown;

        [Header("Scaling")]
        [Tooltip("Length must be 10. Index = skillLevel - 1.")]
        public float[] levelMultipliers = new float[10] { 1f,1.1f,1.2f,1.3f,1.4f,1.5f,1.65f,1.8f,2f,2.25f };

        [Header("Effects")]
        public StatusEffect[] statusEffects;

        [Header("Assets (Addressable keys)")]
        public string vfxKey;
        public string sfxKey;

        [Header("Tower Defense — Range & AoE")]
        public float range = 4f;
        public bool isAoe;
        public float aoeRadius;

        [Header("Passive On-Hit")]
        public PassiveSkillType passiveType = PassiveSkillType.None;
        public float passiveChance = 1.0f;
        public int passiveChainCount = 2;
        public float passiveSplashPercent = 0.3f;

        public float GetDamageMultiplier(int skillLevel)
        {
            int idx = Mathf.Clamp(skillLevel - 1, 0, levelMultipliers.Length - 1);
            return levelMultipliers[idx];
        }
    }
}

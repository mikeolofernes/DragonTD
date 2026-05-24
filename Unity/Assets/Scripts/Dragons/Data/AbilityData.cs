using UnityEngine;

namespace DragonTD.Dragons
{
    public enum AbilityType
    {
        NormalAttack,
        ActiveSkill,
        PassiveSkill,
        UltimateSkill
    }

    [CreateAssetMenu(fileName = "NewAbility", menuName = "DragonTD/Ability")]
    public class AbilityData : ScriptableObject
    {
        [SerializeField] public string AbilityName;
        [SerializeField] [TextArea] public string Description;
        [SerializeField] public AbilityType Type;
        [SerializeField] public float Cooldown;
        [SerializeField] public float DamageMultiplier = 1f;
        [SerializeField] public float Range;
        [SerializeField] public bool IsAoe;
        [SerializeField] public float AoeRadius;
    }
}

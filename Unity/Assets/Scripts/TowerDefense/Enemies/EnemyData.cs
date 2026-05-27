using UnityEngine;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public enum EnemyFaction { Orc, Troll, Undead, CorruptedDragon }
    public enum EnemyTrait { None, Runner, Brute, Shielded, Regenerating, Flying }
    public enum DamageSource { Projectile, LightningProjectile, Skill, Burn }

    [CreateAssetMenu(fileName = "NewEnemy", menuName = "DragonTD/Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string EnemyName;
        public float MaxHp;
        public float MoveSpeed;
        public float Armor;
        public int GoldValue;
        public int DamageToBase;
        public EnemyFaction Faction;
        public DragonElement Element;
        public bool HasElement;
        public EnemyTrait Trait;
        public float RegenPerSecond;
        public float ProjectileDamageMultiplier = 1f;
        public float SkillDamageMultiplier = 1f;
    }
}

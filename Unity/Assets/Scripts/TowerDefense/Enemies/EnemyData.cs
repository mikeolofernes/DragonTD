using UnityEngine;

namespace DragonTD.TowerDefense
{
    public enum EnemyFaction { Orc, Troll, Undead, CorruptedDragon }

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
    }
}

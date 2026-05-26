namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonBaseStats
    {
        public float hp;
        public float attack;
        public float attackSpeed;
        public float armor;
        public float magicResist;
        public float flightSpeed;
        public float range;
        public float mana;
        [UnityEngine.Range(0f, 1f)] public float critChance;
        [UnityEngine.Range(0f, 1f)] public float elementalAffinity;
    }
}

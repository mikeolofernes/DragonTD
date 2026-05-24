namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonInstance
    {
        public DragonData Data;
        public int Level = 1;
        public int BondLevel = 1;
        public long TotalBattles;
        public float BondXp;

        // Computed stats — level scaling: +8% per level above 1, then multiplied by bond bonus
        public float Hp      => Data.BaseHp      * (1f + (Level - 1) * 0.08f) * BondStatMultiplier;
        public float Attack  => Data.BaseAttack  * (1f + (Level - 1) * 0.08f) * BondStatMultiplier;
        public float Defense => Data.BaseDefense * (1f + (Level - 1) * 0.08f) * BondStatMultiplier;

        // 2% bonus per bond level above 1
        public float BondStatMultiplier => 1f + (BondLevel - 1) * 0.02f;

        // XP required to advance from current BondLevel to the next
        public float BondXpThreshold => 100f * BondLevel;

        public void AddBondXp(float amount)
        {
            BondXp += amount;

            while (BondXp >= BondXpThreshold && BondLevel < 20)
            {
                BondXp -= BondXpThreshold;
                BondLevel++;
            }

            // Cap overflow XP at max bond level
            if (BondLevel >= 20)
                BondXp = 0f;
        }

        public void RecordBattle()
        {
            TotalBattles++;
            AddBondXp(10f);
        }
    }
}

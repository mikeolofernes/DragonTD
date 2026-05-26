namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonInstance
    {
        public DragonDefinition Definition;
        public int Level = 1;
        public int BondLevel = 1;
        public float BondXp;
        public long TotalBattles;
        public DragonEvolutionStage EvolutionStage = DragonEvolutionStage.Hatchling;
        public int SkillLevel = 1;  // 1-10, applies to all active skills

        // Computed stats: base * level growth curve * bond multiplier
        public float Hp      => Definition.baseStats.hp     * LevelMultiplier * BondStatMultiplier;
        public float Attack  => Definition.baseStats.attack  * LevelMultiplier * BondStatMultiplier;
        public float Defense => Definition.baseStats.armor   * LevelMultiplier * BondStatMultiplier;
        public float Range   => Definition.baseStats.range;
        public float AttackSpeed => Definition.baseStats.attackSpeed;

        // Bond multiplier: each bond level above 1 grants the statBoostPercent defined in BondData
        public float BondStatMultiplier
        {
            get
            {
                if (Definition.bondData?.bondLevels == null) return 1f;
                float total = 1f;
                foreach (var bl in Definition.bondData.bondLevels)
                {
                    if (bl.level <= BondLevel) total += bl.statBoostPercent;
                }
                return total;
            }
        }

        // Simple per-level multiplier (+8% per level) when no AnimationCurve is available
        private float LevelMultiplier => 1f + (Level - 1) * 0.08f;

        public float BondXpThreshold
        {
            get
            {
                if (Definition.bondData?.bondLevels == null) return 100f * BondLevel;
                int idx = System.Math.Min(BondLevel - 1, Definition.bondData.bondLevels.Length - 1);
                return Definition.bondData.bondLevels[idx].xpRequired;
            }
        }

        public void AddBondXp(float amount)
        {
            BondXp += amount;
            int maxBond = Definition.bondData?.bondLevels?.Length ?? 7;
            while (BondLevel < maxBond && BondXp >= BondXpThreshold)
            {
                BondXp -= BondXpThreshold;
                BondLevel++;
            }
            if (BondLevel >= maxBond) BondXp = 0f;
        }

        public void RecordBattle()
        {
            TotalBattles++;
            float xp = Definition.bondData?.battleBondXP ?? 10f;
            AddBondXp(xp);
        }
    }
}

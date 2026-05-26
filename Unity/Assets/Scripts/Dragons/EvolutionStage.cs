namespace DragonTD.Dragons
{
    [System.Serializable]
    public class EvolutionStage
    {
        public string stageId;
        public string stageName;
        public int requiredLevel;
        public int requiredBond;
        public int requiredKills;
        public string[] requiredItemIds;
        public DragonBaseStats statBonus;
        public string newSkillUnlockId;    // SkillDefinition asset name; null if none
        public string evolutionVfxKey;     // Addressable key
        public string spriteOverrideKey;   // Addressable key
    }
}

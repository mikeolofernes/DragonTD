namespace DragonTD.Dragons
{
    [System.Serializable]
    public class BondLevel
    {
        public int level;                    // 1–7
        public float xpRequired;
        [UnityEngine.Range(0f, 1f)]
        public float statBoostPercent;       // e.g. 0.05 = +5% to all stats
        public string unlockDescription;
        public string skillUnlockId;         // null if no skill unlocked here
        public bool triggersEvolutionAccess;
        public string transformationVfxKey;  // non-null only at level 7
    }
}

namespace DragonTD.Dragons
{
    [System.Serializable]
    public class FusionEntry
    {
        public string partnerDragonId;        // dragonId of the fusion partner
        public string resultDragonId;          // dragonId of the result
        [UnityEngine.Range(0f, 1f)]
        public float successChance = 0.6f;
        public float bondBonusPerLevel = 0.02f; // per bond level above 3
        public FusionType fusionType;
        public string[] alternateResultIds;    // mutation outcomes
        public bool midBattleFusionAllowed;
        public string fusionVfxKey;
        public string fusionMusicKey;
    }
}

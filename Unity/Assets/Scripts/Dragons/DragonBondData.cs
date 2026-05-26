namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonBondData
    {
        // 7 entries, one per bond level
        public BondLevel[] bondLevels = new BondLevel[7];
        public float battleBondXP  = 10f;
        public float feedBondXP    = 5f;
        public float trainBondXP   = 8f;
        public float exploreBondXP = 6f;
        // Addressable audio keys, one per bond level
        public string[] voiceLineKeys = new string[7];
    }
}

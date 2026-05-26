namespace DragonTD.Dragons
{
    [System.Serializable]
    public class StatusEffect
    {
        public string effectId;
        public string displayName;
        public float duration;
        public float magnitude;
        public bool stackable;
        public int maxStacks;
    }
}

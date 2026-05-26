namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonPersonality
    {
        public string temperament;                 // Bold, Timid, Fierce, Gentle, Cunning
        public string[] preferredFoodIds;
        public DragonElement preferredEnvironment;
        public string[] rivalDragonIds;
        [UnityEngine.Range(0.5f, 2f)]
        public float moraleGainMultiplier = 1f;
        [UnityEngine.Range(0.5f, 2f)]
        public float moraleLossMultiplier = 1f;
        public string[] interactionEventIds;       // triggers hidden lore events
    }
}

namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonEvolutionPath
    {
        // Length 6, index matches DragonEvolutionStage enum
        public EvolutionStage[] stages = new EvolutionStage[6];
    }
}

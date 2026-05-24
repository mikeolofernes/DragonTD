namespace DragonTD.Core
{
    public enum GameState
    {
        MainMenu,
        Setup,          // Pre-wave dragon placement
        Wave,           // Active wave in progress
        BetweenWaves,
        Paused,
        Victory,
        Defeat
    }
}

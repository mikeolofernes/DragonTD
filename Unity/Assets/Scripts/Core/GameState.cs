namespace DragonTD.Core
{
    public enum GameState
    {
        MainMenu,
        Planning,       // Pre-wave and between-wave build/upgrade/fusion phase
        Setup,          // Pre-wave dragon placement
        Wave,           // Active wave in progress
        BetweenWaves,
        Paused,
        Victory,
        Defeat
    }
}

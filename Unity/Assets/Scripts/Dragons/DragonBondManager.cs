namespace DragonTD.Dragons
{
    public static class DragonBondManager
    {
        public static void OnBattleCompleted(DragonInstance dragon)
        {
            dragon.RecordBattle();
        }

        public static void OnFed(DragonInstance dragon, float xpAmount)
        {
            dragon.AddBondXp(xpAmount);
        }

        public static void OnExploration(DragonInstance dragon)
        {
            dragon.AddBondXp(25f);
        }
    }
}

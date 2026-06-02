namespace DragonTD.Core
{
    public static class DailyObjectiveCatalog
    {
        public static readonly DailyObjectiveDefinition[] Objectives =
        {
            new DailyObjectiveDefinition("daily_win_battle", "Win 1 battle", "Win any stage.", DailyObjectiveType.WinBattle, 1, 120, 20, 0),
            new DailyObjectiveDefinition("daily_open_chest", "Open 1 chest", "Open any ready chest.", DailyObjectiveType.OpenChest, 1, 80, 10, 5),
            new DailyObjectiveDefinition("daily_summon_dragon", "Summon 1 dragon", "Use a Dragon Summon Ticket.", DailyObjectiveType.SummonDragon, 1, 0, 25, 10)
        };

        public static DailyObjectiveDefinition Get(string objectiveId)
        {
            foreach (DailyObjectiveDefinition objective in Objectives)
            {
                if (objective.objectiveId == objectiveId)
                    return objective;
            }

            return null;
        }
    }

    public enum DailyObjectiveType
    {
        WinBattle,
        OpenChest,
        SummonDragon
    }

    [System.Serializable]
    public class DailyObjectiveDefinition
    {
        public string objectiveId;
        public string displayName;
        public string description;
        public DailyObjectiveType objectiveType;
        public int targetCount;
        public int goldReward;
        public int essenceReward;
        public int gemReward;

        public DailyObjectiveDefinition(string objectiveId, string displayName, string description, DailyObjectiveType objectiveType, int targetCount, int goldReward, int essenceReward, int gemReward)
        {
            this.objectiveId = objectiveId;
            this.displayName = displayName;
            this.description = description;
            this.objectiveType = objectiveType;
            this.targetCount = targetCount;
            this.goldReward = goldReward;
            this.essenceReward = essenceReward;
            this.gemReward = gemReward;
        }

        public string RewardText => $"+{goldReward} Gold, +{essenceReward} Essence, +{gemReward} Gems";
    }
}

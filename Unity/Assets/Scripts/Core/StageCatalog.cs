namespace DragonTD.Core
{
    using DragonTD.Dragons;

    public static class StageCatalog
    {
        public const string DefaultStageId = "chapter_1_stage_1";

        public static readonly StageDefinition[] Stages =
        {
            new StageDefinition("chapter_1_stage_1", "1-1", "Dominion Road", "Goblin Scout Line", 1f, 1f, "Common/Rare", 1, 0, 1f, 10, 2, 3, 1, DragonRoleTag.Damage),
            new StageDefinition("chapter_1_stage_2", "1-2", "Ember Crossing", "Runner pressure", 1.18f, 1.15f, "Common/Rare/Epic", 2, 1, 1.08f, 10, 2, 4, 1, DragonRoleTag.Slow, DragonRoleTag.Aoe),
            new StageDefinition("chapter_1_stage_3", "1-3", "Frost Gate", "Shielded enemies", 1.36f, 1.3f, "Rare/Epic", 3, 2, 1.14f, 12, 1, 4, 1, DragonRoleTag.AntiShield, DragonRoleTag.Damage),
            new StageDefinition("chapter_1_stage_4", "1-4", "Storm Watch", "Flying/heavy mix", 1.58f, 1.5f, "Rare/Epic/Legendary", 4, 3, 1.22f, 14, 0, 5, 1, DragonRoleTag.AntiFlying, DragonRoleTag.Slow),
            new StageDefinition("chapter_2_stage_1", "2-1", "Frozen Pass", "Ice Shard rush", 1.7f, 1.6f, "Rare/Epic", 5, 1, 1.1f, 12, 1, 5, 2, DragonRoleTag.Slow, DragonRoleTag.Damage),
            new StageDefinition("chapter_2_stage_2", "2-2", "Glacier Hold", "Frost Brutes", 1.95f, 1.75f, "Rare/Epic/Legendary", 6, 2, 1.16f, 12, 1, 5, 2, DragonRoleTag.AntiShield, DragonRoleTag.Aoe),
            new StageDefinition("chapter_2_stage_3", "2-3", "Shardspire", "Shielded glacials", 2.2f, 1.9f, "Epic/Legendary", 7, 2, 1.22f, 14, 0, 6, 2, DragonRoleTag.AntiShield, DragonRoleTag.Damage),
            new StageDefinition("chapter_2_stage_4", "2-4", "Winter Throne", "Full ice assault", 2.5f, 2.1f, "Epic/Legendary/Mythic", 8, 3, 1.3f, 16, 0, 6, 2, DragonRoleTag.AntiFlying, DragonRoleTag.Slow, DragonRoleTag.Aoe)
        };

        public static StageDefinition Get(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId))
                stageId = DefaultStageId;

            foreach (StageDefinition stage in Stages)
            {
                if (stage.stageId == stageId)
                    return stage;
            }

            return Stages[0];
        }

        public static StageDefinition GetNext(string stageId)
        {
            int index = GetIndex(stageId);
            return index >= 0 && index + 1 < Stages.Length ? Stages[index + 1] : null;
        }

        public static int GetIndex(string stageId)
        {
            for (int i = 0; i < Stages.Length; i++)
            {
                if (Stages[i].stageId == stageId)
                    return i;
            }

            return 0;
        }
    }

    [System.Serializable]
    public class StageDefinition
    {
        public string stageId;
        public string stageNumber;
        public int chapter = 1;
        public string displayName;
        public string enemyTheme;
        public float difficultyMultiplier;
        public float rewardMultiplier;
        public string chestPreview;
        public int recommendedLevel;
        public int extraEnemiesPerGroup;
        public float spawnRateMultiplier;
        public int tenLifeStarRequirement;
        public int maxLeaksForStar;
        public int maxDragonsForStar;
        public DragonRoleTag[] recommendedRoles;

        public StageDefinition(string stageId, string stageNumber, string displayName, string enemyTheme, float difficultyMultiplier, float rewardMultiplier, string chestPreview, int recommendedLevel, int extraEnemiesPerGroup, float spawnRateMultiplier, int tenLifeStarRequirement, int maxLeaksForStar, int maxDragonsForStar, int chapter, params DragonRoleTag[] recommendedRoles)
        {
            this.stageId = stageId;
            this.stageNumber = stageNumber;
            this.displayName = displayName;
            this.enemyTheme = enemyTheme;
            this.difficultyMultiplier = difficultyMultiplier;
            this.rewardMultiplier = rewardMultiplier;
            this.chestPreview = chestPreview;
            this.recommendedLevel = recommendedLevel;
            this.extraEnemiesPerGroup = extraEnemiesPerGroup;
            this.spawnRateMultiplier = spawnRateMultiplier;
            this.tenLifeStarRequirement = tenLifeStarRequirement;
            this.maxLeaksForStar = maxLeaksForStar;
            this.maxDragonsForStar = maxDragonsForStar;
            this.chapter = chapter;
            this.recommendedRoles = recommendedRoles ?? new DragonRoleTag[0];
        }

        public string Title => $"Stage {stageNumber}: {displayName}";

        public string ObjectiveText =>
            $"Win the stage\n" +
            $"Finish with {tenLifeStarRequirement}+ lives\n" +
            $"Leak {maxLeaksForStar} or fewer enemies\n" +
            $"Use {maxDragonsForStar} or fewer dragons\n" +
            $"Recommended: {DragonRoleUtility.BuildRoleList(recommendedRoles)}";
    }
}

using System;
using System.Collections.Generic;

namespace DragonTD.Core
{
    [Serializable]
    public class PlayerProgressionSaveData
    {
        public int version = 1;
        public int essence;
        public int gold;
        public int gems;
        public int summonTickets;
        public int gachaPullsSinceLastEpic;
        public int gachaTotalPulls;
        public int damageBuffLevel;
        public int attackSpeedBuffLevel;
        public int startingManaBuffLevel;
        public string currentStageId;
        public int highestUnlockedStageIndex;
        public List<string> clearedStageIds = new();
        public List<StageStarSaveData> stageStars = new();
        public List<string> equippedDragonIds = new();
        public List<ChestSlotSaveData> chestSlots = new();
        public List<EventClaimSaveData> eventClaims = new();
        public List<DailyObjectiveSaveData> dailyObjectives = new();
        public List<DragonProgressionSaveData> dragons = new();
        public BattleRewardResult lastBattleReward;
    }

    [Serializable]
    public class ChestSlotSaveData
    {
        public string rarity;
        public long unlockCompleteUtcTicks;
        public bool claimed;
    }

    [Serializable]
    public class ChestRewardResult
    {
        public int slotIndex;
        public string rarity;
        public int gold;
        public int gems;
        public string summary;
    }

    [Serializable]
    public class EventClaimSaveData
    {
        public string eventId;
        public long lastClaimUtcTicks;
        public int claimCount;
    }

    [Serializable]
    public class DailyObjectiveSaveData
    {
        public string objectiveId;
        public long dateUtcTicks;
        public int progress;
        public bool claimed;
    }

    [Serializable]
    public class StageStarSaveData
    {
        public string stageId;
        public int bestStars;
    }

    [Serializable]
    public class DragonProgressionSaveData
    {
        public string dragonId;
        public int level;
        public int bondLevel;
        public float bondXp;
        public long totalBattles;
        public int evolutionStage;
        public int skillLevel;
    }

    public enum AccountBuffType
    {
        Damage,
        AttackSpeed,
        StartingMana
    }

    public enum LoadoutPresetType
    {
        Balanced,
        Boss,
        FastEnemies,
        ShieldBreak
    }
}

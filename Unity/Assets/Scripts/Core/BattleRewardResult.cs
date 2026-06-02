using System;
using System.Collections.Generic;

namespace DragonTD.Core
{
    [Serializable]
    public class BattleRewardResult
    {
        public bool victory;
        public int wavesCleared;
        public int dragonsRewarded;
        public int totalBondXp;
        public int bondLevelUps;
        public string stageId;
        public string stageTitle;
        public int starsEarned;
        public int bestStars;
        public bool firstClear;
        public string objectiveSummary;
        public int stageBonusGold;
        public int stageBonusEssence;
        public int stageBonusGems;
        public int stageBonusSummonTickets;
        public bool chestAwarded;
        public int chestSlotIndex = -1;
        public string chestRarity;
        public string summary;
        public List<DragonBattleRewardEntry> dragonRewards = new();
    }

    [Serializable]
    public class DragonBattleRewardEntry
    {
        public string dragonId;
        public string displayName;
        public int bondXpGranted;
        public int bondLevelBefore;
        public int bondLevelAfter;
        public long totalBattles;
    }
}

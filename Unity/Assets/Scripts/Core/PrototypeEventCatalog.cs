using System;

namespace DragonTD.Core
{
    public static class PrototypeEventCatalog
    {
        public static readonly PrototypeEventDefinition[] Events =
        {
            new PrototypeEventDefinition(
                "daily_hunt",
                "Daily Hunt",
                "Clear patrol objectives and claim a daily account boost.",
                250,
                35,
                0,
                true,
                false),
            new PrototypeEventDefinition(
                "gem_rush",
                "Gem Rush",
                "Short challenge preview. Full scoring and entry rules arrive with Events v2.",
                0,
                0,
                25,
                false,
                false),
            new PrototypeEventDefinition(
                "clan_raid",
                "Clan Raid",
                "Co-op raid placeholder. Requires Clan/social backend.",
                0,
                0,
                0,
                false,
                true)
        };
    }

    [Serializable]
    public class PrototypeEventDefinition
    {
        public string eventId;
        public string displayName;
        public string description;
        public int goldReward;
        public int essenceReward;
        public int gemReward;
        public bool claimable;
        public bool locked;

        public string RewardText
        {
            get
            {
                if (locked) return "Locked";
                if (!claimable) return gemReward > 0 ? $"Preview: +{gemReward} Gems" : "Preview";

                string reward = string.Empty;
                if (goldReward > 0) reward += $"+{goldReward} Gold";
                if (essenceReward > 0) reward += string.IsNullOrEmpty(reward) ? $"+{essenceReward} Essence" : $", +{essenceReward} Essence";
                if (gemReward > 0) reward += string.IsNullOrEmpty(reward) ? $"+{gemReward} Gems" : $", +{gemReward} Gems";
                return string.IsNullOrEmpty(reward) ? "No reward" : reward;
            }
        }

        public PrototypeEventDefinition(string eventId, string displayName, string description, int goldReward, int essenceReward, int gemReward, bool claimable, bool locked)
        {
            this.eventId = eventId;
            this.displayName = displayName;
            this.description = description;
            this.goldReward = goldReward;
            this.essenceReward = essenceReward;
            this.gemReward = gemReward;
            this.claimable = claimable;
            this.locked = locked;
        }
    }
}

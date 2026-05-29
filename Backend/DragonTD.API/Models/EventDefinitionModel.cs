namespace DragonTD.API.Models;

public class EventDefinition
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // "DailyClaimable" | "ScoredChallenge" | "Locked"
    public string EventType { get; set; } = "DailyClaimable";

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public int GoldReward { get; set; }
    public int EssenceReward { get; set; }
    public int GemReward { get; set; }

    // JSON-serialized EventRewardTier[] for ScoredChallenge
    public string RewardTiersJson { get; set; } = "[]";
}

public class EventRewardTier
{
    public int ScoreThreshold { get; set; }
    public int GoldReward { get; set; }
    public int EssenceReward { get; set; }
    public int GemReward { get; set; }
    public int SummonTickets { get; set; }
}

namespace DragonTD.API.Models;

public record GachaPullRequest(int PlayerId, bool IsTenPull);

public record GachaPullResultItem(int DragonDefinitionId, string DragonName, DragonRarity Rarity, bool IsNew);

public record GachaPullResponse(
    List<GachaPullResultItem> Results,
    int GemsSpent,
    int PullsSinceLastS,
    int TotalPulls,
    int GemsRemaining);

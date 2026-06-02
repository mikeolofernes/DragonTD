namespace DragonTD.API.Models;

public class PlayerProgressionState
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string SaveJson { get; set; } = "{}";
    public string? LastBattleRewardJson { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

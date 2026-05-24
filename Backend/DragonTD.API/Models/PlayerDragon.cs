namespace DragonTD.API.Models;

public class PlayerDragon
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public int DragonDefinitionId { get; set; }
    public DragonDefinition DragonDefinition { get; set; } = null!;
    public int Level { get; set; } = 1;
    public int BondLevel { get; set; } = 1;
    public float BondXp { get; set; } = 0f;
    public long TotalBattles { get; set; } = 0;
    public DateTime ObtainedAt { get; set; } = DateTime.UtcNow;
    public bool IsFavorite { get; set; } = false;
}

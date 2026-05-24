namespace DragonTD.API.Models;

public enum DragonRarity { C, B, A, S, SS, SSS }
public enum DragonElement { Fire, Water, Wind, Earth, Lightning, Ice, Shadow, Light }
public enum DragonRole { Vanguard, Striker, Mystic, Tempest, Guardian, Siege, Assassin }

public class DragonDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lore { get; set; } = string.Empty;
    public DragonRarity Rarity { get; set; }
    public DragonElement Element { get; set; }
    public DragonRole Role { get; set; }
    public float BaseHp { get; set; }
    public float BaseAttack { get; set; }
    public float BaseDefense { get; set; }
    public float BaseSpeed { get; set; }
    public float BaseRange { get; set; }
    public int ManaCost { get; set; }
    public bool IsAvailableInGacha { get; set; } = true;
}

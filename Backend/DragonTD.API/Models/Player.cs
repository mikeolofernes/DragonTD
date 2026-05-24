namespace DragonTD.API.Models;

public class Player
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirebaseUid { get; set; } = string.Empty;
    public int Gems { get; set; } = 0;
    public int Gold { get; set; } = 0;
    public int PlayerLevel { get; set; } = 1;
    public long PlayerXp { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlayerDragon> Dragons { get; set; } = new List<PlayerDragon>();
}

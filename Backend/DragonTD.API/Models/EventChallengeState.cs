namespace DragonTD.API.Models;

public class EventChallengeState
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string EventId { get; set; } = string.Empty;
    public int BestScore { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PlayerEventClaim
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string EventId { get; set; } = string.Empty;
    public DateOnly ClaimDateUtc { get; set; }
    public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;
}

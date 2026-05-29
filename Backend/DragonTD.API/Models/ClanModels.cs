namespace DragonTD.API.Models;

public record ClanShellResponse(bool Locked, string Status, string Message);

public class Clan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int MemberLimit { get; set; } = 30;
    public int RaidScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Player Owner { get; set; } = null!;
    public ICollection<ClanMember> Members { get; set; } = new List<ClanMember>();
}

public class ClanMember
{
    public int Id { get; set; }
    public int ClanId { get; set; }
    public int PlayerId { get; set; }
    public string Role { get; set; } = "Member"; // "Owner" | "Officer" | "Member"
    public int RaidContribution { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Clan Clan { get; set; } = null!;
    public Player Player { get; set; } = null!;
}

public class CreateClanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RaidContributeRequest
{
    public int Score { get; set; }
}

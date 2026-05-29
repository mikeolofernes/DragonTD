using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clan")]
public class ClanController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public ClanController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Tag))
            return BadRequest(new { success = false, error = "Name and tag are required" });

        bool alreadyInClan = await _db.ClanMembers.AnyAsync(m => m.PlayerId == CurrentPlayerId);
        if (alreadyInClan)
            return Conflict(new { success = false, error = "Already in a clan" });

        bool nameTaken = await _db.Clans.AnyAsync(c => c.Name == request.Name);
        if (nameTaken)
            return Conflict(new { success = false, error = "Clan name already taken" });

        Player? player = await _db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return Unauthorized(new { success = false, error = "Player not found" });

        var clan = new Clan
        {
            Name = request.Name,
            Tag = request.Tag.ToUpperInvariant(),
            Description = request.Description,
            OwnerId = CurrentPlayerId
        };
        _db.Clans.Add(clan);
        await _db.SaveChangesAsync();

        _db.ClanMembers.Add(new ClanMember
        {
            ClanId = clan.Id,
            PlayerId = CurrentPlayerId,
            Role = "Owner"
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                id = clan.Id,
                name = clan.Name,
                tag = clan.Tag,
                description = clan.Description,
                member_count = 1,
                raid_score = 0,
                my_role = "Owner"
            },
            error = (string?)null
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyClan()
    {
        ClanMember? membership = await _db.ClanMembers
            .Include(m => m.Clan)
            .ThenInclude(c => c.Members)
            .FirstOrDefaultAsync(m => m.PlayerId == CurrentPlayerId);

        if (membership is null)
        {
            return Ok(new
            {
                success = true,
                data = new ClanShellResponse(false, "not_in_clan", "Player is not in a clan"),
                error = (string?)null
            });
        }

        Clan clan = membership.Clan;
        return Ok(new
        {
            success = true,
            data = new
            {
                locked = false,
                my_role = membership.Role,
                my_contribution = membership.RaidContribution,
                clan = new
                {
                    id = clan.Id,
                    name = clan.Name,
                    tag = clan.Tag,
                    description = clan.Description,
                    member_count = clan.Members.Count,
                    member_limit = clan.MemberLimit,
                    raid_score = clan.RaidScore
                },
                members = clan.Members.Select(m => new
                {
                    player_id = m.PlayerId,
                    role = m.Role,
                    raid_contribution = m.RaidContribution,
                    joined_at = m.JoinedAt
                })
            },
            error = (string?)null
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetClan(int id)
    {
        Clan? clan = await _db.Clans
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clan is null)
            return NotFound(new { success = false, error = "Clan not found" });

        return Ok(new
        {
            success = true,
            data = new
            {
                id = clan.Id,
                name = clan.Name,
                tag = clan.Tag,
                description = clan.Description,
                member_count = clan.Members.Count,
                member_limit = clan.MemberLimit,
                raid_score = clan.RaidScore
            },
            error = (string?)null
        });
    }

    [HttpPost("{id:int}/join")]
    public async Task<IActionResult> Join(int id)
    {
        bool alreadyInClan = await _db.ClanMembers.AnyAsync(m => m.PlayerId == CurrentPlayerId);
        if (alreadyInClan)
            return Conflict(new { success = false, error = "Already in a clan" });

        Clan? clan = await _db.Clans.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id);
        if (clan is null)
            return NotFound(new { success = false, error = "Clan not found" });

        if (clan.Members.Count >= clan.MemberLimit)
            return BadRequest(new { success = false, error = "Clan is full" });

        _db.ClanMembers.Add(new ClanMember
        {
            ClanId = id,
            PlayerId = CurrentPlayerId,
            Role = "Member"
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                clan_id = id,
                clan_name = clan.Name,
                role = "Member"
            },
            error = (string?)null
        });
    }

    [HttpPost("{id:int}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        ClanMember? membership = await _db.ClanMembers
            .FirstOrDefaultAsync(m => m.PlayerId == CurrentPlayerId && m.ClanId == id);

        if (membership is null)
            return BadRequest(new { success = false, error = "Not a member of this clan" });

        if (membership.Role == "Owner")
        {
            Clan? clan = await _db.Clans.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id);
            if (clan is not null)
                _db.Clans.Remove(clan);
        }
        else
        {
            _db.ClanMembers.Remove(membership);
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = new { left_clan_id = id }, error = (string?)null });
    }

    [HttpPost("raid/contribute")]
    public async Task<IActionResult> RaidContribute([FromBody] RaidContributeRequest request)
    {
        ClanMember? membership = await _db.ClanMembers
            .Include(m => m.Clan)
            .FirstOrDefaultAsync(m => m.PlayerId == CurrentPlayerId);

        if (membership is null)
            return BadRequest(new { success = false, error = "Not in a clan" });

        int contribution = Math.Max(0, request.Score);
        membership.RaidContribution += contribution;
        membership.Clan.RaidScore += contribution;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                clan_id = membership.ClanId,
                my_contribution = membership.RaidContribution,
                clan_total = membership.Clan.RaidScore
            },
            error = (string?)null
        });
    }

    [HttpGet("raid/leaderboard")]
    public async Task<IActionResult> RaidLeaderboard()
    {
        var top = await _db.Clans
            .OrderByDescending(c => c.RaidScore)
            .Take(20)
            .Select(c => new
            {
                clan_id = c.Id,
                name = c.Name,
                tag = c.Tag,
                raid_score = c.RaidScore,
                member_count = c.Members.Count
            })
            .ToListAsync();

        return Ok(new { success = true, data = top, error = (string?)null });
    }
}

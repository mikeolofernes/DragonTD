using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DragonTD.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/events")]
public class EventsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        DateTime now = DateTime.UtcNow;
        List<EventDefinition> activeEvents = await _db.EventDefinitions
            .Where(e => e.StartUtc <= now && e.EndUtc >= now)
            .OrderBy(e => e.Id)
            .ToListAsync();

        var scores = await _db.EventChallengeStates
            .Where(s => s.PlayerId == CurrentPlayerId)
            .ToDictionaryAsync(s => s.EventId, s => s.BestScore);

        return Ok(new
        {
            success = true,
            data = activeEvents.Select(e => new
            {
                event_id = e.EventId,
                display_name = e.DisplayName,
                description = e.Description,
                event_type = e.EventType,
                claimable = e.EventType == "DailyClaimable" || e.EventType == "ScoredChallenge",
                locked = e.EventType == "Locked",
                gold_reward = e.GoldReward,
                essence_reward = e.EssenceReward,
                gem_reward = e.GemReward,
                reward_tiers = ParseTiers(e.RewardTiersJson),
                best_score = scores.TryGetValue(e.EventId, out int score) ? score : 0,
                start_utc = e.StartUtc,
                end_utc = e.EndUtc
            }),
            error = (string?)null
        });
    }

    [HttpPost("{eventId}/claim")]
    public async Task<IActionResult> Claim(string eventId)
    {
        DateTime now = DateTime.UtcNow;
        EventDefinition? eventDef = await _db.EventDefinitions
            .FirstOrDefaultAsync(e => e.EventId == eventId && e.StartUtc <= now && e.EndUtc >= now);

        if (eventDef is null)
            return NotFound(new { success = false, error = "Event not found or not active" });
        if (eventDef.EventType == "Locked")
            return BadRequest(new { success = false, error = "Event is not claimable" });

        Player? player = await _db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return Unauthorized(new { success = false, error = "Player not found" });

        if (eventDef.EventType == "DailyClaimable")
            return await ClaimDaily(eventDef, player);

        if (eventDef.EventType == "ScoredChallenge")
            return await ClaimScoredTier(eventDef, player);

        return BadRequest(new { success = false, error = "Unknown event type" });
    }

    [HttpPost("{eventId}/score")]
    public async Task<IActionResult> SubmitScore(string eventId, [FromBody] ScoreRequest request)
    {
        if (!await _db.EventDefinitions.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { success = false, error = "Event not found" });

        EventChallengeState? state = await _db.EventChallengeStates
            .FirstOrDefaultAsync(s => s.PlayerId == CurrentPlayerId && s.EventId == eventId);
        if (state is null)
        {
            state = new EventChallengeState { PlayerId = CurrentPlayerId, EventId = eventId };
            _db.EventChallengeStates.Add(state);
        }

        state.BestScore = Math.Max(state.BestScore, request?.Score ?? 0);
        state.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new { event_id = eventId, best_score = state.BestScore }, error = (string?)null });
    }

    private async Task<IActionResult> ClaimDaily(EventDefinition eventDef, Player player)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool alreadyClaimed = await _db.PlayerEventClaims.AnyAsync(c =>
            c.PlayerId == CurrentPlayerId && c.EventId == eventDef.EventId && c.ClaimDateUtc == today);
        if (alreadyClaimed)
            return Conflict(new { success = false, error = "Event already claimed today" });

        player.Gold += eventDef.GoldReward;
        player.Gems += eventDef.GemReward;
        _db.PlayerEventClaims.Add(new PlayerEventClaim
        {
            PlayerId = CurrentPlayerId,
            EventId = eventDef.EventId,
            ClaimDateUtc = today
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                event_id = eventDef.EventId,
                gold_reward = eventDef.GoldReward,
                essence_reward = eventDef.EssenceReward,
                gem_reward = eventDef.GemReward
            },
            error = (string?)null
        });
    }

    private async Task<IActionResult> ClaimScoredTier(EventDefinition eventDef, Player player)
    {
        // One-time claim per event (DateOnly.MaxValue as sentinel for non-daily)
        bool alreadyClaimed = await _db.PlayerEventClaims.AnyAsync(c =>
            c.PlayerId == CurrentPlayerId && c.EventId == eventDef.EventId);
        if (alreadyClaimed)
            return Conflict(new { success = false, error = "Scored challenge already claimed" });

        EventChallengeState? state = await _db.EventChallengeStates
            .FirstOrDefaultAsync(s => s.PlayerId == CurrentPlayerId && s.EventId == eventDef.EventId);
        int bestScore = state?.BestScore ?? 0;

        List<EventRewardTier> tiers = ParseTiers(eventDef.RewardTiersJson);
        EventRewardTier? bestTier = null;
        foreach (EventRewardTier tier in tiers)
        {
            if (bestScore >= tier.ScoreThreshold)
                bestTier = tier;
        }

        if (bestTier is null)
            return BadRequest(new { success = false, error = $"Score {bestScore} does not meet any reward tier" });

        player.Gold += bestTier.GoldReward;
        player.Gems += bestTier.GemReward;
        _db.PlayerEventClaims.Add(new PlayerEventClaim
        {
            PlayerId = CurrentPlayerId,
            EventId = eventDef.EventId,
            ClaimDateUtc = DateOnly.MaxValue
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                event_id = eventDef.EventId,
                score = bestScore,
                gold_reward = bestTier.GoldReward,
                essence_reward = bestTier.EssenceReward,
                gem_reward = bestTier.GemReward,
                summon_tickets = bestTier.SummonTickets
            },
            error = (string?)null
        });
    }

    private static List<EventRewardTier> ParseTiers(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return new List<EventRewardTier>();
        try
        {
            return JsonSerializer.Deserialize<List<EventRewardTier>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<EventRewardTier>();
        }
        catch
        {
            return new List<EventRewardTier>();
        }
    }
}

public class ScoreRequest
{
    public int Score { get; set; }
}

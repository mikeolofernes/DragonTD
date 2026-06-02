using System.Text.Json;
using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/progression")]
public class ProgressionController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public ProgressionController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        PlayerProgressionState? state = await _db.PlayerProgressionStates
            .FirstOrDefaultAsync(s => s.PlayerId == CurrentPlayerId);
        if (state is null)
            return Content("{}", "application/json");

        return Content(state.SaveJson, "application/json");
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] JsonElement payload)
    {
        PlayerProgressionState state = await GetOrCreateStateAsync();
        state.SaveJson = payload.GetRawText();
        state.UpdatedAt = DateTime.UtcNow;
        await TouchPlayerAsync();
        await _db.SaveChangesAsync();
        return Ok(new { success = true, error = (string?)null });
    }

    [HttpPost("battle-rewards")]
    public async Task<IActionResult> SyncBattleReward([FromBody] JsonElement payload)
    {
        PlayerProgressionState state = await GetOrCreateStateAsync();
        state.SaveJson = payload.GetRawText();
        if (payload.TryGetProperty("last_battle_reward", out JsonElement reward))
            state.LastBattleRewardJson = reward.GetRawText();
        state.UpdatedAt = DateTime.UtcNow;
        await TouchPlayerAsync();
        await _db.SaveChangesAsync();
        return Ok(new { success = true, error = (string?)null });
    }

    private async Task<PlayerProgressionState> GetOrCreateStateAsync()
    {
        PlayerProgressionState? state = await _db.PlayerProgressionStates
            .FirstOrDefaultAsync(s => s.PlayerId == CurrentPlayerId);
        if (state is not null)
            return state;

        state = new PlayerProgressionState { PlayerId = CurrentPlayerId };
        _db.PlayerProgressionStates.Add(state);
        return state;
    }

    private async Task TouchPlayerAsync()
    {
        Player? player = await _db.Players.FindAsync(CurrentPlayerId);
        if (player is not null)
            player.LastSyncedAt = DateTime.UtcNow;
    }
}

using DragonTD.API.Models;
using DragonTD.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace DragonTD.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GachaController : ControllerBase
{
    private readonly GachaService _gacha;
    public GachaController(GachaService gacha) => _gacha = gacha;

    /// <summary>Single pull (300 gems) or ten-pull (2700 gems).</summary>
    [HttpPost("pull")]
    public async Task<ActionResult<GachaPullResponse>> Pull([FromBody] GachaPullRequest request)
    {
        try
        {
            return Ok(await _gacha.PullAsync(request));
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Returns pity state for the requesting player.</summary>
    [HttpGet("pity/{playerId}")]
    public async Task<ActionResult<object>> GetPity(int playerId, [FromServices] DragonTD.API.Data.AppDbContext db)
    {
        var player = await db.Players.FindAsync(playerId);
        if (player is null) return NotFound();
        return Ok(new { player.GachaPullsSinceLastS, player.GachaTotalPulls });
    }
}

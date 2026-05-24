using DragonTD.API.Models;
using DragonTD.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace DragonTD.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DragonController : ControllerBase
{
    private readonly DragonService _service;
    public DragonController(DragonService service) => _service = service;

    [HttpGet("definitions")]
    public async Task<ActionResult<List<DragonDefinition>>> GetDefinitions() =>
        Ok(await _service.GetAllDefinitionsAsync());

    [HttpGet("player/{playerId}")]
    public async Task<ActionResult<List<PlayerDragon>>> GetPlayerDragons(int playerId) =>
        Ok(await _service.GetPlayerDragonsAsync(playerId));

    [HttpPost("player/{playerId}/add/{dragonId}")]
    public async Task<ActionResult<PlayerDragon>> AddDragon(int playerId, int dragonId)
    {
        var result = await _service.AddDragonToPlayerAsync(playerId, dragonId);
        return CreatedAtAction(nameof(GetPlayerDragons), new { playerId }, result);
    }

    [HttpPost("{playerDragonId}/bond")]
    public async Task<IActionResult> UpdateBond(int playerDragonId, [FromBody] float bondXpGain)
    {
        await _service.UpdateBondAsync(playerDragonId, bondXpGain);
        return NoContent();
    }
}

using DragonTD.API.Data;
using DragonTD.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayerController : ControllerBase
{
    private readonly AppDbContext _db;
    public PlayerController(AppDbContext db) => _db = db;

    [HttpGet("{id}")]
    public async Task<ActionResult<Player>> GetPlayer(int id)
    {
        var player = await _db.Players.Include(p => p.Dragons).FirstOrDefaultAsync(p => p.Id == id);
        return player is null ? NotFound() : Ok(player);
    }

    [HttpPost("register")]
    public async Task<ActionResult<Player>> Register([FromBody] RegisterRequest req)
    {
        if (await _db.Players.AnyAsync(p => p.FirebaseUid == req.FirebaseUid))
            return Conflict("Player already exists.");
        var player = new Player { Username = req.Username, FirebaseUid = req.FirebaseUid, Gems = 1500 };
        _db.Players.Add(player);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, player);
    }

    [HttpPatch("{id}/login")]
    public async Task<IActionResult> UpdateLastLogin(int id)
    {
        var player = await _db.Players.FindAsync(id);
        if (player is null) return NotFound();
        player.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record RegisterRequest(string Username, string FirebaseUid);

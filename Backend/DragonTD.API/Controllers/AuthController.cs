using DragonTD.API.Data;
using DragonTD.API.Models;
using DragonTD.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace DragonTD.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokens;

    public AuthController(AppDbContext db, JwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("device")]
    public async Task<IActionResult> LoginWithDevice([FromBody] DeviceAuthRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { success = false, data = (object?)null, error = "device_id is required" });

        string firebaseUid = $"device:{request.DeviceId.Trim()}";
        Player? player = await _db.Players.FirstOrDefaultAsync(p => p.FirebaseUid == firebaseUid);
        if (player is null)
        {
            player = new Player
            {
                FirebaseUid = firebaseUid,
                Username = string.IsNullOrWhiteSpace(request.DisplayName) ? $"Player {request.DeviceId}" : request.DisplayName.Trim(),
                Gems = 250,
                Gold = 0
            };
            _db.Players.Add(player);
        }

        player.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        AuthSessionDto session = _tokens.CreateSession(player);
        return Ok(new
        {
            success = true,
            data = new
            {
                playerId = session.PlayerId,
                accessToken = session.AccessToken,
                refreshToken = session.RefreshToken,
                expiresUtcTicks = session.ExpiresUtcTicks
            },
            error = (string?)null
        });
    }
}

public class DeviceAuthRequest
{
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

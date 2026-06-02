using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DragonTD.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentPlayerId
    {
        get
        {
            string? raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("player_id");
            return int.TryParse(raw, out int playerId) ? playerId : 0;
        }
    }
}

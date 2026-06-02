using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DragonTD.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace DragonTD.API.Services;

public class JwtTokenService
{
    private readonly JwtTokenOptions _options;

    public JwtTokenService(JwtTokenOptions options)
    {
        _options = options;
    }

    public AuthSessionDto CreateSession(Player player)
    {
        DateTime expiresUtc = DateTime.UtcNow.AddDays(7);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
                new Claim("player_id", player.Id.ToString()),
                new Claim("device_id", player.FirebaseUid)
            },
            expires: expiresUtc,
            signingCredentials: credentials);

        return new AuthSessionDto(
            player.Id.ToString(),
            new JwtSecurityTokenHandler().WriteToken(token),
            string.Empty,
            expiresUtc.Ticks);
    }
}

public record AuthSessionDto(string PlayerId, string AccessToken, string RefreshToken, long ExpiresUtcTicks);

public record JwtTokenOptions(string Secret, string Issuer, string Audience);

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DragonTD.API.Data;
using Microsoft.Extensions.DependencyInjection;

namespace DragonTD.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task DeviceLoginCreatesPlayerAndReturnsBearerSession()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/device", new
        {
            device_id = "device-001",
            display_name = "Tester"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        JsonElement data = body.RootElement.GetProperty("data");
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("playerId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("accessToken").GetString()));
        Assert.True(data.GetProperty("expiresUtcTicks").GetInt64() > DateTime.UtcNow.Ticks);

        using IServiceScope scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(db.Players, p => p.FirebaseUid == "device:device-001" && p.Username == "Tester");
    }

    [Fact]
    public async Task DeviceLoginReusesExistingDevicePlayer()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/v1/auth/device", new
        {
            device_id = "same-device",
            display_name = "First"
        });
        var second = await client.PostAsJsonAsync("/api/v1/auth/device", new
        {
            device_id = "same-device",
            display_name = "Second"
        });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(db.Players.Where(p => p.FirebaseUid == "device:same-device"));
    }
}

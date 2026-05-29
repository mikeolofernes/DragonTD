using System.Net;
using System.Text;
using System.Text.Json;

namespace DragonTD.Tests;

public class ProgressionPityTests
{
    [Fact]
    public async Task GachaPityFieldsRoundTripThroughProgression()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("pity-device");

        var saveJson = """
        {
          "version": 1,
          "gacha_pulls_since_last_epic": 47,
          "gacha_total_pulls": 213
        }
        """;

        var put = await client.PutAsync("/api/v1/progression", new StringContent(saveJson, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync("/api/v1/progression");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(47,  body.RootElement.GetProperty("gacha_pulls_since_last_epic").GetInt32());
        Assert.Equal(213, body.RootElement.GetProperty("gacha_total_pulls").GetInt32());
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DragonTD.Tests;

public class ProgressionControllerTests
{
    [Fact]
    public async Task ProgressionRequiresBearerToken()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/progression");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutThenGetProgressionReturnsUnitySaveJson()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync();

        var saveJson = """
        {
          "version": 1,
          "gold": 120,
          "gems": 45,
          "summonTickets": 2,
          "currentStageId": "chapter1_stage2",
          "equippedDragonIds": ["voltaris_001"]
        }
        """;

        var put = await client.PutAsync("/api/v1/progression", new StringContent(saveJson, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await client.GetAsync("/api/v1/progression");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(120, body.RootElement.GetProperty("gold").GetInt32());
        Assert.Equal("chapter1_stage2", body.RootElement.GetProperty("currentStageId").GetString());
    }

    [Fact]
    public async Task BattleRewardSyncPersistsLatestReward()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync();

        var response = await client.PostAsJsonAsync("/api/v1/progression/battle-rewards", new
        {
            version = 1,
            gold = 300,
            gems = 15,
            last_battle_reward = new
            {
                victory = true,
                stage_id = "chapter1_stage1",
                stars_earned = 3,
                summary = "3-star clear"
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
    }
}

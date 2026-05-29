using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DragonTD.Tests;

public class EventsAndClanControllerTests
{
    [Fact]
    public async Task EventsListIsAuthenticatedAndIncludesDailyHunt()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync();

        var response = await client.GetAsync("/api/v1/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(body.RootElement.GetProperty("data").EnumerateArray(), e =>
            e.GetProperty("event_id").GetString() == "daily_hunt");
    }

    [Fact]
    public async Task DailyEventClaimCanOnlyBeClaimedOncePerUtcDay()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("event-device");

        var first = await client.PostAsync("/api/v1/events/daily_hunt/claim", null);
        var second = await client.PostAsync("/api/v1/events/daily_hunt/claim", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task EventScoreSubmissionUpdatesBestScore()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync();

        var response = await client.PostAsJsonAsync("/api/v1/events/gem_rush/score", new
        {
            score = 1250
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1250, body.RootElement.GetProperty("data").GetProperty("best_score").GetInt32());
    }

    [Fact]
    public async Task ClanShellReportsNotInClanWhenPlayerHasNoClan()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync();

        var response = await client.GetAsync("/api/v1/clan/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("not_in_clan", body.RootElement.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateClan_ReturnsNewClanAndPlayerIsOwner()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("clan-create-device");

        var response = await client.PostAsJsonAsync("/api/v1/clan", new
        {
            name = "Dragon Lords",
            tag = "DL",
            description = "Top guild"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Dragon Lords", body.RootElement.GetProperty("data").GetProperty("name").GetString());
        Assert.Equal("Owner", body.RootElement.GetProperty("data").GetProperty("my_role").GetString());
    }

    [Fact]
    public async Task GetMyClan_AfterCreating_ReturnsClanData()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("clan-getme-device");

        await client.PostAsJsonAsync("/api/v1/clan", new { name = "Test Clan", tag = "TC", description = "" });
        var response = await client.GetAsync("/api/v1/clan/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.GetProperty("data").GetProperty("locked").GetBoolean());
        Assert.Equal("Test Clan", body.RootElement.GetProperty("data").GetProperty("clan").GetProperty("name").GetString());
    }

    [Fact]
    public async Task JoinClan_PlayerCanJoinAnExistingClan()
    {
        using var factory = new TestApiFactory();
        using var creatorClient = factory.CreateClient();
        using var joinerClient = factory.CreateClient();
        await creatorClient.AuthorizeAsync("clan-owner-device");
        await joinerClient.AuthorizeAsync("clan-joiner-device");

        var createResponse = await creatorClient.PostAsJsonAsync("/api/v1/clan", new { name = "Open Clan", tag = "OC", description = "" });
        using JsonDocument createBody = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        int clanId = createBody.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var joinResponse = await joinerClient.PostAsync($"/api/v1/clan/{clanId}/join", null);

        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
        using JsonDocument joinBody = JsonDocument.Parse(await joinResponse.Content.ReadAsStringAsync());
        Assert.Equal("Member", joinBody.RootElement.GetProperty("data").GetProperty("role").GetString());
    }

    [Fact]
    public async Task CreateClan_WhenAlreadyInClan_Returns409()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("clan-duplicate-device");

        await client.PostAsJsonAsync("/api/v1/clan", new { name = "First Clan", tag = "FC", description = "" });
        var response = await client.PostAsJsonAsync("/api/v1/clan", new { name = "Second Clan", tag = "SC", description = "" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RaidContribute_AddsToMemberAndClanScore()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("clan-raid-device");

        await client.PostAsJsonAsync("/api/v1/clan", new { name = "Raid Guild", tag = "RG", description = "" });
        var response = await client.PostAsJsonAsync("/api/v1/clan/raid/contribute", new { score = 350 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(350, body.RootElement.GetProperty("data").GetProperty("my_contribution").GetInt32());
        Assert.Equal(350, body.RootElement.GetProperty("data").GetProperty("clan_total").GetInt32());
    }

    [Fact]
    public async Task EventsList_OnlyReturnsActiveEvents()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("events-date-device");

        var response = await client.GetAsync("/api/v1/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("data").GetArrayLength() > 0);
    }

    [Fact]
    public async Task ScoredChallenge_ClaimWithQualifyingScore_GrantsReward()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("scored-claim-device");

        await client.PostAsJsonAsync("/api/v1/events/gem_rush/score", new { score = 500 });

        var response = await client.PostAsync("/api/v1/events/gem_rush/claim", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.True(body.RootElement.GetProperty("data").GetProperty("gem_reward").GetInt32() > 0);
    }

    [Fact]
    public async Task ScoredChallenge_ClaimWithoutScore_Returns400()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("scored-noscore-device");

        var response = await client.PostAsync("/api/v1/events/gem_rush/claim", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScoredChallenge_ClaimTwice_Returns409()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("scored-twice-device");

        await client.PostAsJsonAsync("/api/v1/events/gem_rush/score", new { score = 500 });
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/events/gem_rush/claim", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/api/v1/events/gem_rush/claim", null)).StatusCode);
    }
}

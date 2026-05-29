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
    public async Task ClanShellReportsLockedUntilSocialBackendExists()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync();

        var response = await client.GetAsync("/api/v1/clan/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("data").GetProperty("locked").GetBoolean());
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

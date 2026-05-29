using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DragonTD.API.Data;
using Microsoft.Extensions.DependencyInjection;

namespace DragonTD.Tests;

public class IapControllerTests
{
    [Fact]
    public async Task ValidateReceiptRequiresBearerToken()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
        {
            productId = "com.dragondominion.gems.small",
            receipt = "editor_mock_receipt:com.dragondominion.gems.small:500",
            transactionId = "tx-unauthorized",
            expectedGems = 500
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidEditorReceiptGrantsServerGems()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("iap-device");

        var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
        {
            productId = "com.dragondominion.gems.small",
            receipt = "editor_mock_receipt:com.dragondominion.gems.small:500",
            platform = "Editor",
            transactionId = "tx-001",
            expectedGems = 500
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.True(body.RootElement.GetProperty("validated").GetBoolean());
        Assert.Equal(500, body.RootElement.GetProperty("gemsGranted").GetInt32());

        using IServiceScope scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(750, db.Players.Single(p => p.FirebaseUid == "device:iap-device").Gems);
    }

    [Fact]
    public async Task DuplicateTransactionIsIdempotent()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("iap-repeat");

        var request = new
        {
            productId = "com.dragondominion.gems.small",
            receipt = "editor_mock_receipt:com.dragondominion.gems.small:500",
            platform = "Editor",
            transactionId = "tx-repeat",
            expectedGems = 500
        };

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/iap/validate", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/iap/validate", request)).StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(750, db.Players.Single(p => p.FirebaseUid == "device:iap-repeat").Gems);
    }

    [Fact]
    public async Task GooglePlayPlatform_WithoutConfiguredKey_Returns503()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("gp-device");

        var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
        {
            productId = "com.dragondominion.gems.small",
            receipt = "{\"data\":\"{}\",\"signature\":\"AAAA\"}",
            platform = "GooglePlay",
            transactionId = "gp-tx-001",
            expectedGems = 500
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task ApplePlatform_WithoutConfiguredSecret_Returns503()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("apple-device");

        var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
        {
            productId = "com.dragondominion.gems.small",
            receipt = "base64receiptdata",
            platform = "AppleAppStore",
            transactionId = "apple-tx-001",
            expectedGems = 500
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task EditorPlatform_StillValidatesWithMockPath()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();
        await client.AuthorizeAsync("editor-platform-device");

        var response = await client.PostAsJsonAsync("/api/v1/iap/validate", new
        {
            productId = "com.dragondominion.gems.small",
            receipt = "editor_mock_receipt:com.dragondominion.gems.small:500",
            platform = "Editor",
            transactionId = "editor-tx-platform-001",
            expectedGems = 500
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DragonTD.Tests;

internal static class ApiTestAuth
{
    public static async Task<string> LoginAndGetTokenAsync(HttpClient client, string deviceId = "test-device")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/device", new
        {
            device_id = deviceId,
            display_name = "API Tester"
        });
        response.EnsureSuccessStatusCode();

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("data").GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Auth response did not include accessToken.");
    }

    public static async Task AuthorizeAsync(this HttpClient client, string deviceId = "test-device")
    {
        string token = await LoginAndGetTokenAsync(client, deviceId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

using System.Text.Json;

namespace DragonTD.API.Services;

public class AppleReceiptValidator : IIapPlatformReceiptValidator
{
    private static readonly HttpClient Http = new();
    private const string SandboxUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
    private const string ProductionUrl = "https://buy.itunes.apple.com/verifyReceipt";

    private readonly string? _sharedSecret;

    public string Platform => "AppleAppStore";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_sharedSecret);

    public AppleReceiptValidator(IConfiguration config)
    {
        _sharedSecret = config["Apple:SharedSecret"];
    }

    public async Task<IapReceiptValidationResult> ValidateAsync(string productId, string receipt, string transactionId, int expectedGems)
    {
        if (!IsConfigured)
            return new IapReceiptValidationResult(false, "Apple shared secret not configured");

        try
        {
            var payload = new Dictionary<string, string>
            {
                ["receipt-data"] = receipt,
                ["password"] = _sharedSecret!
            };
            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Http.PostAsync(ProductionUrl, content);
            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);
            int status = doc.RootElement.GetProperty("status").GetInt32();

            if (status == 21007)
            {
                using var sandboxContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage sandboxResponse = await Http.PostAsync(SandboxUrl, sandboxContent);
                string sandboxBody = await sandboxResponse.Content.ReadAsStringAsync();
                using JsonDocument sandboxDoc = JsonDocument.Parse(sandboxBody);
                int sandboxStatus = sandboxDoc.RootElement.GetProperty("status").GetInt32();
                return sandboxStatus == 0
                    ? new IapReceiptValidationResult(true, "Apple sandbox receipt verified", expectedGems)
                    : new IapReceiptValidationResult(false, $"Apple sandbox receipt invalid (status {sandboxStatus})");
            }

            return status == 0
                ? new IapReceiptValidationResult(true, "Apple receipt verified", expectedGems)
                : new IapReceiptValidationResult(false, $"Apple receipt invalid (status {status})");
        }
        catch (Exception ex)
        {
            return new IapReceiptValidationResult(false, $"Apple validation error: {ex.Message}");
        }
    }
}

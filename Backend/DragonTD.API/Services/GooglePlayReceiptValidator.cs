using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DragonTD.API.Services;

public class GooglePlayReceiptValidator : IIapPlatformReceiptValidator
{
    private readonly string? _base64PublicKey;

    public string Platform => "GooglePlay";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_base64PublicKey);

    public GooglePlayReceiptValidator(IConfiguration config)
    {
        _base64PublicKey = config["GooglePlay:PublicKey"];
    }

    public Task<IapReceiptValidationResult> ValidateAsync(string productId, string receipt, string transactionId, int expectedGems)
    {
        if (!IsConfigured)
            return Task.FromResult(new IapReceiptValidationResult(false, "Google Play public key not configured"));

        try
        {
            using JsonDocument doc = JsonDocument.Parse(receipt);
            string data = doc.RootElement.GetProperty("data").GetString() ?? string.Empty;
            string signature = doc.RootElement.GetProperty("signature").GetString() ?? string.Empty;

            byte[] keyBytes = Convert.FromBase64String(_base64PublicKey!);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] sigBytes = Convert.FromBase64String(signature);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            bool valid = rsa.VerifyData(dataBytes, sigBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

            return Task.FromResult(valid
                ? new IapReceiptValidationResult(true, "Google Play receipt verified", expectedGems)
                : new IapReceiptValidationResult(false, "Google Play signature invalid"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new IapReceiptValidationResult(false, $"Google Play validation error: {ex.Message}"));
        }
    }
}

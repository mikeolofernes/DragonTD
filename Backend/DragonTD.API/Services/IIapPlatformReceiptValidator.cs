namespace DragonTD.API.Services;

public interface IIapPlatformReceiptValidator
{
    string Platform { get; }
    bool IsConfigured { get; }
    Task<IapReceiptValidationResult> ValidateAsync(string productId, string receipt, string transactionId, int expectedGems);
}

public record IapReceiptValidationResult(bool Success, string Message, int GemsGranted = 0);

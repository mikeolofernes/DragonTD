using DragonTD.API.Data;
using DragonTD.API.Models;
using DragonTD.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonTD.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/iap")]
public class IapController : ApiControllerBase
{
    private static readonly Dictionary<string, int> GemPacks = new()
    {
        ["com.dragondominion.gems.small"] = 500,
        ["com.dragondominion.gems.medium"] = 1200,
        ["com.dragondominion.gems.large"] = 3000,
        ["com.dragondominion.gems.epic"] = 6500,
        ["com.dragondominion.gems.legendary"] = 14000
    };

    private readonly AppDbContext _db;
    private readonly IEnumerable<IIapPlatformReceiptValidator> _validators;

    public IapController(AppDbContext db, IEnumerable<IIapPlatformReceiptValidator> validators)
    {
        _db = db;
        _validators = validators;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] IapValidationRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ProductId))
            return BadRequest(StorePurchaseResultDto.Fail("Missing purchase request"));
        if (!GemPacks.TryGetValue(request.ProductId, out int gems))
            return BadRequest(StorePurchaseResultDto.Fail("Unknown product", request.ProductId));
        if (request.ExpectedGems != gems)
            return BadRequest(StorePurchaseResultDto.Fail("Gem amount mismatch", request.ProductId));

        string platform = request.Platform ?? "Editor";
        IapReceiptValidationResult validationResult = await ValidateByPlatform(platform, request, gems);

        if (!validationResult.Success)
        {
            if (validationResult.Message.Contains("not configured"))
                return StatusCode(503, StorePurchaseResultDto.Fail(validationResult.Message, request.ProductId));
            return BadRequest(StorePurchaseResultDto.Fail(validationResult.Message, request.ProductId));
        }

        IapPurchaseReceipt? existing = await _db.IapPurchaseReceipts
            .FirstOrDefaultAsync(r => r.TransactionId == request.TransactionId);
        if (existing is not null)
            return Ok(StorePurchaseResultDto.Valid(existing.ProductId, existing.TransactionId, existing.GemsGranted, "Receipt already validated"));

        Player? player = await _db.Players.FindAsync(CurrentPlayerId);
        if (player is null)
            return Unauthorized(StorePurchaseResultDto.Fail("Player not found", request.ProductId));

        player.Gems += gems;
        _db.IapPurchaseReceipts.Add(new IapPurchaseReceipt
        {
            PlayerId = player.Id,
            ProductId = request.ProductId,
            TransactionId = request.TransactionId ?? Guid.NewGuid().ToString("N"),
            Receipt = request.Receipt ?? string.Empty,
            Platform = platform,
            GemsGranted = gems
        });
        await _db.SaveChangesAsync();

        return Ok(StorePurchaseResultDto.Valid(request.ProductId, request.TransactionId, gems, $"Validated purchase: +{gems} Gems"));
    }

    private async Task<IapReceiptValidationResult> ValidateByPlatform(string platform, IapValidationRequest request, int gems)
    {
        if (platform == "Editor")
        {
            string expectedReceipt = $"editor_mock_receipt:{request.ProductId}:{gems}";
            return request.Receipt == expectedReceipt
                ? new IapReceiptValidationResult(true, "Editor mock receipt valid", gems)
                : new IapReceiptValidationResult(false, "Receipt validation failed");
        }

        IIapPlatformReceiptValidator? validator = null;
        foreach (IIapPlatformReceiptValidator v in _validators)
        {
            if (v.Platform == platform)
            {
                validator = v;
                break;
            }
        }

        if (validator == null)
            return new IapReceiptValidationResult(false, $"Unknown platform: {platform}");

        if (!validator.IsConfigured)
            return new IapReceiptValidationResult(false, $"{platform} validator not configured");

        return await validator.ValidateAsync(request.ProductId!, request.Receipt ?? string.Empty, request.TransactionId ?? string.Empty, gems);
    }
}

public class IapValidationRequest
{
    public string? ProductId { get; set; }
    public string? Receipt { get; set; }
    public string? Platform { get; set; }
    public string? TransactionId { get; set; }
    public int ExpectedGems { get; set; }
}

public record StorePurchaseResultDto(bool Success, bool Validated, string? ProductId, string? TransactionId, int GemsGranted, string Message)
{
    public static StorePurchaseResultDto Valid(string productId, string? transactionId, int gems, string message) =>
        new(true, true, productId, transactionId, gems, message);

    public static StorePurchaseResultDto Fail(string message, string? productId = null) =>
        new(false, false, productId, null, 0, message);
}

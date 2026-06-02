namespace DragonTD.API.Models;

public class IapPurchaseReceipt
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public string ProductId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Receipt { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public int GemsGranted { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

namespace FinancialIntelligence.Api.Models;

public sealed class TransactionRecord
{
    public Guid TransactionId { get; set; }
    public Guid LoadId { get; set; }
    public Guid BusinessId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string? NormalizedMerchantName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? Channel { get; set; }
}
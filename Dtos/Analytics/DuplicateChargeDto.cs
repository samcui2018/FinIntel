namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class DuplicateChargeDto
{
    public Guid TransactionId { get; set; }

    public DateTime TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public string MerchantName { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }

    public string? AuthorizationCode { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public string DuplicateReason { get; set; } = string.Empty;
    public int DuplicateCount { get; set; }
}
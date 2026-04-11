namespace FinancialIntelligence.Api.Models;

public class CanonicalTransactionRow
{
    public int SourceRowNumber { get; set; }

    public string? SourceTransactionId { get; set; }
    public DateTime? TransactionDate { get; set; }
    public DateTime? PostedDate { get; set; }

    public decimal? Amount { get; set; }
    public string? CurrencyCode { get; set; }

    public string? Description { get; set; }
    public string? MerchantName { get; set; }

    public string? ReferenceNumber { get; set; }
    public string? AuthorizationCode { get; set; }

    public string? MerchantAccountId { get; set; }
    public string? CardAccountId { get; set; }

    public Dictionary<string, string?> ExtraFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
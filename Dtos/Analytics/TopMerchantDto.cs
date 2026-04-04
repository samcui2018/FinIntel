namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class TopMerchantDto
{
    public string MerchantName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int TransactionCount { get; set; }

    public string CurrencyCode { get; set; } = "USD";
}
namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class AnalyticsSummaryDto
{
    public int TransactionCount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AverageAmount { get; set; }

    public decimal ThisMonthAmount { get; set; }

    public string? TopMerchant { get; set; }

    public decimal TopVendorPct { get; set; }

    public DateTime? LatestUploadAt { get; set; }

    public string CurrencyCode { get; set; } = "USD";
}
namespace FinancialIntelligence.Api.Models;

public sealed class BenchmarkProfile
{
    public string Segment { get; set; } = "SMB-General";

    public decimal AvgMonthlySpend { get; set; }
    public decimal AvgTransactionAmount { get; set; }
    public decimal TopVendorConcentrationPct { get; set; }

    public string CurrencyCode { get; set; } = "USD";
}
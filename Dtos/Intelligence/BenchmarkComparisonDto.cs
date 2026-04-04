namespace FinancialIntelligence.Api.Dtos.Intelligence;

public sealed class BenchmarkComparisonDto
{
    public string Segment { get; set; } = "SMB-General";

    public decimal BusinessMonthlySpend { get; set; }
    public decimal BenchmarkMonthlySpend { get; set; }
    public decimal MonthlySpendDelta { get; set; }
    public decimal MonthlySpendDeltaPct { get; set; }

    public decimal BusinessAverageTransaction { get; set; }
    public decimal BenchmarkAverageTransaction { get; set; }
    public decimal AverageTransactionDelta { get; set; }
    public decimal AverageTransactionDeltaPct { get; set; }

    public decimal BusinessTopVendorConcentrationPct { get; set; }
    public decimal BenchmarkTopVendorConcentrationPct { get; set; }
    public decimal TopVendorConcentrationDeltaPct { get; set; }

    public string CurrencyCode { get; set; } = "USD";
}
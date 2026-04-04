namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class MonthlySpendDto
{
    public DateTime MonthStart { get; set; }

    public decimal Amount { get; set; }

    public int TransactionCount { get; set; }

    public string CurrencyCode { get; set; } = "USD";
}
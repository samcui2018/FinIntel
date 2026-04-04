namespace FinancialIntelligence.Api.Dtos.Analytics;

public class MonthlyTrendPointResponse
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}
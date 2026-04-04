namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class CategorySpendDto
{
    public string Category { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int TransactionCount { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public DateTime MonthStart { get; set; }
}

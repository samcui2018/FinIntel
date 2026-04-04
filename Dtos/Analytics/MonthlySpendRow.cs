namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class MonthlySpendRow
{
    public DateTime MonthStart { get; set; }
    public decimal Amount { get; set; }
}

public sealed class MerchantSpendRow
{
    public string MerchantName { get; set; } = "";
    public decimal Amount { get; set; }
}

public sealed class MonthlyCategorySpendRow
{
    public DateTime MonthStart { get; set; }
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
}

public sealed class DuplicateChargeRow
{
    public string MerchantName { get; set; } = "";
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public int Count { get; set; }
}
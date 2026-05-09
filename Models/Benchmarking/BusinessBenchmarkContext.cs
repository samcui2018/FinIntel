namespace FinancialIntelligence.Api.Models;

public sealed class BusinessBenchmarkContext
{
    public Guid BusinessId { get; init; }
    public string? Industry { get; init; }
    public decimal? AverageMonthlySpend { get; init; }
    public int? AverageMonthlyTransactionCount { get; init; }
    public string Geography { get; init; } = "US";
}
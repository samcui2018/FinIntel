using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Services.Insights;

namespace FinancialIntelligence.Api.Services.Insights;
public sealed class CashFlowForecastInsightGenerator : IInsightGenerator
{
    private readonly ITransactionQueryRepository _transactionRepository;

    public CashFlowForecastInsightGenerator(ITransactionQueryRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetByLoadIdAsync(
            loadId,
            businessId,
            cancellationToken);

        if (transactions.Count == 0)
        {
            return Array.Empty<InsightRecord>();
        }

        var monthly = transactions
            .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
            .Select(g => new
            {
                Month = g.Key,
                NetCashFlow = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Month)
            .ToList();

        if (monthly.Count < 3)
        {
            return Array.Empty<InsightRecord>();
        }

        var last3 = monthly.TakeLast(3).ToList();
        var average = last3.Average(x => x.NetCashFlow);
        var worseningTrend = last3.Count == 3 &&
            last3[2].NetCashFlow < last3[1].NetCashFlow &&
            last3[1].NetCashFlow < last3[0].NetCashFlow;

        var severity =
            average < 0 && worseningTrend ? "High" :
            average < 0 ? "Medium" :
            "Low";

        var title =
            average < 0
                ? "Negative cash flow trend detected"
                : "Positive cash flow trend detected";

        var recommendation =
            average < 0
                ? "Review expense reductions, pricing, or collections to improve near-term cash flow."
                : "Maintain current trajectory and monitor for changes.";

        return new[]
        {
            new InsightRecord
            {
                InsightId = Guid.NewGuid(),
                LoadId = loadId,
                BusinessId = businessId,
                InsightType = "CashFlowForecast",
                Severity = severity,
                Title = title,
                Description = $"Average monthly net cash flow over the last 3 months is ${average:N2}.",
                ImpactLabel = "Average Monthly Cash Flow",
                ImpactValue = decimal.Round(average, 2),
                Recommendation = recommendation,
                ConfidenceScore = worseningTrend ? 0.82m : 0.72m,
                CreatedAtUtc = DateTime.UtcNow
            }
        };
    }
}
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public sealed class BenchmarkInsightGenerator : IInsightGenerator
{
    private readonly IBenchmarkService _benchmarkService;

    public BenchmarkInsightGenerator(IBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    public async Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // Console.WriteLine($"benchmarkGenerator called for businessId: {businessId}");
        var benchmark = await _benchmarkService.CompareAsync(
            loadId,
            businessId,
            monthsBack: 6,
            cancellationToken);

        var insights = new List<InsightRecord>();

        if (benchmark.TopVendorConcentrationDeltaPct >= 15m)
        {
            insights.Add(new InsightRecord
            {
                InsightId = Guid.NewGuid(),
                BusinessId = businessId,
                InsightType = "Benchmark",
                Title = "Vendor concentration is higher than benchmark",
                Description = $"Your top vendor concentration is {benchmark.BusinessTopVendorConcentrationPct:0.#}% versus a benchmark of {benchmark.BenchmarkTopVendorConcentrationPct:0.#}%.",
                Severity = benchmark.TopVendorConcentrationDeltaPct >= 25m ? "High" : "Medium",
                ImpactValue = null,
                ImpactLabel = "Benchmark Risk",
                Recommendation = "Review vendor dependency and identify diversification opportunities.",
                ConfidenceScore = 0.80m,
                CurrencyCode = benchmark.CurrencyCode,
                MetricsJson = null,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (benchmark.MonthlySpendDeltaPct >= 20m)
        {
            insights.Add(new InsightRecord
            {
                InsightId = Guid.NewGuid(),
                BusinessId = businessId,
                InsightType = "Benchmark",
                Title = "Monthly spend is above benchmark",
                Description = $"Your current monthly spend is {benchmark.MonthlySpendDeltaPct:0.#}% above the benchmark for the selected segment.",
                Severity = benchmark.MonthlySpendDeltaPct >= 35m ? "High" : "Medium",
                ImpactValue = benchmark.MonthlySpendDelta,
                ImpactLabel = "Cost Benchmark",
                Recommendation = "Examine the largest spend drivers and confirm whether the higher spend supports planned growth or avoidable cost.",
                ConfidenceScore = 0.78m,
                CurrencyCode = benchmark.CurrencyCode,
                MetricsJson = null,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return insights;
    }
}
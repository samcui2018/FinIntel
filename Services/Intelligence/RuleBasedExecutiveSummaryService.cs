using System.Text;
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Dtos.Intelligence;

namespace FinancialIntelligence.Api.Services.Intelligence;

public sealed class RuleBasedExecutiveSummaryService : IExecutiveSummaryService
{
    public Task<string> GenerateAsync(
        Guid businessId,
        IReadOnlyList<InsightDto> insights,
        BenchmarkComparisonDto benchmark,
        SpendForecastDto forecast,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        if (insights.Count == 0)
        {
            sb.Append("There are not enough signals yet to produce a strong executive summary. ");
        }
        else
        {
            var top = insights
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.EstimatedImpact ?? 0m)
                .Take(2)
                .ToList();

            sb.Append("Recent transaction patterns show ");
            sb.Append(string.Join(" and ", top.Select(x => x.Title.ToLowerInvariant())));
            sb.Append(". ");
        }

        if (benchmark.MonthlySpendDeltaPct > 15m)
        {
            sb.Append($"Monthly spend is currently {benchmark.MonthlySpendDeltaPct:0.#}% above benchmark. ");
        }
        else if (benchmark.MonthlySpendDeltaPct < -15m)
        {
            sb.Append($"Monthly spend is currently {Math.Abs(benchmark.MonthlySpendDeltaPct):0.#}% below benchmark. ");
        }

        if (benchmark.TopVendorConcentrationDeltaPct > 10m)
        {
            sb.Append("Vendor concentration is higher than benchmark, which may increase dependency risk. ");
        }

        if (forecast.HasSufficientHistory)
        {
            if (forecast.TrendSlope > 0m)
            {
                sb.Append($"Forecasting suggests next month's spend may rise to about {forecast.NextMonthForecast:C}. ");
            }
            else if (forecast.TrendSlope < 0m)
            {
                sb.Append($"Forecasting suggests next month's spend may ease to about {forecast.NextMonthForecast:C}. ");
            }
        }

        sb.Append("Focus first on the highest-impact spend drivers and confirm whether current trends are expected or actionable.");

        return Task.FromResult(sb.ToString().Trim());
    }
}
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services.Intelligence;

public sealed class SpendAnomalyInsightService : IInsightContributor
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public SpendAnomalyInsightService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var monthlySpend = await _analyticsRepository.GetMonthlySpendAsync(
            businessId,
            monthsBack,
            cancellationToken);

        if (monthlySpend is null || monthlySpend.Count < 2)
        {
            return Array.Empty<InsightDto>();
        }

        var ordered = monthlySpend
            .OrderBy(x => x.MonthStart)
            .ToList();

        var latest = ordered[^1];
        var previous = ordered[^2];

        if (previous.Amount <= 0)
        {
            return Array.Empty<InsightDto>();
        }

        var changePct = (latest.Amount - previous.Amount) / previous.Amount;
        if (Math.Abs(changePct) < 0.20m)
        {
            return Array.Empty<InsightDto>();
        }

        var estimatedImpact = Math.Abs(latest.Amount - previous.Amount);
        var severity =
            Math.Abs(changePct) >= 0.50m ? "High" :
            Math.Abs(changePct) >= 0.30m ? "Medium" :
            "Low";

        var confidence =
            ordered.Count >= 4 ? 0.80m :
            ordered.Count >= 3 ? 0.70m :
            0.60m;

        return new[]
        {
            new InsightDto
            {
                Type = "spend_anomaly",
                Title = changePct > 0
                    ? "Significant spend increase detected"
                    : "Significant spend decrease detected",
                Description =
                    $"Latest monthly spend changed by {changePct:P0} versus the prior month " +
                    $"({previous.Amount:C0} to {latest.Amount:C0}).",
                Severity = severity,
                EstimatedImpact = decimal.Round(estimatedImpact, 2),
                Recommendation = changePct > 0
                    ? "Review recent merchants and categories to confirm whether the increase is expected."
                    : "Review the drop in spending to confirm whether it reflects seasonality, timing shifts, or reduced activity.",
                Score = Score(estimatedImpact, confidence, severity),
                Metrics = new Dictionary<string, object>
                {
                    ["previousMonthAmount"] = previous.Amount,
                    ["latestMonthAmount"] = latest.Amount,
                    ["changePct"] = changePct,
                    ["confidence"] = confidence
                }
            }
        };
    }

    private static decimal Score(decimal estimatedImpact, decimal confidence, string severity)
    {
        var severityWeight = severity switch
        {
            "High" => 1.20m,
            "Medium" => 1.00m,
            _ => 0.85m
        };

        var normalizedImpact = Math.Min(estimatedImpact / 1000m, 10m);
        return decimal.Round((normalizedImpact * 10m) * confidence * severityWeight, 2);
    }
}
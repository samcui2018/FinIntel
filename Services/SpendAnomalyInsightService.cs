using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services.Intelligence;

public sealed class SpendAnomalyInsightService : IInsightAnalyzer
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

        var labels = ordered
            .Select(x => x.MonthStart.ToString("yyyy-MM"))
            .ToList();

        var spendValues = ordered
            .Select(x => x.Amount)
            .ToList();

        var highlightIndex = ordered.Count - 1;

        var insight = new InsightDto
        {
            BusinessId = businessId,
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
            },
            VisualizationType = "line",
            Visualization = InsightVisualizationFactory.CreateLineChart(
                title: "Monthly spend trend",
                labels: labels,
                highlightIndexes: new[] { highlightIndex },
                new InsightVisualizationSeriesDto
                {
                    Name = "Spend",
                    Values = spendValues
                })
        };

        return new[] { insight };
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

    private static InsightDto? TryBuildSpendAnomalyInsight(
    IReadOnlyList<MonthlySpendDto> monthlySpend)
    {
        if (monthlySpend is null || monthlySpend.Count < 4)
        {
            return null;
        }

        var ordered = monthlySpend
            .OrderBy(x => x.MonthStart)
            .ToList();

        var avg = ordered.Average(x => x.Amount);
        if (avg <= 0)
        {
            return null;
        }

        var latest = ordered[^1];
        var pctAboveAverage = ((latest.Amount - avg) / avg) * 100m;

        // if (pctAboveAverage < 25m)
        // {
        //     return null;
        // }

        var estimatedImpact = latest.Amount - avg;

        return BuildSpendAnomalyInsight(
            monthlySpend: ordered,
            anomalyMonth: latest,
            estimatedImpact: Math.Round(estimatedImpact, 2),
            score: pctAboveAverage >= 50m ? 90m : 78m);
    }
    // inside AnalyticsService or SpendAnomalyInsightService
    private static InsightDto BuildSpendAnomalyInsight(
        IReadOnlyList<MonthlySpendDto> monthlySpend,
        MonthlySpendDto anomalyMonth,
        decimal estimatedImpact,
        decimal score = 88m)
    {
        var ordered = monthlySpend
            .OrderBy(x => x.MonthStart)
            .ToList();

        var labels = ordered.Select(x => x.MonthStart.ToString("yyyy-MM")).ToList();
        var spendValues = ordered.Select(x => x.Amount).ToList();

        var highlightIndex = ordered.FindIndex(x => x.MonthStart == anomalyMonth.MonthStart);

        return new InsightDto
        {
            Type = "Spend_Anomaly",
            Title = "Unusual spending spike detected",
            Description = $"Spending in {anomalyMonth.MonthStart:yyyy-MM} was unusually high compared with the recent pattern.",
            Severity = estimatedImpact >= 1000m ? "High" : "Medium",
            Score = score,
            EstimatedImpact = estimatedImpact,
            VisualizationType = "line",
            Visualization = InsightVisualizationFactory.CreateLineChart(
                title: "Monthly spend trend",
                labels: labels,
                highlightIndexes: highlightIndex >= 0 ? new[] { highlightIndex } : Array.Empty<int>(),
                new InsightVisualizationSeriesDto
                {
                    Name = "Spend",
                    Values = spendValues
                })
        };
    }
}
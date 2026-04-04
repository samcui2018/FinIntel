using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services.Insights;

public sealed class InsightRanker : IInsightRanker
{
    public IReadOnlyList<RankedInsight> Rank(IReadOnlyList<InsightRecord> insights)
    {
        if (insights is null || insights.Count == 0)
        {
            return Array.Empty<RankedInsight>();
        }

        var ranked = insights
            .Select(insight =>
            {
                var severityComponent = GetSeverityComponent(insight.Severity);
                var impactComponent = GetImpactComponent(insight.ImpactValue);
                var confidenceComponent = GetConfidenceComponent(insight.ConfidenceScore);

                return new RankedInsight
                {
                    Insight = insight,
                    SeverityComponent = severityComponent,
                    ImpactComponent = impactComponent,
                    ConfidenceComponent = confidenceComponent,
                    PriorityScore = severityComponent + impactComponent + confidenceComponent
                };
            })
            .OrderByDescending(x => x.PriorityScore)
            .ThenByDescending(x => Math.Abs(x.Insight.ImpactValue ?? 0m))
            .ThenByDescending(x => x.Insight.CreatedAtUtc)
            .ThenBy(x => x.Insight.Title)
            .ToList();

        return ranked;
    }

    private static decimal GetSeverityComponent(string? severity)
    {
        var weight = severity?.Trim().ToLowerInvariant() switch
        {
            "high" => 3m,
            "medium" => 2m,
            "low" => 1m,
            _ => 1m
        };

        return weight * 100m;
    }

    private static decimal GetImpactComponent(decimal? impactValue)
    {
        var value = Math.Abs(impactValue ?? 0m);

        var bucket = value switch
        {
            >= 50000m => 5m,
            >= 10000m => 4m,
            >= 2500m => 3m,
            >= 500m => 2m,
            > 0m => 1m,
            _ => 0m
        };

        return bucket * 10m;
    }

    private static decimal GetConfidenceComponent(decimal? confidenceScore)
    {
        var value = confidenceScore ?? 0m;

        if (value < 0m)
        {
            value = 0m;
        }
        else if (value > 1m)
        {
            value = 1m;
        }

        return value * 10m;
    }
}
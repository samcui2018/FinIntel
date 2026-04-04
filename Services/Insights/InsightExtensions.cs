using FinancialIntelligence.Api.Models;
namespace FinancialIntelligence.Api.Services.Insights;

public static class InsightRankingExtensions
{
    public static IReadOnlyList<RankedInsight> Top(
        this IReadOnlyList<RankedInsight> rankedInsights,
        int count)
    {
        if (rankedInsights.Count == 0 || count <= 0)
        {
            return Array.Empty<RankedInsight>();
        }

        return rankedInsights.Take(count).ToList();
    }
}
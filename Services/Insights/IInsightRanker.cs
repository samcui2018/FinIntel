using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services.Insights;

public interface IInsightRanker
{
    IReadOnlyList<RankedInsight> Rank(IReadOnlyList<InsightRecord> insights);
}

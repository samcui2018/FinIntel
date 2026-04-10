using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public interface IInsightRanker
{
    IReadOnlyList<RankedInsight> Rank(IReadOnlyList<InsightRecord> insights);
}

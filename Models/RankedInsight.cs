
namespace FinancialIntelligence.Api.Models;

public sealed class RankedInsight
{
    public required InsightRecord Insight { get; init; }

    public decimal SeverityComponent { get; init; }
    public decimal ImpactComponent { get; init; }
    public decimal ConfidenceComponent { get; init; }

    public decimal PriorityScore { get; init; }
}
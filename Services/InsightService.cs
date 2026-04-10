using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services;

public sealed class InsightService : IInsightService
{
    private readonly IEnumerable<IInsightAnalyzer> _contributors;
    private readonly IInsightRanker _ranker;

    public InsightService(
        IEnumerable<IInsightAnalyzer> contributors,
        IInsightRanker ranker)
    {
        _contributors = contributors;
        _ranker = ranker;
    }

    public async Task<IReadOnlyList<InsightDto>> GetInsightsAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var tasks = _contributors
            .Select(c => c.AnalyzeAsync(businessId, monthsBack, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks);

        var insights = tasks
            .SelectMany(t => t.Result)
            .ToList();

        return insights
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.EstimatedImpact ?? 0m)
            .ToList();
    }
}
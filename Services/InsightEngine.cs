using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services;
public class InsightEngine : IInsightEngine
{
    private readonly IEnumerable<IInsightGenerator> _generators;
    private readonly IInsightRepository _insightRepository;

    public InsightEngine(
        IEnumerable<IInsightGenerator> generators,
        IInsightRepository insightRepository)
    {
        _generators = generators;
        _insightRepository = insightRepository;
    }

    public async Task<IReadOnlyList<InsightRecord>> RunAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var allInsights = new List<InsightRecord>();

        foreach (var generator in _generators)
        {
            var generated = await generator.GenerateAsync(loadId, businessId, cancellationToken);
            allInsights.AddRange(generated);
        }

        if (allInsights.Count > 0)
        {
            await _insightRepository.BuildInsightsAsync(allInsights, cancellationToken);
        }

        return allInsights;
    }
}
using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services;

public sealed class PythonInsightService : IPythonInsightService
{
    private readonly IEnumerable<IInsightAnalyzer> _contributors;
    private readonly IInsightRanker _ranker;

    public PythonInsightService(
        IEnumerable<IInsightAnalyzer> contributors,
        IInsightRanker ranker)
    {
        _contributors = contributors;
        _ranker = ranker;
    }

    public async Task<IReadOnlyList<InsightDto>> GetPythonInsightsAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var tasks = _contributors
            .Select(c => c.AnalyzeAsync("SpendAnomaly", businessId, monthsBack, cancellationToken))
            .ToList();

        // var tasks = new[]
        // {
        //     _spendAnomalyContributor.AnalyzeAsync("SpendAnomaly", businessId, monthsBack, cancellationToken),
        //     _duplicateChargeContributor.AnalyzeAsync("DuplicateCharge", businessId, monthsBack, cancellationToken),
        //     _vendorConcentrationContributor.AnalyzeAsync("VendorConcentration", businessId, monthsBack, cancellationToken)
        // };

        var results = await Task.WhenAll(tasks);

        var insights = results
        .SelectMany(r => r)
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.EstimatedImpact ?? 0m)
        .ToList();

        return insights;

        // var insights = tasks
        //     .SelectMany(t => t.Result)
        //     .ToList();

        // return insights
        //     .OrderByDescending(x => x.Score)
        //     .ThenByDescending(x => x.EstimatedImpact ?? 0m)
        //     .ToList();
    }
}
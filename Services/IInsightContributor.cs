using FinancialIntelligence.Api.Dtos.Analytics;

public interface IInsightContributor
{
    Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
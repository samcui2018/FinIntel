using FinancialIntelligence.Api.Dtos.Analytics;
namespace FinancialIntelligence.Api.Services;
public interface IInsightAnalyzer
{
    Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
        string scriptKey,
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
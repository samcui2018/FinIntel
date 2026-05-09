using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services;

public interface IPythonInsightService
{
    Task<IReadOnlyList<InsightDto>> GetPythonInsightsAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
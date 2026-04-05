using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services.Intelligence;

public interface ISpendAnomalyInsightService
{
    Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
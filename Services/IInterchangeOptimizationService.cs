using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services.Intelligence;

public interface IInterchangeOptimizationService
{
    Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
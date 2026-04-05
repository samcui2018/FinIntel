using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Services.Intelligence;

public interface IInsightService
{
    Task<IReadOnlyList<InsightDto>> GetInsightsAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
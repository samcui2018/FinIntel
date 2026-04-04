using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Dtos.Intelligence;

namespace FinancialIntelligence.Api.Services.Intelligence;

public interface IExecutiveSummaryService
{
    Task<string> GenerateAsync(
        Guid businessId,
        IReadOnlyList<InsightDto> insights,
        BenchmarkComparisonDto benchmark,
        SpendForecastDto forecast,
        CancellationToken cancellationToken = default);
}
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Dtos.Intelligence;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public interface IExecutiveSummaryService
{
    Task<string> GenerateAsync(
        Guid businessId,
        IReadOnlyList<InsightDto> insights,
        // BenchmarkComparisonDto benchmark,
        SpendForecastDto forecast,
        CancellationToken cancellationToken = default);
}
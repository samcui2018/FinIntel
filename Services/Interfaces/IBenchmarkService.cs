using FinancialIntelligence.Api.Dtos.Intelligence;

namespace FinancialIntelligence.Api.Services;

public interface IBenchmarkService
{
    Task<BenchmarkComparisonDto> CompareAsync(
        Guid loadId,
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
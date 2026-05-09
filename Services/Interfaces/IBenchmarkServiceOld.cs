using FinancialIntelligence.Api.Dtos.Intelligence;

namespace FinancialIntelligence.Api.Services;

public interface IBenchmarkServiceOld
{
    Task<BenchmarkComparisonDto> CompareAsync(
        Guid loadId,
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
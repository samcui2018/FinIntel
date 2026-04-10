using FinancialIntelligence.Api.Dtos.Intelligence;

namespace FinancialIntelligence.Api.Services;

public interface IIntelligenceService
{
    Task<IntelligenceResponse> GetIntelligenceAsync(
        Guid loadId,
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default);
}
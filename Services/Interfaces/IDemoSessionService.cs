using FinancialIntelligence.Api.Dtos.Auth;

namespace FinancialIntelligence.Api.Services;

public interface IDemoSessionService
{
    Task<DemoStartResponseDto> StartDemoAsync(CancellationToken cancellationToken = default);
}
using FinancialIntelligence.Api.Dtos.Auth;

namespace FinancialIntelligence.Api.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
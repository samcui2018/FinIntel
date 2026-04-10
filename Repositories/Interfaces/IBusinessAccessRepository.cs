using FinancialIntelligence.Api.Dtos.Auth;

namespace FinancialIntelligence.Api.Repositories;

public interface IBusinessAccessRepository
{
    Task<List<UserBusinessDto>> GetBusinessesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UserHasAccessToBusinessAsync(Guid userId, Guid businessId, CancellationToken cancellationToken = default);
    Task SetDefaultBusinessAsync(Guid userId, Guid businessId, CancellationToken cancellationToken = default);
}
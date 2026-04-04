using FinancialIntelligence.Api.Dtos.Businesses;

namespace FinancialIntelligence.Api.Services;

public interface IBusinessService
{
    Task<IReadOnlyList<BusinessResponse>> GetBusinessesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<BusinessDetailResponse?> GetBusinessForUserAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateBusinessAsync(
        Guid userId,
        CreateBusinessRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateBusinessAsync(
        Guid userId,
        Guid businessId,
        UpdateBusinessRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<bool> SetDefaultBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);

    Task<bool> UserHasAccessToBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);
}
using FinancialIntelligence.Api.Dtos.Businesses;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services;

public sealed class BusinessService : IBusinessService
{
    private readonly IBusinessRepository _businessRepository;

    public BusinessService(IBusinessRepository businessRepository)
    {
        _businessRepository = businessRepository;
    }

    public Task<IReadOnlyList<BusinessResponse>> GetBusinessesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _businessRepository.GetBusinessesForUserAsync(userId, cancellationToken);

    public Task<BusinessDetailResponse?> GetBusinessForUserAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
        => _businessRepository.GetBusinessForUserAsync(userId, businessId, cancellationToken);

    public Task<Guid> CreateBusinessAsync(
        Guid userId,
        CreateBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName))
        {
            throw new ArgumentException("Business name is required.");
        }

        return _businessRepository.CreateBusinessAsync(userId, request, cancellationToken);
    }

    public Task<bool> UpdateBusinessAsync(
        Guid userId,
        Guid businessId,
        UpdateBusinessRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName))
        {
            throw new ArgumentException("Business name is required.");
        }

        return _businessRepository.UpdateBusinessAsync(userId, businessId, request, cancellationToken);
    }

    public Task<bool> DeleteBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
        => _businessRepository.DeleteBusinessAsync(userId, businessId, cancellationToken);

    public Task<bool> SetDefaultBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
        => _businessRepository.SetDefaultBusinessAsync(userId, businessId, cancellationToken);

    public Task<bool> UserHasAccessToBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
        => _businessRepository.UserHasAccessToBusinessAsync(userId, businessId, cancellationToken);
}
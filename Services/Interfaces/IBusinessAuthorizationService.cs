namespace FinancialIntelligence.Api.Services;

public interface IBusinessAuthorizationService
{
    Task<bool> UserHasAccessToBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default);
}
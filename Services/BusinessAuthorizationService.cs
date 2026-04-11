using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services;

public sealed class BusinessAuthorizationService : IBusinessAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly IBusinessAccessRepository _businessAccessRepository;
    private readonly ILogger<BusinessAuthorizationService> _logger;

    public BusinessAuthorizationService(
        IUserRepository userRepository,
        IBusinessAccessRepository businessAccessRepository, 
        ILogger<BusinessAuthorizationService> logger)
    {
        _userRepository = userRepository;
        _businessAccessRepository = businessAccessRepository;
        _logger = logger;
    }

    public async Task<bool> UserHasAccessToBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || businessId == Guid.Empty)
        {
            return false;
        }

        var hasAccess = await _businessAccessRepository.UserHasAccessToBusinessAsync(
            userId,
            businessId,
            cancellationToken);

        if (!hasAccess)
        {
            _logger.LogWarning(
                "Access denied. User {UserId} attempted to access business {BusinessId}.",
                userId,
                businessId);
        }

        return hasAccess;
    }
}
using FinancialIntelligence.Api.Repositories;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace FinancialIntelligence.Api.Services;

public sealed class BusinessAuthorizationService : IBusinessAuthorizationService
{
    //private readonly IUserRepository _userRepository;
    private readonly IBusinessAccessRepository _businessAccessRepository;
    private readonly ILogger<BusinessAuthorizationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BusinessAuthorizationService(
        IUserRepository userRepository,
        IBusinessAccessRepository businessAccessRepository, 
        ILogger<BusinessAuthorizationService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        //_userRepository = userRepository;
        _businessAccessRepository = businessAccessRepository;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> UserHasAccessToBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var isDemo = string.Equals(
            user.FindFirstValue("is_demo"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (isDemo)
        {
            var tokenBusinessIdRaw = user.FindFirstValue("business_id");

            return Guid.TryParse(tokenBusinessIdRaw, out var tokenBusinessId)
                && tokenBusinessId == businessId;
        }

        var userIdRaw =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdRaw, out var userIdFromToekn))
            return false;

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
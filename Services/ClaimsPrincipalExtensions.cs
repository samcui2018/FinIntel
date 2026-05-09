using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace FinancialIntelligence.Api.Services;

//not used for now
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var raw =
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(raw, out var value) ? value : null;
    }

    public static Guid? GetBusinessId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("business_id");
        return Guid.TryParse(raw, out var value) ? value : null;
    }

    public static bool IsDemoUser(this ClaimsPrincipal user)
    {
        return string.Equals(
            user.FindFirstValue("is_demo"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
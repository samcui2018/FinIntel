// using System.Security.Claims;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;

// [ApiController]
// [Route("api/[controller]")]
// public class MeController : ControllerBase
// {
//     [HttpGet]
//     [Authorize]
//     public IActionResult GetMe()
//     {
//         return Ok(new
//         {
//             UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
//             Email = User.FindFirstValue(ClaimTypes.Email),
//             Role = User.FindFirstValue(ClaimTypes.Role)
//         });
//     }
// }
using System.Security.Claims;
using FinancialIntelligence.Api.Dtos.Auth;
using FinancialIntelligence.Api.Dtos.Business;
using FinancialIntelligence.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialIntelligence.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IBusinessAccessRepository _businessAccessRepository;

    public MeController(IBusinessAccessRepository businessAccessRepository)
    {
        _businessAccessRepository = businessAccessRepository;
    }

    [HttpGet("businesses")]
    public async Task<ActionResult<List<UserBusinessDto>>> GetBusinesses(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var businesses = await _businessAccessRepository.GetBusinessesForUserAsync(userId, cancellationToken);
        return Ok(businesses);
    }

    [HttpPost("current-business")]
    public async Task<ActionResult> SetCurrentBusiness(
        [FromBody] SetCurrentBusinessRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var hasAccess = await _businessAccessRepository.UserHasAccessToBusinessAsync(userId, request.BusinessId, cancellationToken);
        if (!hasAccess)
            return Forbid();

        await _businessAccessRepository.SetDefaultBusinessAsync(userId, request.BusinessId, cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var userId))
            throw new UnauthorizedAccessException("User ID claim missing or invalid.");

        return userId;
    }
}
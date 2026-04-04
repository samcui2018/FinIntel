using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinancialIntelligence.Api.Dtos.Businesses;
using FinancialIntelligence.Api.Services;

namespace FinancialIntelligence.Api.Controllers;

[ApiController]
[Route("api/businesses")]
[Authorize]
public sealed class BusinessesController : ControllerBase
{
    private readonly IBusinessService _businessService;

    public BusinessesController(IBusinessService businessService)
    {
        _businessService = businessService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusinessResponse>>> GetBusinesses(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        var businesses = await _businessService.GetBusinessesForUserAsync(userId, cancellationToken);
        return Ok(businesses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BusinessDetailResponse>> GetBusiness(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        var business = await _businessService.GetBusinessForUserAsync(userId, id, cancellationToken);

        if (business is null)
        {
            return NotFound();
        }

        return Ok(business);
    }

    [HttpPost]
    public async Task<ActionResult> CreateBusiness(
        [FromBody] CreateBusinessRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var businessId = await _businessService.CreateBusinessAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetBusiness), new { id = businessId }, new { businessId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateBusiness(
        Guid id,
        [FromBody] UpdateBusinessRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var updated = await _businessService.UpdateBusinessAsync(userId, id, request, cancellationToken);

            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteBusiness(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        var deleted = await _businessService.DeleteBusinessAsync(userId, id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult> SetDefaultBusiness(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        var updated = await _businessService.SetDefaultBusinessAsync(userId, id, cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out userId);
    }
}
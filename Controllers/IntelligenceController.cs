using FinancialIntelligence.Api.Dtos.Intelligence;
using FinancialIntelligence.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialIntelligence.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}/intelligence")]
[Authorize]
public sealed class IntelligenceController : ControllerBase
{
    private readonly IIntelligenceService _intelligenceService;

    public IntelligenceController(IIntelligenceService intelligenceService)
    {
        _intelligenceService = intelligenceService;
    }

    [HttpGet]
    public async Task<ActionResult<IntelligenceResponse>> Get(
        Guid businessId,
        [FromQuery] int monthsBack = 6,
        CancellationToken cancellationToken = default)
    {
        var result = await _intelligenceService.GetIntelligenceAsync(
            Guid.NewGuid(),
            businessId,
            monthsBack,
            cancellationToken);

        return Ok(result);
    }
}
using System.Security.Claims;
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialIntelligence.Api.Controllers;
[ApiController]
[Route("api/businesses/{businessId:guid}/analytics")]
[Authorize]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IBusinessService _businessService;
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(
        IBusinessService businessService,
        IAnalyticsService analyticsService)
    {
        _businessService = businessService;
        _analyticsService = analyticsService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult> GetSummary(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        var hasAccess = await _businessService.UserHasAccessToBusinessAsync(
            userId,
            businessId,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var result = await _analyticsService.GetSummaryAsync(
            userId,
            businessId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("monthly-trend")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthlyTrendPointResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonthlyTrend(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized("User ID claim is missing or invalid.");

        if (businessId == Guid.Empty)
            return BadRequest("businessId is required.");

        var result = await _analyticsService.GetMonthlyTrendAsync(userId, businessId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-merchants")]
    [ProducesResponseType(typeof(IReadOnlyList<TopMerchantResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopMerchants(
        Guid businessId,
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized("User ID claim is missing or invalid.");

        if (businessId == Guid.Empty)
            return BadRequest("businessId is required.");

        if (top <= 0)
            return BadRequest("top must be greater than zero.");

        var result = await _analyticsService.GetTopMerchantsAsync(userId, businessId, top, cancellationToken);
        return Ok(result);
    }

    [HttpGet("upload-history")]
    [ProducesResponseType(typeof(IReadOnlyList<UploadHistoryItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUploadHistory(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized("User ID claim is missing or invalid.");

        var result = await _analyticsService.GetUploadHistoryAsync(userId, cancellationToken);
        return Ok(result);
    }
     [HttpGet("top-insights")]
    public async Task<ActionResult<TopInsightsResponse>> GetTopInsights(
        Guid businessId,
        int monthsBack = 6,
        CancellationToken cancellationToken = default)
    {
        if (businessId == Guid.Empty)
            return BadRequest("businessId is required.");

        if (monthsBack < 2 || monthsBack > 24)
            return BadRequest("monthsBack must be between 2 and 24.");

        var result = await _analyticsService.GetTopInsightsAsync(
            businessId,
            monthsBack,
            cancellationToken);

        return Ok(result);
    }
    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out userId);
    }
}
using FinancialIntelligence.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FinancialIntelligence.Api.Controllers;

[ApiController]
[Route("api/insights")]
public class InsightsController : ControllerBase
{
    private readonly IInsightRepository _insightRepository;

    public InsightsController(IInsightRepository insightRepository)
    {
        _insightRepository = insightRepository;
    }

    [HttpGet("business/{businessId:guid}")]
    public async Task<IActionResult> GetByBusinessId(Guid businessId, CancellationToken cancellationToken)
    {
        var insights = await _insightRepository.GetByBusinessIdAsync(businessId, cancellationToken);
        return Ok(insights);
    }

    [HttpGet("load/{loadId:guid}")]
    public async Task<IActionResult> GetByLoadId(Guid loadId, CancellationToken cancellationToken)
    {
        var insights = await _insightRepository.GetByLoadIdAsync(loadId, cancellationToken);
        return Ok(insights);
    }
}
using FinancialIntelligence.Api.Dtos.Auth;
using FinancialIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FinancialIntelligence.Api.Controllers;

[ApiController]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    private readonly IDemoSessionService _demoSessionService;

    public DemoController(IDemoSessionService demoSessionService)
    {
        _demoSessionService = demoSessionService;
    }

    [HttpPost("start")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DemoStartResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DemoStartResponseDto>> Start(CancellationToken cancellationToken)
    {
        var response = await _demoSessionService.StartDemoAsync(cancellationToken);
        return Ok(response);
    }
}
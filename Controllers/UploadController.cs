using FinancialIntelligence.Api.Dtos.Upload;
using FinancialIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FinancialIntelligence.Api.Controllers;

[ApiController]
[Route("api/businesses/{businessId:guid}/uploads")]
[Authorize]
public sealed class UploadsController : ControllerBase
{
    private readonly ITransactionUploadService _uploadService;
    private readonly IBusinessService _businessService;

    public UploadsController(
        ITransactionUploadService uploadService,
        IBusinessService businessService)
    {
        _uploadService = uploadService;
        _businessService = businessService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadTransactionsResponse>> Upload(
        Guid businessId, // now comes from route
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest("A CSV or Excel file is required.");
        }

        if (businessId == Guid.Empty)
        {
            return BadRequest("BusinessId is required.");
        }

        var hasAccess = await _businessService.UserHasAccessToBusinessAsync(
            userId,
            businessId,
            cancellationToken);

        if (!hasAccess)
        {
            return Forbid();
        }

        var response = await _uploadService.UploadFileAsync(
            file,
            businessId,
            userId,
            cancellationToken);

        return Ok(response);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out userId);
    }
}


// using FinancialIntelligence.Api.Dtos.Upload;
// using FinancialIntelligence.Api.Services;
// using Microsoft.AspNetCore.Mvc;
// using System.Security.Claims;
// using Microsoft.AspNetCore.Authorization;

// namespace FinancialIntelligence.Api.Controllers;

// [ApiController]
// [Route("api/uploads")]
// [Authorize]
// public sealed class UploadsController : ControllerBase
// {
//     private readonly ITransactionUploadService _uploadService;
//     private readonly IBusinessService _businessService;

//     public UploadsController(
//         ITransactionUploadService uploadService,
//         IBusinessService businessService)
//     {
//         _uploadService = uploadService;
//         _businessService = businessService;
//     }

//     [HttpPost("csv")]
//     [Consumes("multipart/form-data")]
//     public async Task<ActionResult<UploadTransactionsResponse>> UploadCsv(
//         IFormFile file,
//         [FromForm] Guid businessId,
//         CancellationToken cancellationToken)
//     {
//         if (!TryGetUserId(out var userId))
//         {
//             return Unauthorized("User ID claim is missing or invalid.");
//         }

//         if (file is null || file.Length == 0)
//         {
//             return BadRequest("A CSV file is required.");
//         }

//         if (businessId == Guid.Empty)
//         {
//             return BadRequest("BusinessId is required.");
//         }

//         var hasAccess = await _businessService.UserHasAccessToBusinessAsync(
//             userId,
//             businessId,
//             cancellationToken);

//         if (!hasAccess)
//         {
//             return Forbid();
//         }

//         var response = await _uploadService.UploadCsvAsync(
//             file,
//             businessId,
//             userId,
//             cancellationToken);

//         return Ok(response);
//     }

//     private bool TryGetUserId(out Guid userId)
//     {
//         userId = Guid.Empty;

//         var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
//             ?? User.FindFirst("sub")?.Value;

//         return Guid.TryParse(userIdClaim, out userId);
//     }
// }

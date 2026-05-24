using Application.Features.AuditLogManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class AuditLogController : BaseApiController
{
    public AuditLogController(ISender sender) : base(sender) { }

    [Authorize]
    [HttpGet("GetAuditLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetAuditLogListResult>>> GetAuditLogListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? entityType = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var request = new GetAuditLogListRequest
        {
            EntityType = entityType,
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate
        };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetAuditLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetAuditLogListAsync)}",
            Content = response
        });
    }
}

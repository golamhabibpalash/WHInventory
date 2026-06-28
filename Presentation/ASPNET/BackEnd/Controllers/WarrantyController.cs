using Application.Features.WarrantyManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class WarrantyController : BaseApiController
{
    public WarrantyController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpGet("GetWarrantyCheck")]
    public async Task<ActionResult<ApiSuccessResult<GetWarrantyCheckResult>>> GetWarrantyCheckAsync(
        [FromQuery] string? customerId,
        [FromQuery] string? productId,
        [FromQuery] DateTime? claimDate,
        CancellationToken cancellationToken)
    {
        var request = new GetWarrantyCheckRequest
        {
            CustomerId = customerId,
            ProductId = productId,
            ClaimDate = claimDate
        };

        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetWarrantyCheckResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetWarrantyCheckAsync)}",
            Content = response
        });
    }
}

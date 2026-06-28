using Application.Features.PricePolicyManager.Commands;
using Application.Features.PricePolicyManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PricePolicyController : BaseApiController
{
    public PricePolicyController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePricePolicy")]
    public async Task<ActionResult<ApiSuccessResult<CreatePricePolicyResult>>> CreatePricePolicyAsync(CreatePricePolicyRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePricePolicyResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePricePolicyAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePricePolicy")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePricePolicyResult>>> UpdatePricePolicyAsync(UpdatePricePolicyRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePricePolicyResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePricePolicyAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePricePolicy")]
    public async Task<ActionResult<ApiSuccessResult<DeletePricePolicyResult>>> DeletePricePolicyAsync(DeletePricePolicyRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePricePolicyResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePricePolicyAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPricePolicyList")]
    public async Task<ActionResult<ApiSuccessResult<GetPricePolicyListResult>>> GetPricePolicyListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var request = new GetPricePolicyListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPricePolicyListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPricePolicyListAsync)}",
            Content = response
        });
    }
}

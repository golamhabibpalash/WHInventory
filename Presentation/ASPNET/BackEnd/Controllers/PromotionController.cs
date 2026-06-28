using Application.Features.PromotionManager.Commands;
using Application.Features.PromotionManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PromotionController : BaseApiController
{
    public PromotionController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePromotion")]
    public async Task<ActionResult<ApiSuccessResult<CreatePromotionResult>>> CreatePromotionAsync(CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePromotionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePromotionAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePromotion")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePromotionResult>>> UpdatePromotionAsync(UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePromotionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePromotionAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePromotion")]
    public async Task<ActionResult<ApiSuccessResult<DeletePromotionResult>>> DeletePromotionAsync(DeletePromotionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePromotionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePromotionAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPromotionList")]
    public async Task<ActionResult<ApiSuccessResult<GetPromotionListResult>>> GetPromotionListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] string? productId = null)
    {
        var request = new GetPromotionListRequest { IsDeleted = isDeleted, ProductId = productId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPromotionListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPromotionListAsync)}",
            Content = response
        });
    }
}

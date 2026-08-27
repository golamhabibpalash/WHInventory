using Application.Features.PaymentManager.Commands;
using Application.Features.PaymentManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PaymentController : BaseApiController
{
    public PaymentController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePayment")]
    public async Task<ActionResult<ApiSuccessResult<CreatePaymentResult>>> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePaymentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePaymentAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePayment")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePaymentResult>>> UpdatePaymentAsync(UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePaymentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePaymentAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePayment")]
    public async Task<ActionResult<ApiSuccessResult<DeletePaymentResult>>> DeletePaymentAsync(DeletePaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePaymentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePaymentAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPaymentListByModule")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentListByModuleResult>>> GetPaymentListByModuleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? moduleName,
        [FromQuery] string? moduleId)
    {
        var request = new GetPaymentListByModuleRequest { ModuleName = moduleName, ModuleId = moduleId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentListByModuleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentListByModuleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPaymentSummary")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentSummaryResult>>> GetPaymentSummaryAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? moduleName,
        [FromQuery] string? moduleId)
    {
        var request = new GetPaymentSummaryRequest { ModuleName = moduleName, ModuleId = moduleId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentSummaryResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentSummaryAsync)}",
            Content = response
        });
    }
}

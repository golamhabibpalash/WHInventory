using Application.Features.PriceHistoryManager.Queries;
using Application.Features.PriceManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PriceController : BaseApiController
{
    public PriceController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpGet("ResolvePrice")]
    public async Task<ActionResult<ApiSuccessResult<ResolvePriceForProductResult>>> ResolvePriceAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? productId = null,
        [FromQuery] string? customerId = null,
        [FromQuery] double quantity = 1,
        [FromQuery] DateTime? saleDate = null)
    {
        var request = new ResolvePriceForProductRequest
        {
            ProductId = productId,
            CustomerId = customerId,
            Quantity = quantity,
            SaleDate = saleDate
        };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ResolvePriceForProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ResolvePriceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPriceReport")]
    public async Task<ActionResult<ApiSuccessResult<GetPriceReportResult>>> GetPriceReportAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? productGroupId = null,
        [FromQuery] string? productId = null)
    {
        var request = new GetPriceReportRequest { ProductGroupId = productGroupId, ProductId = productId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPriceReportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPriceReportAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPriceHistoryList")]
    public async Task<ActionResult<ApiSuccessResult<GetPriceHistoryListResult>>> GetPriceHistoryListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? productPriceId = null,
        [FromQuery] string? productId = null)
    {
        var request = new GetPriceHistoryListRequest { ProductPriceId = productPriceId, ProductId = productId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPriceHistoryListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPriceHistoryListAsync)}",
            Content = response
        });
    }
}

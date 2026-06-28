using Application.Features.ProductPriceManager.Commands;
using Application.Features.ProductPriceManager.Queries;
using Application.Features.QuantityBreakManager.Commands;
using Application.Features.QuantityBreakManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ProductPriceController : BaseApiController
{
    public ProductPriceController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateProductPrice")]
    public async Task<ActionResult<ApiSuccessResult<CreateProductPriceResult>>> CreateProductPriceAsync(CreateProductPriceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateProductPriceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateProductPriceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateProductPrice")]
    public async Task<ActionResult<ApiSuccessResult<UpdateProductPriceResult>>> UpdateProductPriceAsync(UpdateProductPriceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateProductPriceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateProductPriceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteProductPrice")]
    public async Task<ActionResult<ApiSuccessResult<DeleteProductPriceResult>>> DeleteProductPriceAsync(DeleteProductPriceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteProductPriceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteProductPriceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetProductPriceList")]
    public async Task<ActionResult<ApiSuccessResult<GetProductPriceListResult>>> GetProductPriceListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] string? productId = null,
        [FromQuery] string? pricePolicyId = null)
    {
        var request = new GetProductPriceListRequest { IsDeleted = isDeleted, ProductId = productId, PricePolicyId = pricePolicyId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProductPriceListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductPriceListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("CreateQuantityBreak")]
    public async Task<ActionResult<ApiSuccessResult<CreateQuantityBreakResult>>> CreateQuantityBreakAsync(CreateQuantityBreakRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateQuantityBreakResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateQuantityBreakAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateQuantityBreak")]
    public async Task<ActionResult<ApiSuccessResult<UpdateQuantityBreakResult>>> UpdateQuantityBreakAsync(UpdateQuantityBreakRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateQuantityBreakResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateQuantityBreakAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteQuantityBreak")]
    public async Task<ActionResult<ApiSuccessResult<DeleteQuantityBreakResult>>> DeleteQuantityBreakAsync(DeleteQuantityBreakRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteQuantityBreakResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteQuantityBreakAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetQuantityBreakList")]
    public async Task<ActionResult<ApiSuccessResult<GetQuantityBreakListResult>>> GetQuantityBreakListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? productPriceId = null)
    {
        var request = new GetQuantityBreakListRequest { ProductPriceId = productPriceId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetQuantityBreakListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetQuantityBreakListAsync)}",
            Content = response
        });
    }
}

using Application.Features.ProductManager.Commands;
using Application.Features.ProductManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ProductController : BaseApiController
{
    public ProductController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateProduct")]
    public async Task<ActionResult<ApiSuccessResult<CreateProductResult>>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateProduct")]
    public async Task<ActionResult<ApiSuccessResult<UpdateProductResult>>> UpdateProductAsync(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UploadProductImage")]
    public async Task<ActionResult<ApiSuccessResult<UploadProductImageResult>>> UploadProductImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("An image file is required.");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileData = memoryStream.ToArray();
        var extension = Path.GetExtension(file.FileName).TrimStart('.');

        var request = new UploadProductImageRequest
        {
            OriginalFileName = file.FileName,
            Extension = extension,
            Data = fileData,
            Size = fileData.Length
        };

        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UploadProductImageResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UploadProductImageAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteProduct")]
    public async Task<ActionResult<ApiSuccessResult<DeleteProductResult>>> DeleteProductAsync(DeleteProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetProductList")]
    public async Task<ActionResult<ApiSuccessResult<GetProductListResult>>> GetProductListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetProductListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProductListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductListAsync)}",
            Content = response
        });
    }


}



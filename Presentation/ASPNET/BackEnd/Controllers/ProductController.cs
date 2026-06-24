using Application.Features.ProductManager.Commands;
using Application.Features.ProductManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ClosedXML.Excel;
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

    [Authorize]
    [HttpGet("GetProductByBarcode")]
    public async Task<ActionResult<ApiSuccessResult<GetProductByBarcodeResult>>> GetProductByBarcodeAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? barcode = null
        )
    {
        var request = new GetProductByBarcodeRequest { Barcode = barcode };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProductByBarcodeResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductByBarcodeAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpGet("DownloadImportTemplate")]
    public IActionResult DownloadImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell(1, 2).Value = "UnitPrice";
        sheet.Cell(1, 3).Value = "UnitMeasureName";
        sheet.Cell(1, 4).Value = "ProductGroupName";
        sheet.Cell(1, 5).Value = "BrandName";
        sheet.Cell(1, 6).Value = "Barcode";
        sheet.Cell(1, 7).Value = "Description";
        sheet.Cell(1, 8).Value = "IsWarrantyApplicable";

        var headerRange = sheet.Range("A1:H1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "product-import-template.xlsx");
    }

    [Authorize]
    [HttpPost("BulkCreateProduct")]
    public async Task<ActionResult<ApiSuccessResult<BulkCreateProductResult>>> BulkCreateProductAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("An Excel file is required.");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileData = memoryStream.ToArray();

        var request = new BulkCreateProductRequest
        {
            Data = fileData
        };

        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<BulkCreateProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(BulkCreateProductAsync)}",
            Content = response
        });
    }

}



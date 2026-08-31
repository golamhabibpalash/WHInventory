using Application.Features.BrandManager.Commands;
using ClosedXML.Excel;
using Application.Features.BrandManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BrandController : BaseApiController
{
    public BrandController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateBrand")]
    public async Task<ActionResult<ApiSuccessResult<CreateBrandResult>>> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateBrandResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateBrandAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateBrand")]
    public async Task<ActionResult<ApiSuccessResult<UpdateBrandResult>>> UpdateBrandAsync(UpdateBrandRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateBrandResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBrandAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteBrand")]
    public async Task<ActionResult<ApiSuccessResult<DeleteBrandResult>>> DeleteBrandAsync(DeleteBrandRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteBrandResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteBrandAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("ToggleBrandStatus")]
    public async Task<ActionResult<ApiSuccessResult<ToggleBrandStatusResult>>> ToggleBrandStatusAsync(ToggleBrandStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ToggleBrandStatusResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ToggleBrandStatusAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBrandList")]
    public async Task<ActionResult<ApiSuccessResult<GetBrandListResult>>> GetBrandListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] bool? isActive = null
        )
    {
        var request = new GetBrandListRequest { IsDeleted = isDeleted, IsActive = isActive };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBrandListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBrandListAsync)}",
            Content = response
        });
    }

    /// <summary>
    /// Anonymous because the page offers it as a plain link, which cannot carry the bearer
    /// token. The workbook is an empty header row - it exposes no data.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("DownloadImportTemplate")]
    public IActionResult DownloadImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Brands");
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell(1, 2).Value = "Description";
        sheet.Cell(1, 3).Value = "Status";

        var headerRange = sheet.Range("A1:C1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "brand-import-template.xlsx");
    }

    [Authorize]
    [HttpPost("BulkCreateBrand")]
    public async Task<ActionResult<ApiSuccessResult<BulkCreateBrandResult>>> BulkCreateBrandAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("An Excel file is required.");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileData = memoryStream.ToArray();

        var request = new BulkCreateBrandRequest
        {
            Data = fileData
        };

        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<BulkCreateBrandResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(BulkCreateBrandAsync)}",
            Content = response
        });
    }

}

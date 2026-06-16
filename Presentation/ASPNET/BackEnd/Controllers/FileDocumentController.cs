using Application.Features.FileDocumentManager.Commands;
using Application.Features.FileDocumentManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.FileDocumentManager;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class FileDocumentController : BaseApiController
{
    public FileDocumentController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("UploadDocument")]
    public async Task<ActionResult<CreateDocumentResult>> UploadDocumentAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Invalid file.");
        }

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileData = memoryStream.ToArray();
            var extension = Path.GetExtension(file.FileName).TrimStart('.');

            var command = new CreateDocumentRequest
            {
                OriginalFileName = file.FileName,
                Extension = extension,
                Data = fileData,
                Size = fileData.Length
            };

            var result = await _sender.Send(command, cancellationToken);

            if (result?.DocumentName == null)
            {
                return StatusCode(500, "An error occurred while uploading the document.");
            }

            return Ok(new ApiSuccessResult<CreateDocumentResult>
            {
                Code = StatusCodes.Status200OK,
                Message = $"Success executing {nameof(UploadDocumentAsync)}",
                Content = result
            });
        }
    }


    [Authorize]
    [HttpPost("BulkUploadDocuments")]
    public async Task<ActionResult<List<CreateDocumentResult>>> BulkUploadDocumentsAsync(
        [FromForm] List<IFormFile> files,
        [FromForm] string? moduleName,
        [FromForm] string? moduleId,
        CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided.");
        }

        var createdById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var results = new List<CreateDocumentResult>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var extension = Path.GetExtension(file.FileName);
            if (!FileDocumentHelper.AllowedBulkExtensions.Contains(extension))
            {
                return BadRequest($"File type '{extension}' is not allowed. Allowed types: PDF, Word, Excel, PowerPoint.");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileData = memoryStream.ToArray();

            var command = new CreateDocumentRequest
            {
                OriginalFileName = file.FileName,
                Extension = extension.TrimStart('.'),
                Data = fileData,
                Size = fileData.Length,
                CreatedById = createdById,
                ModuleName = moduleName,
                ModuleId = moduleId
            };

            var result = await _sender.Send(command, cancellationToken);
            results.Add(result);
        }

        return Ok(new ApiSuccessResult<List<CreateDocumentResult>>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(BulkUploadDocumentsAsync)}",
            Content = results
        });
    }

    [Authorize]
    [HttpGet("GetDocumentsByModule")]
    public async Task<ActionResult<GetDocumentsByModuleResult>> GetDocumentsByModuleAsync(
        [FromQuery] string moduleName,
        [FromQuery] string moduleId,
        CancellationToken cancellationToken)
    {
        var request = new GetDocumentsByModuleRequest
        {
            ModuleName = moduleName,
            ModuleId = moduleId
        };

        var result = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetDocumentsByModuleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetDocumentsByModuleAsync)}",
            Content = result
        });
    }

    [Authorize]
    [HttpPost("DeleteDocument")]
    public async Task<ActionResult<DeleteDocumentResult>> DeleteDocumentAsync(
        [FromBody] DeleteDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteDocumentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteDocumentAsync)}",
            Content = result
        });
    }

    [Authorize]
    [HttpGet("GetDocument")]
    public async Task<IActionResult> GetDocumentAsync(
        [FromQuery] string documentName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(documentName) || Path.GetExtension(documentName) == string.Empty)
        {
            documentName = "nodocument.txt";
        }

        var request = new GetDocumentRequest
        {
            DocumentName = documentName
        };

        var result = await _sender.Send(request, cancellationToken);

        if (result?.Data == null)
        {
            return NotFound("Document not found.");
        }

        var extension = Path.GetExtension(documentName).ToLower();
        var mimeType = FileDocumentHelper.GetMimeType(extension);

        return File(result.Data, mimeType);
    }


}



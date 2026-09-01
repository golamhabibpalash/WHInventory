using Application.Features.TenantManager.Commands;
using Application.Features.TenantManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

/// <summary>
/// Platform administration: the tenant registry is not tenant-filtered, so every action here is
/// gated on the Tenants role, which provisioning deliberately withholds from tenant administrators.
/// </summary>
[Route("api/[controller]")]
[Authorize(Roles = "Tenants")]
public class TenantController : BaseApiController
{
    public TenantController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// The one anonymous door in this controller: a visitor creating their own organisation. It
    /// refuses unless AllowPublicTenantSignUp is set, and reuses CreateTenant's guards wholesale.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("SignUp")]
    public async Task<ActionResult<ApiSuccessResult<SignUpTenantResult>>> SignUpAsync(SignUpTenantRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<SignUpTenantResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(SignUpAsync)}",
            Content = response
        });
    }

    [HttpGet("GetTenantList")]
    public async Task<ActionResult<ApiSuccessResult<GetTenantListResult>>> GetTenantListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetTenantListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetTenantListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetTenantListAsync)}",
            Content = response
        });
    }

    [HttpPost("CreateTenant")]
    public async Task<ActionResult<ApiSuccessResult<CreateTenantResult>>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateTenantResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateTenantAsync)}",
            Content = response
        });
    }

    [HttpPost("UpdateTenant")]
    public async Task<ActionResult<ApiSuccessResult<UpdateTenantResult>>> UpdateTenantAsync(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateTenantResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateTenantAsync)}",
            Content = response
        });
    }

    [HttpPost("DeleteTenant")]
    public async Task<ActionResult<ApiSuccessResult<DeleteTenantResult>>> DeleteTenantAsync(DeleteTenantRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteTenantResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteTenantAsync)}",
            Content = response
        });
    }
}

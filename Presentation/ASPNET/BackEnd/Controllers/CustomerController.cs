using Application.Features.CustomerManager.Commands;
using Application.Features.CustomerManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class CustomerController : BaseApiController
{
    public CustomerController(ISender sender) : base(sender)
    {
    }

    [Authorize(Roles = "Customers")]
    [HttpPost("CreateCustomer")]
    public async Task<ActionResult<ApiSuccessResult<CreateCustomerResult>>> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateCustomerAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Customers")]
    [HttpPost("UpdateCustomer")]
    public async Task<ActionResult<ApiSuccessResult<UpdateCustomerResult>>> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateCustomerAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Customers")]
    [HttpPost("DeleteCustomer")]
    public async Task<ActionResult<ApiSuccessResult<DeleteCustomerResult>>> DeleteCustomerAsync(DeleteCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteCustomerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomerSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomerSingleResult>>> GetCustomerSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? id
        )
    {
        var request = new GetCustomerSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCustomerSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCustomerSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomerList")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomerListResult>>> GetCustomerListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetCustomerListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCustomerListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCustomerListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("SearchCustomer")]
    public async Task<ActionResult<ApiSuccessResult<SearchCustomerResult>>> SearchCustomerAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? term = null
        )
    {
        var request = new SearchCustomerRequest { Term = term };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<SearchCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(SearchCustomerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomerDue")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomerDueResult>>> GetCustomerDueAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? customerId = null,
        [FromQuery] string? excludeSalesOrderId = null
        )
    {
        var request = new GetCustomerDueRequest { CustomerId = customerId, ExcludeSalesOrderId = excludeSalesOrderId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCustomerDueResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCustomerDueAsync)}",
            Content = response
        });
    }

    [Authorize(Roles = "Dues,SalesOrders,Customers")]
    [HttpPost("CollectCustomerDue")]
    public async Task<ActionResult<ApiSuccessResult<CollectCustomerDueResult>>> CollectCustomerDueAsync(CollectCustomerDueRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CollectCustomerDueResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CollectCustomerDueAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomerDueList")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomerDueListResult>>> GetCustomerDueListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetCustomerDueListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCustomerDueListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCustomerDueListAsync)}",
            Content = response
        });
    }


}



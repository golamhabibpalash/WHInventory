using Application.Features.CustomerManager.Services;
using FluentValidation;
using MediatR;

namespace Application.Features.CustomerManager.Queries;


public class GetCustomerDueDto
{
    public string? CustomerId { get; init; }

    /// <summary>Outstanding receivable across the customer's confirmed sales orders. Never negative.</summary>
    public double PreviousDue { get; init; }
}

public class GetCustomerDueResult
{
    public GetCustomerDueDto? Data { get; init; }
}

public class GetCustomerDueRequest : IRequest<GetCustomerDueResult>
{
    public string? CustomerId { get; init; }

    /// <summary>
    /// Sales order to leave out of the total, so an order being edited is not counted in its own
    /// "previous" due. Null (a brand-new order) counts every confirmed order for the customer.
    /// </summary>
    public string? ExcludeSalesOrderId { get; init; }
}

public class GetCustomerDueValidator : AbstractValidator<GetCustomerDueRequest>
{
    public GetCustomerDueValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public class GetCustomerDueHandler : IRequestHandler<GetCustomerDueRequest, GetCustomerDueResult>
{
    private readonly CustomerDueService _dueService;

    public GetCustomerDueHandler(CustomerDueService dueService)
    {
        _dueService = dueService;
    }

    public async Task<GetCustomerDueResult> Handle(GetCustomerDueRequest request, CancellationToken cancellationToken)
    {
        var due = await _dueService.GetDueAsync(request.CustomerId, request.ExcludeSalesOrderId, cancellationToken);

        return new GetCustomerDueResult
        {
            Data = new GetCustomerDueDto
            {
                CustomerId = request.CustomerId,
                PreviousDue = due
            }
        };
    }
}

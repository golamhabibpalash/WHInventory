using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    private readonly IQueryContext _context;

    public GetCustomerDueHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetCustomerDueResult> Handle(GetCustomerDueRequest request, CancellationToken cancellationToken)
    {
        // Receivable = value billed to the customer on confirmed sales orders, less payments received
        // against those same orders. Both sides are aggregated in the database (no rows pulled into
        // memory), mirroring how PaymentService derives a single document's outstanding balance.
        var orders = _context.SalesOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.CustomerId == request.CustomerId && x.OrderStatus == SalesOrderStatus.Confirmed);

        if (!string.IsNullOrWhiteSpace(request.ExcludeSalesOrderId))
        {
            orders = orders.Where(x => x.Id != request.ExcludeSalesOrderId);
        }

        var billed = await orders.SumAsync(x => (double?)x.AfterTaxAmount ?? 0.0, cancellationToken);

        var paid = await _context.Payment
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(p =>
                p.Direction == PaymentDirection.Received &&
                p.ModuleName == nameof(SalesOrder) &&
                orders.Any(o => o.Id == p.ModuleId))
            .SumAsync(p => (double?)p.Amount ?? 0.0, cancellationToken);

        // An overpayment leaves the customer in credit, which is not a "due".
        var due = Math.Max(billed - paid, 0.0);

        return new GetCustomerDueResult
        {
            Data = new GetCustomerDueDto
            {
                CustomerId = request.CustomerId,
                PreviousDue = due.ToMoney()
            }
        };
    }
}

using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Features.CustomerManager.Services;
using Application.Features.PaymentManager.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public class GetCustomerDueListDto
{
    public string? CustomerId { get; init; }
    public string? CustomerNumber { get; init; }
    public string? CustomerName { get; init; }
    public string? PhoneNumber { get; init; }
    public int OrderCount { get; init; }
    public DateTime? LastOrderDate { get; init; }
    public double BilledAmount { get; init; }
    public double PaidAmount { get; init; }
    public double DueAmount { get; init; }

    /// <summary>"Due" while anything is still outstanding, otherwise "Settled".</summary>
    public string DueStatus { get; init; } = string.Empty;
}

public class GetCustomerDueListResult
{
    public List<GetCustomerDueListDto>? Data { get; init; }
}

public class GetCustomerDueListRequest : IRequest<GetCustomerDueListResult>
{
    public bool IsDeleted { get; init; } = false;
}

public class GetCustomerDueListHandler : IRequestHandler<GetCustomerDueListRequest, GetCustomerDueListResult>
{
    private readonly CustomerDueService _dueService;
    private readonly IQueryContext _context;

    public GetCustomerDueListHandler(CustomerDueService dueService, IQueryContext context)
    {
        _dueService = dueService;
        _context = context;
    }

    public async Task<GetCustomerDueListResult> Handle(GetCustomerDueListRequest request, CancellationToken cancellationToken)
    {
        var aggregates = await _dueService.GetAggregatesAsync(cancellationToken: cancellationToken);

        var customerIds = aggregates
            .Select(x => x.CustomerId)
            .Where(x => x != null)
            .ToHashSet();

        var customers = await _context.Customer
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Where(x => x.Id != null && customerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Number, x.Name, x.PhoneNumber })
            .ToDictionaryAsync(x => x.Id!, cancellationToken);

        var rows = aggregates
            .Where(x => x.CustomerId != null && customers.ContainsKey(x.CustomerId!))
            .Select(x =>
            {
                var customer = customers[x.CustomerId!];
                var due = x.DueAmount.ToMoney();
                return new GetCustomerDueListDto
                {
                    CustomerId = x.CustomerId,
                    CustomerNumber = customer.Number,
                    CustomerName = customer.Name,
                    PhoneNumber = customer.PhoneNumber,
                    OrderCount = x.OrderCount,
                    LastOrderDate = x.LastOrderDate,
                    BilledAmount = x.BilledAmount.ToMoney(),
                    PaidAmount = x.PaidAmount.ToMoney(),
                    DueAmount = due,
                    DueStatus = due > PaymentService.Tolerance ? "Due" : "Settled"
                };
            })
            .OrderByDescending(x => x.DueAmount)
            .ThenBy(x => x.CustomerName)
            .ToList();

        return new GetCustomerDueListResult
        {
            Data = rows
        };
    }
}

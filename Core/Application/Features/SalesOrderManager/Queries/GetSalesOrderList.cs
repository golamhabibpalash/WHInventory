using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SalesOrderManager.Queries;

public record GetSalesOrderListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? OrderDate { get; init; }
    public SalesOrderStatus? OrderStatus { get; init; }
    public string? OrderStatusName { get; init; }
    public string? Description { get; init; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? TaxId { get; init; }
    public string? TaxName { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetSalesOrderListResult
{
    public List<GetSalesOrderListDto>? Data { get; init; }
}

public class GetSalesOrderListRequest : IRequest<GetSalesOrderListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetSalesOrderListHandler : IRequestHandler<GetSalesOrderListRequest, GetSalesOrderListResult>
{
    private readonly IQueryContext _context;

    public GetSalesOrderListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetSalesOrderListResult> Handle(GetSalesOrderListRequest request, CancellationToken cancellationToken)
    {
        var dtos = await _context
            .SalesOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Select(x => new GetSalesOrderListDto
            {
                Id = x.Id,
                Number = x.Number,
                OrderDate = x.OrderDate,
                OrderStatus = x.OrderStatus,
                OrderStatusName = x.OrderStatus.HasValue ? x.OrderStatus.Value.ToFriendlyName() : string.Empty,
                Description = x.Description,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                TaxId = x.TaxId,
                TaxName = x.Tax != null ? x.Tax.Name : string.Empty,
                BeforeTaxAmount = x.BeforeTaxAmount,
                TaxAmount = x.TaxAmount,
                AfterTaxAmount = x.AfterTaxAmount,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .Take(2000)
            .ToListAsync(cancellationToken);

        return new GetSalesOrderListResult
        {
            Data = dtos
        };
    }


}




using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PurchaseOrderManager.Queries;

public record GetPurchaseOrderListDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public DateTime? OrderDate { get; init; }
    public PurchaseOrderStatus? OrderStatus { get; init; }
    public string? OrderStatusName { get; init; }
    public string? Description { get; init; }
    public string? VendorId { get; init; }
    public string? VendorName { get; init; }
    public string? TaxId { get; init; }
    public string? TaxName { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetPurchaseOrderListResult
{
    public List<GetPurchaseOrderListDto>? Data { get; init; }
}

public class GetPurchaseOrderListRequest : IRequest<GetPurchaseOrderListResult>
{
    public bool IsDeleted { get; init; } = false;
}


public class GetPurchaseOrderListHandler : IRequestHandler<GetPurchaseOrderListRequest, GetPurchaseOrderListResult>
{
    private readonly IQueryContext _context;

    public GetPurchaseOrderListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetPurchaseOrderListResult> Handle(GetPurchaseOrderListRequest request, CancellationToken cancellationToken)
    {
        var dtos = await _context
            .PurchaseOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Select(x => new GetPurchaseOrderListDto
            {
                Id = x.Id,
                Number = x.Number,
                OrderDate = x.OrderDate,
                OrderStatus = x.OrderStatus,
                OrderStatusName = x.OrderStatus.HasValue ? x.OrderStatus.Value.ToFriendlyName() : string.Empty,
                Description = x.Description,
                VendorId = x.VendorId,
                VendorName = x.Vendor != null ? x.Vendor.Name : string.Empty,
                TaxId = x.TaxId,
                TaxName = x.Tax != null ? x.Tax.Name : string.Empty,
                BeforeTaxAmount = x.BeforeTaxAmount,
                TaxAmount = x.TaxAmount,
                AfterTaxAmount = x.AfterTaxAmount,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .Take(2000)
            .ToListAsync(cancellationToken);

        return new GetPurchaseOrderListResult
        {
            Data = dtos
        };
    }


}




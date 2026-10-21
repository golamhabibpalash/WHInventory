using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
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
    public string? CustomerPhoneNumber { get; init; }
    public string? TaxId { get; init; }
    public string? TaxName { get; init; }
    public double? SubTotalAmount { get; init; }
    public double? DiscountAmount { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
    public double TotalOrderedQty { get; set; }
    public double TotalDeliveredQty { get; set; }
    public string? DeliveryStatus { get; set; }
    public DateTime? CreatedAtUtc { get; init; }
    public string? CreatedById { get; init; }
    public string? CreatedByName { get; init; }
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
    private readonly ISecurityService _securityService;

    public GetSalesOrderListHandler(IQueryContext context, ISecurityService securityService)
    {
        _context = context;
        _securityService = securityService;
    }

    public async Task<GetSalesOrderListResult> Handle(GetSalesOrderListRequest request, CancellationToken cancellationToken)
    {
        var page = await _context
            .SalesOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.OrderDate,
                x.OrderStatus,
                x.Description,
                x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.Name : string.Empty,
                CustomerPhoneNumber = x.Customer != null ? x.Customer.PhoneNumber : string.Empty,
                x.TaxId,
                TaxName = x.Tax != null ? x.Tax.Name : string.Empty,
                x.SubTotalAmount,
                x.DiscountAmount,
                x.BeforeTaxAmount,
                x.TaxAmount,
                x.AfterTaxAmount,
                x.CreatedAtUtc,
                x.CreatedById
            })
            .Take(2000)
            .ToListAsync(cancellationToken);

        var users = await _securityService.GetUserListAsync(cancellationToken);
        var userNames = users.ToDictionary(u => u.Id ?? string.Empty, u => $"{u.FirstName} {u.LastName}".Trim());

        var dtos = page.Select(x => new GetSalesOrderListDto
        {
            Id = x.Id,
            Number = x.Number,
            OrderDate = x.OrderDate,
            OrderStatus = x.OrderStatus,
            OrderStatusName = x.OrderStatus.HasValue ? x.OrderStatus.Value.ToFriendlyName() : string.Empty,
            Description = x.Description,
            CustomerId = x.CustomerId,
            CustomerName = x.CustomerName,
            CustomerPhoneNumber = x.CustomerPhoneNumber,
            TaxId = x.TaxId,
            TaxName = x.TaxName,
            SubTotalAmount = x.SubTotalAmount,
            DiscountAmount = x.DiscountAmount,
            BeforeTaxAmount = x.BeforeTaxAmount,
            TaxAmount = x.TaxAmount,
            AfterTaxAmount = x.AfterTaxAmount,
            CreatedAtUtc = x.CreatedAtUtc,
            CreatedById = x.CreatedById,
            CreatedByName = x.CreatedById != null && userNames.TryGetValue(x.CreatedById, out var cn) ? cn : null
        }).ToList();

        // Delivery progress per sales order: ordered = sum of its item quantities; delivered = sum of
        // the movements on every delivery order linked to it. Mirrors GetDeliveryOrderList so the two
        // pages agree, and surfaces whether an order is fully/partially/not yet delivered.
        var soIds = dtos.Where(x => !string.IsNullOrEmpty(x.Id)).Select(x => x.Id!).ToList();
        if (soIds.Count > 0)
        {
            var orderedRows = await _context.SalesOrderItem
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.SalesOrderId != null && soIds.Contains(x.SalesOrderId))
                .GroupBy(x => x.SalesOrderId!)
                .Select(g => new { SOId = g.Key, Total = g.Sum(x => x.Quantity ?? 0.0) })
                .ToListAsync(cancellationToken);
            var orderedDict = orderedRows.ToDictionary(x => x.SOId, x => x.Total);

            var deliveryOrders = await _context.DeliveryOrder
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.SalesOrderId != null && soIds.Contains(x.SalesOrderId))
                .Select(x => new { x.Id, x.SalesOrderId })
                .ToListAsync(cancellationToken);
            var doIds = deliveryOrders.Where(x => !string.IsNullOrEmpty(x.Id)).Select(x => x.Id!).ToList();

            var deliveredByDo = new Dictionary<string, double>();
            if (doIds.Count > 0)
            {
                var deliveredRows = await _context.InventoryTransaction
                    .AsNoTracking()
                    .ApplyIsDeletedFilter(false)
                    .Where(x => x.ModuleId != null && doIds.Contains(x.ModuleId)
                             && x.ModuleName == nameof(DeliveryOrder))
                    .GroupBy(x => x.ModuleId!)
                    .Select(g => new { DOId = g.Key, Total = g.Sum(x => x.Movement ?? 0.0) })
                    .ToListAsync(cancellationToken);
                deliveredByDo = deliveredRows.ToDictionary(x => x.DOId, x => x.Total);
            }

            var deliveredBySo = new Dictionary<string, double>();
            foreach (var deliveryOrder in deliveryOrders)
            {
                if (string.IsNullOrEmpty(deliveryOrder.SalesOrderId) || string.IsNullOrEmpty(deliveryOrder.Id)) continue;
                var delivered = deliveredByDo.TryGetValue(deliveryOrder.Id, out var dq) ? dq : 0.0;
                deliveredBySo[deliveryOrder.SalesOrderId] =
                    (deliveredBySo.TryGetValue(deliveryOrder.SalesOrderId, out var acc) ? acc : 0.0) + delivered;
            }

            foreach (var dto in dtos)
            {
                if (string.IsNullOrEmpty(dto.Id)) continue;
                var ordered = orderedDict.TryGetValue(dto.Id, out var oq) ? oq : 0.0;
                var delivered = deliveredBySo.TryGetValue(dto.Id, out var dv) ? dv : 0.0;

                dto.TotalOrderedQty = ordered;
                dto.TotalDeliveredQty = delivered;
                dto.DeliveryStatus = ordered <= 0
                    ? "N/A"
                    : delivered >= ordered
                        ? "Complete"
                        : delivered > 0
                            ? "Partial"
                            : "Not Started";
            }
        }

        return new GetSalesOrderListResult
        {
            Data = dtos
        };
    }


}




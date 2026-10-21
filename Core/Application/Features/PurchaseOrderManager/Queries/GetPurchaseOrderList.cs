using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
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
    public string? ReferenceNumber { get; init; }
    public string? VendorId { get; init; }
    public string? VendorName { get; init; }
    public string? TaxId { get; init; }
    public string? TaxName { get; init; }
    public double? SubTotalAmount { get; init; }
    public double? DiscountAmount { get; init; }
    public double? BeforeTaxAmount { get; init; }
    public double? TaxAmount { get; init; }
    public double? AfterTaxAmount { get; init; }
    public double TotalOrderedQty { get; set; }
    public double TotalReceivedQty { get; set; }
    public string? ReceiveStatus { get; set; }
    public DateTime? CreatedAtUtc { get; init; }
    public string? CreatedById { get; init; }
    public string? CreatedByName { get; init; }
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
    private readonly ISecurityService _securityService;

    public GetPurchaseOrderListHandler(IQueryContext context, ISecurityService securityService)
    {
        _context = context;
        _securityService = securityService;
    }

    public async Task<GetPurchaseOrderListResult> Handle(GetPurchaseOrderListRequest request, CancellationToken cancellationToken)
    {
        var page = await _context
            .PurchaseOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(request.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.OrderDate,
                x.OrderStatus,
                x.Description,
                x.ReferenceNumber,
                x.VendorId,
                VendorName = x.Vendor != null ? x.Vendor.Name : string.Empty,
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

        var poIds = page.Where(x => !string.IsNullOrEmpty(x.Id)).Select(x => x.Id!).ToList();

        var orderedByPo = new Dictionary<string, double>();
        if (poIds.Count > 0)
        {
            var orderedRows = await _context.PurchaseOrderItem
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.PurchaseOrderId != null && poIds.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId!)
                .Select(g => new { POId = g.Key, Total = g.Sum(x => x.Quantity ?? 0.0) })
                .ToListAsync(cancellationToken);
            orderedByPo = orderedRows.ToDictionary(x => x.POId, x => x.Total);
        }

        var receivedByPo = new Dictionary<string, double>();
        if (poIds.Count > 0)
        {
            var grRows = await _context.GoodsReceive
                .AsNoTracking()
                .ApplyIsDeletedFilter(false)
                .Where(x => x.PurchaseOrderId != null && poIds.Contains(x.PurchaseOrderId))
                .Select(x => new { x.Id, POId = x.PurchaseOrderId! })
                .ToListAsync(cancellationToken);

            var grIds = grRows.Select(x => x.Id).ToList();
            if (grIds.Count > 0)
            {
                var receivedRows = await _context.InventoryTransaction
                    .AsNoTracking()
                    .ApplyIsDeletedFilter(false)
                    .Where(x => x.ModuleId != null && grIds.Contains(x.ModuleId)
                             && x.ModuleName == nameof(GoodsReceive)
                             && x.Status == InventoryTransactionStatus.Confirmed)
                    .GroupBy(x => x.ModuleId!)
                    .Select(g => new { GRId = g.Key, Total = g.Sum(x => x.Movement ?? 0.0) })
                    .ToListAsync(cancellationToken);

                var receivedByGr = receivedRows.ToDictionary(x => x.GRId, x => x.Total);
                foreach (var gr in grRows)
                {
                    receivedByPo[gr.POId] = receivedByPo.GetValueOrDefault(gr.POId) + receivedByGr.GetValueOrDefault(gr.Id);
                }
            }
        }

        var dtos = page.Select(x => new GetPurchaseOrderListDto
        {
            Id = x.Id,
            Number = x.Number,
            OrderDate = x.OrderDate,
            OrderStatus = x.OrderStatus,
            OrderStatusName = x.OrderStatus.HasValue ? x.OrderStatus.Value.ToFriendlyName() : string.Empty,
            Description = x.Description,
            ReferenceNumber = x.ReferenceNumber,
            VendorId = x.VendorId,
            VendorName = x.VendorName,
            TaxId = x.TaxId,
            TaxName = x.TaxName,
            SubTotalAmount = x.SubTotalAmount,
            DiscountAmount = x.DiscountAmount,
            BeforeTaxAmount = x.BeforeTaxAmount,
            TaxAmount = x.TaxAmount,
            AfterTaxAmount = x.AfterTaxAmount,
            TotalOrderedQty = x.Id != null ? orderedByPo.GetValueOrDefault(x.Id) : 0.0,
            TotalReceivedQty = x.Id != null ? receivedByPo.GetValueOrDefault(x.Id) : 0.0,
            ReceiveStatus = ReceiveStatusOf(
                x.Id != null ? orderedByPo.GetValueOrDefault(x.Id) : 0.0,
                x.Id != null ? receivedByPo.GetValueOrDefault(x.Id) : 0.0),
            CreatedAtUtc = x.CreatedAtUtc,
            CreatedById = x.CreatedById,
            CreatedByName = x.CreatedById != null && userNames.TryGetValue(x.CreatedById, out var cn) ? cn : null
        }).ToList();

        return new GetPurchaseOrderListResult
        {
            Data = dtos
        };
    }

    private static string ReceiveStatusOf(double totalOrdered, double totalReceived)
    {
        if (totalOrdered <= 0)
            return "N/A";
        if (totalReceived >= totalOrdered)
            return "Complete";
        if (totalReceived > 0)
            return "Partial";
        return "Not Started";
    }


}




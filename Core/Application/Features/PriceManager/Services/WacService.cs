using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PriceManager.Services;

public class WacService
{
    private readonly IQueryContext _context;

    public WacService(IQueryContext context)
    {
        _context = context;
    }

    public async Task<double> GetWeightedAverageCostAsync(string productId, CancellationToken cancellationToken = default)
    {
        // Get purchase order IDs for all confirmed goods receives
        var confirmedPurchaseOrderIds = await _context.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(gr => gr.Status == GoodsReceiveStatus.Confirmed && gr.PurchaseOrderId != null)
            .Select(gr => gr.PurchaseOrderId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (confirmedPurchaseOrderIds.Count == 0)
        {
            return 0;
        }

        var result = await _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(poi =>
                poi.ProductId == productId &&
                poi.PurchaseOrderId != null &&
                confirmedPurchaseOrderIds.Contains(poi.PurchaseOrderId))
            .GroupBy(poi => poi.ProductId)
            .Select(g => new
            {
                TotalCost = g.Sum(poi => (poi.UnitPrice ?? 0) * (poi.Quantity ?? 0)),
                TotalQty = g.Sum(poi => poi.Quantity ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null || result.TotalQty == 0)
        {
            return 0;
        }

        return result.TotalQty > 0 ? result.TotalCost / result.TotalQty : 0;
    }
}

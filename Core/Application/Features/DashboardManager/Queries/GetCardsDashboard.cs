using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;


public class GetCardsDashboardDto
{
    public CardsItem? CardsDashboard { get; init; }
}

public class GetCardsDashboardResult
{
    public GetCardsDashboardDto? Data { get; init; }
}

public class GetCardsDashboardRequest : IRequest<GetCardsDashboardResult>
{
    /// <summary>Optional warehouse (branch) scope for the ledger-derived totals.</summary>
    public string? WarehouseId { get; init; }
}

public class GetCardsDashboardHandler : IRequestHandler<GetCardsDashboardRequest, GetCardsDashboardResult>
{
    private readonly IQueryContext _context;

    public GetCardsDashboardHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetCardsDashboardResult> Handle(GetCardsDashboardRequest request, CancellationToken cancellationToken)
    {
        // These must run one at a time: EF Core rejects concurrent operations on a single
        // DbContext instance, and IQueryContext is scoped to the request.
        var salesTotal = await _context.SalesOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .SumAsync(x => (double?)x.Quantity, cancellationToken);

        var purchaseTotal = await _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .SumAsync(x => (double?)x.Quantity, cancellationToken);

        // One grouped pass over the ledger replaces six separate per-module sums.
        var ledger = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.Status == InventoryTransactionStatus.Confirmed &&
                x.Warehouse!.SystemWarehouse == false);

        if (!string.IsNullOrWhiteSpace(request.WarehouseId))
        {
            ledger = ledger.Where(x => x.WarehouseId == request.WarehouseId);
        }

        var movementByModule = await ledger
            .GroupBy(x => x.ModuleName)
            .Select(g => new { ModuleName = g.Key, Total = g.Sum(x => x.Movement ?? 0.0) })
            .ToListAsync(cancellationToken);

        var totalsByModule = movementByModule
            .Where(x => x.ModuleName != null)
            .ToDictionary(x => x.ModuleName!, x => x.Total);

        double MovementFor(string moduleName) => totalsByModule.GetValueOrDefault(moduleName, 0.0);

        var cardsDashboardData = new CardsItem
        {
            SalesTotal = salesTotal,
            SalesReturnTotal = MovementFor(nameof(SalesReturn)),
            PurchaseTotal = purchaseTotal,
            PurchaseReturnTotal = MovementFor(nameof(PurchaseReturn)),
            DeliveryOrderTotal = MovementFor(nameof(DeliveryOrder)),
            GoodsReceiveTotal = MovementFor(nameof(GoodsReceive)),
            TransferOutTotal = MovementFor(nameof(TransferOut)),
            TransferInTotal = MovementFor(nameof(TransferIn))
        };



        var result = new GetCardsDashboardResult
        {
            Data = new GetCardsDashboardDto
            {
                CardsDashboard = cardsDashboardData
            }
        };

        return result;
    }
}

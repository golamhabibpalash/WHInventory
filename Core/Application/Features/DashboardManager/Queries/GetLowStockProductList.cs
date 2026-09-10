using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Common;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;


/// <summary>
/// One physical product whose confirmed on-hand quantity is at or below
/// <see cref="Constants.InventoryConsts.LowStockThreshold"/>. Backs the drill-down list
/// behind the dashboard "Low Stock Items" tile, so it uses the same filters as
/// <see cref="GetOverviewDashboardHandler"/>.
/// </summary>
public class GetLowStockProductListDto
{
    public string? ProductId { get; init; }
    public string? ProductNumber { get; init; }
    public string? ProductName { get; init; }
    public string? ProductGroupName { get; init; }
    public string? BrandName { get; init; }
    public string? UnitMeasureName { get; init; }
    public double StockOnHand { get; init; }
    public double LowStockThreshold { get; init; }
}

public class GetLowStockProductListResult
{
    public List<GetLowStockProductListDto>? Data { get; init; }
}

public class GetLowStockProductListRequest : IRequest<GetLowStockProductListResult>
{
    /// <summary>Optional warehouse (branch) scope. Null means every warehouse, matching the tile.</summary>
    public string? WarehouseId { get; init; }
}

public class GetLowStockProductListHandler : IRequestHandler<GetLowStockProductListRequest, GetLowStockProductListResult>
{
    private const double LowStockThreshold = Constants.InventoryConsts.LowStockThreshold;

    private readonly IQueryContext _context;

    public GetLowStockProductListHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetLowStockProductListResult> Handle(GetLowStockProductListRequest request, CancellationToken cancellationToken)
    {
        var scopedToWarehouse = !string.IsNullOrWhiteSpace(request.WarehouseId);

        // Confirmed ledger movement across real warehouses only; the system warehouses are
        // virtual counterparties, never a branch to report on.
        var ledger = _context.InventoryTransaction
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.Status == InventoryTransactionStatus.Confirmed &&
                x.Warehouse!.SystemWarehouse == false &&
                x.Product!.Physical == true);

        if (scopedToWarehouse)
        {
            ledger = ledger.Where(x => x.WarehouseId == request.WarehouseId);
        }

        var stockByProduct = await ledger
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Stock ?? 0.0) })
            .ToListAsync(cancellationToken);

        var stockLookup = stockByProduct
            .Where(x => x.ProductId != null)
            .ToDictionary(x => x.ProductId!, x => x.Stock);

        // Start from the catalogue, not the ledger: a product with no confirmed movement at all
        // has zero on hand and must still show up as low stock.
        var products = await _context.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Physical == true)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.Name,
                GroupName = x.ProductGroup!.Name,
                BrandName = x.Brand!.Name,
                UnitName = x.UnitMeasure!.Name
            })
            .ToListAsync(cancellationToken);

        var lowStock = products
            .Select(p => new GetLowStockProductListDto
            {
                ProductId = p.Id,
                ProductNumber = p.Number,
                ProductName = p.Name,
                ProductGroupName = p.GroupName,
                BrandName = p.BrandName,
                UnitMeasureName = p.UnitName,
                StockOnHand = stockLookup.GetValueOrDefault(p.Id, 0.0),
                LowStockThreshold = LowStockThreshold
            })
            .Where(x => x.StockOnHand <= LowStockThreshold)
            .OrderBy(x => x.StockOnHand)
            .ThenBy(x => x.ProductName)
            .ToList();

        return new GetLowStockProductListResult
        {
            Data = lowStock
        };
    }
}

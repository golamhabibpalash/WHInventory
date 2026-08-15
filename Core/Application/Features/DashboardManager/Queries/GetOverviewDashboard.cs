using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Queries;


public class GetOverviewDashboardDto
{
    public OverviewKpiItem? KpiDashboard { get; init; }
    public List<OverviewStatusItem>? InventoryStatusDashboard { get; init; }
    public List<OverviewTrendItem>? MovementTrendDashboard { get; init; }
    public List<OverviewCategoryItem>? TopCategoryDashboard { get; init; }
    public List<OverviewActivityItem>? RecentActivityDashboard { get; init; }
}

public class GetOverviewDashboardResult
{
    public GetOverviewDashboardDto? Data { get; init; }
}

public class GetOverviewDashboardRequest : IRequest<GetOverviewDashboardResult>
{
    /// <summary>Optional warehouse (branch) to scope the figures to. Null means all warehouses.</summary>
    public string? WarehouseId { get; init; }
}

public class GetOverviewDashboardHandler : IRequestHandler<GetOverviewDashboardRequest, GetOverviewDashboardResult>
{
    /// <summary>On-hand quantity at or below this counts as low stock.</summary>
    private const double LowStockThreshold = 10;

    /// <summary>Days shown on the inbound/outbound trend chart, today inclusive.</summary>
    private const int TrendDays = 7;

    /// <summary>Baseline window the KPI tiles compare against.</summary>
    private const int ComparisonDays = 30;

    private const int TopCategoryCount = 4;
    private const int RecentActivityCount = 6;

    private readonly IQueryContext _context;

    public GetOverviewDashboardHandler(IQueryContext context)
    {
        _context = context;
    }

    public async Task<GetOverviewDashboardResult> Handle(GetOverviewDashboardRequest request, CancellationToken cancellationToken)
    {
        var todayStart = DateTime.UtcNow.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var comparisonStart = todayStart.AddDays(-ComparisonDays);
        var trendStart = todayStart.AddDays(-(TrendDays - 1));

        // Every ledger row is booked against a real warehouse; the system warehouses only ever appear
        // as the virtual WarehouseFrom/WarehouseTo, so this is the full on-hand picture.
        var scopedToWarehouse = !string.IsNullOrWhiteSpace(request.WarehouseId);

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

        var stockNow = await ledger
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Stock ?? 0.0) })
            .ToListAsync(cancellationToken);

        var stockThen = await ledger
            .Where(x => x.MovementDate < comparisonStart)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.Stock ?? 0.0) })
            .ToListAsync(cancellationToken);

        var products = await _context.Product
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Physical == true)
            .Select(x => new { x.Id, GroupName = x.ProductGroup!.Name, x.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        var trendRows = await ledger
            .Where(x => x.MovementDate >= trendStart && x.MovementDate < tomorrowStart)
            .Select(x => new { x.MovementDate, x.TransType, Movement = x.Movement ?? 0.0 })
            .ToListAsync(cancellationToken);

        // Daily averages over the preceding window give the KPI tiles something to compare today against.
        var priorFlow = await ledger
            .Where(x => x.MovementDate >= comparisonStart && x.MovementDate < todayStart)
            .GroupBy(x => x.TransType)
            .Select(g => new { TransType = g.Key, Movement = g.Sum(x => x.Movement ?? 0.0) })
            .ToListAsync(cancellationToken);

        var transferOutTotal = await ledger
            .Where(x => x.ModuleName == nameof(TransferOut))
            .SumAsync(x => (double?)x.Movement, cancellationToken) ?? 0.0;

        var transferInTotal = await ledger
            .Where(x => x.ModuleName == nameof(TransferIn))
            .SumAsync(x => (double?)x.Movement, cancellationToken) ?? 0.0;

        // Confirmed sales orders with no confirmed delivery yet are committed but still on the shelf.
        // Sales and purchase orders carry no warehouse, so Reserved and On Order cannot be
        // attributed to one branch. Under a warehouse filter they are left out rather than
        // repeated unchanged, which would overstate that branch's pipeline.
        var reserved = 0.0;
        var onOrder = 0.0;

        if (!scopedToWarehouse)
        {
            (reserved, onOrder) = await GetPipelineTotalsAsync(cancellationToken);
        }

        // Pull a generous slice of lines so the roll-up below still yields enough distinct documents.
        var activityRows = await ledger
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(RecentActivityCount * 15)
            .Select(x => new
            {
                x.ModuleName,
                x.ModuleNumber,
                x.MovementDate,
                x.CreatedAtUtc,
                x.TransType,
                Movement = x.Movement ?? 0.0
            })
            .ToListAsync(cancellationToken);

        var stockNowByProduct = stockNow
            .Where(x => x.ProductId != null)
            .ToDictionary(x => x.ProductId!, x => x.Stock);

        var stockThenByProduct = stockThen
            .Where(x => x.ProductId != null)
            .ToDictionary(x => x.ProductId!, x => x.Stock);

        var totalInventory = stockNowByProduct.Values.Sum();
        var totalInventoryThen = stockThenByProduct.Values.Sum();

        var lowStockCount = products
            .Count(p => stockNowByProduct.GetValueOrDefault(p.Id, 0.0) <= LowStockThreshold);

        // Products that did not exist a month ago cannot be part of the month-ago baseline.
        var lowStockCountThen = products
            .Count(p => p.CreatedAtUtc < comparisonStart &&
                        stockThenByProduct.GetValueOrDefault(p.Id, 0.0) <= LowStockThreshold);

        var inboundToday = trendRows
            .Where(x => x.TransType == InventoryTransType.In && x.MovementDate >= todayStart)
            .Sum(x => x.Movement);

        var outboundToday = trendRows
            .Where(x => x.TransType == InventoryTransType.Out && x.MovementDate >= todayStart)
            .Sum(x => x.Movement);

        var inboundDailyAverage = priorFlow
            .Where(x => x.TransType == InventoryTransType.In)
            .Sum(x => x.Movement) / ComparisonDays;

        var outboundDailyAverage = priorFlow
            .Where(x => x.TransType == InventoryTransType.Out)
            .Sum(x => x.Movement) / ComparisonDays;

        var kpi = new OverviewKpiItem
        {
            TotalInventory = totalInventory,
            TotalInventoryDeltaPct = CalculateDeltaPct(totalInventory, totalInventoryThen),
            InboundToday = inboundToday,
            InboundDeltaPct = CalculateDeltaPct(inboundToday, inboundDailyAverage),
            OutboundToday = outboundToday,
            OutboundDeltaPct = CalculateDeltaPct(outboundToday, outboundDailyAverage),
            LowStockCount = lowStockCount,
            LowStockDeltaPct = CalculateDeltaPct(lowStockCount, lowStockCountThen),
            LowStockThreshold = LowStockThreshold
        };

        // Reserved units are still physically on hand, so subtract them out of the In Stock slice
        // to keep the doughnut a partition rather than an overlapping tally.
        var onHand = Math.Max(totalInventory, 0.0);
        var reservedSlice = Math.Min(Math.Max(reserved, 0.0), onHand);
        var inventoryStatus = new List<OverviewStatusItem>
        {
            new() { Label = "In Stock", Value = onHand - reservedSlice },
            new() { Label = "Reserved", Value = reservedSlice },
            new() { Label = "In Transit", Value = Math.Max(transferOutTotal - transferInTotal, 0.0) },
            new() { Label = "On Order", Value = Math.Max(onOrder, 0.0) }
        };

        var movementByDay = trendRows
            .Where(x => x.MovementDate.HasValue)
            .GroupBy(x => x.MovementDate!.Value.Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Inbound = g.Where(x => x.TransType == InventoryTransType.In).Sum(x => x.Movement),
                    Outbound = g.Where(x => x.TransType == InventoryTransType.Out).Sum(x => x.Movement)
                });

        var movementTrend = Enumerable.Range(0, TrendDays)
            .Select(offset =>
            {
                var day = trendStart.AddDays(offset);
                var found = movementByDay.GetValueOrDefault(day);
                return new OverviewTrendItem
                {
                    Date = day,
                    Label = day.ToString("dd MMM"),
                    Inbound = found?.Inbound ?? 0.0,
                    Outbound = found?.Outbound ?? 0.0
                };
            })
            .ToList();

        var groupTotals = products
            .GroupBy(p => string.IsNullOrWhiteSpace(p.GroupName) ? "Ungrouped" : p.GroupName!)
            .Select(g => new
            {
                Name = g.Key,
                Quantity = g.Sum(p => Math.Max(stockNowByProduct.GetValueOrDefault(p.Id, 0.0), 0.0))
            })
            .Where(x => x.Quantity > 0)
            .OrderByDescending(x => x.Quantity)
            .ToList();

        // Everything past the top N is folded into a single "Others" row so the shares still add to 100%.
        var categoryTotal = groupTotals.Sum(x => x.Quantity);
        var topCategories = groupTotals
            .Take(TopCategoryCount)
            .Select(x => new OverviewCategoryItem
            {
                Name = x.Name,
                Quantity = x.Quantity,
                Percentage = categoryTotal > 0 ? Math.Round(x.Quantity / categoryTotal * 100, 1) : 0
            })
            .ToList();

        var remainder = groupTotals.Skip(TopCategoryCount).Sum(x => x.Quantity);
        if (remainder > 0)
        {
            topCategories.Add(new OverviewCategoryItem
            {
                Name = "Others",
                Quantity = remainder,
                Percentage = categoryTotal > 0 ? Math.Round(remainder / categoryTotal * 100, 1) : 0
            });
        }

        var recentActivities = activityRows
            .GroupBy(x => new { x.ModuleName, x.ModuleNumber })
            .Select(g => new OverviewActivityItem
            {
                ModuleName = g.Key.ModuleName ?? string.Empty,
                Title = DescribeModule(g.Key.ModuleName),
                Number = g.Key.ModuleNumber ?? string.Empty,
                Direction = g.First().TransType == InventoryTransType.In ? "In" : "Out",
                Quantity = g.Sum(x => x.Movement),
                OccurredAtUtc = g.Max(x => x.MovementDate ?? x.CreatedAtUtc)
            })
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(RecentActivityCount)
            .ToList();

        return new GetOverviewDashboardResult
        {
            Data = new GetOverviewDashboardDto
            {
                KpiDashboard = kpi,
                InventoryStatusDashboard = inventoryStatus,
                MovementTrendDashboard = movementTrend,
                TopCategoryDashboard = topCategories,
                RecentActivityDashboard = recentActivities
            }
        };
    }

    /// <summary>
    /// Company-wide committed quantities: confirmed sales orders not yet delivered (Reserved) and
    /// confirmed purchase orders not yet received (On Order).
    /// </summary>
    private async Task<(double Reserved, double OnOrder)> GetPipelineTotalsAsync(CancellationToken cancellationToken)
    {
        // The null guard matters: a NULL inside a SQL NOT IN list makes the whole predicate NULL,
        // which would silently zero out the result.
        var deliveredSalesOrderIds = _context.DeliveryOrder
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Status == DeliveryOrderStatus.Confirmed && x.SalesOrderId != null)
            .Select(x => x.SalesOrderId!);

        var reserved = await _context.SalesOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.SalesOrder!.OrderStatus == SalesOrderStatus.Confirmed &&
                x.SalesOrderId != null &&
                !deliveredSalesOrderIds.Contains(x.SalesOrderId))
            .SumAsync(x => (double?)x.Quantity, cancellationToken) ?? 0.0;

        var receivedPurchaseOrderIds = _context.GoodsReceive
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x => x.Status == GoodsReceiveStatus.Confirmed && x.PurchaseOrderId != null)
            .Select(x => x.PurchaseOrderId!);

        var onOrder = await _context.PurchaseOrderItem
            .AsNoTracking()
            .ApplyIsDeletedFilter(false)
            .Where(x =>
                x.PurchaseOrder!.OrderStatus == PurchaseOrderStatus.Confirmed &&
                x.PurchaseOrderId != null &&
                !receivedPurchaseOrderIds.Contains(x.PurchaseOrderId))
            .SumAsync(x => (double?)x.Quantity, cancellationToken) ?? 0.0;

        return (reserved, onOrder);
    }

    private static double? CalculateDeltaPct(double current, double baseline)
    {
        if (baseline <= 0) return null;
        return Math.Round((current - baseline) / baseline * 100, 1);
    }

    private static string DescribeModule(string? moduleName) => moduleName switch
    {
        nameof(GoodsReceive) => "Goods received",
        nameof(DeliveryOrder) => "Delivered to customer",
        nameof(SalesReturn) => "Sales return received",
        nameof(PurchaseReturn) => "Returned to vendor",
        nameof(TransferIn) => "Transfer received",
        nameof(TransferOut) => "Transfer dispatched",
        nameof(PositiveAdjustment) => "Positive adjustment",
        nameof(NegativeAdjustment) => "Negative adjustment",
        nameof(StockCount) => "Stock count correction",
        nameof(Scrapping) => "Stock scrapped",
        _ => "Inventory movement"
    };
}

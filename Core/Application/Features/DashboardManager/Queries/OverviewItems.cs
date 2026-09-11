namespace Application.Features.DashboardManager.Queries;

/// <summary>
/// Headline inventory numbers with a percentage change against the comparison window.
/// A null delta means there is no baseline to compare against yet.
/// </summary>
public class OverviewKpiItem
{
    public double TotalInventory { get; init; }
    public double? TotalInventoryDeltaPct { get; init; }
    public double InboundToday { get; init; }
    public double? InboundDeltaPct { get; init; }
    public double OutboundToday { get; init; }
    public double? OutboundDeltaPct { get; init; }
    public int LowStockCount { get; init; }
    public double? LowStockDeltaPct { get; init; }
    public double LowStockThreshold { get; init; }
    public double TodayPurchaseAmount { get; init; }
    public double TodaySalesAmount { get; init; }
    public double TodayDueAmount { get; init; }

    /// <summary>
    /// Units still owed to customers: ordered on a confirmed Sales Order but not yet covered by a
    /// Delivery Order, per product line. A partially delivered order only contributes its remainder,
    /// not its full original quantity. Company-wide only (Sales Orders carry no WarehouseId).
    /// </summary>
    public double PendingDeliveryCount { get; init; }
    public int PendingDeliveryOrderCount { get; init; }

    /// <summary>
    /// Units still owed by vendors: ordered on a confirmed Purchase Order but not yet covered by a
    /// Goods Receive, per product line. A partially received order only contributes its remainder,
    /// not its full original quantity. Company-wide only (Purchase Orders carry no WarehouseId).
    /// </summary>
    public double PendingGoodsReceiveCount { get; init; }
    public int PendingGoodsReceiveOrderCount { get; init; }
}

/// <summary>
/// One slice of the inventory status doughnut.
/// </summary>
public class OverviewStatusItem
{
    public string Label { get; init; } = string.Empty;
    public double Value { get; init; }
}

/// <summary>
/// One day of inbound / outbound movement for the trend chart.
/// </summary>
public class OverviewTrendItem
{
    public DateTime Date { get; init; }
    public string Label { get; init; } = string.Empty;
    public double Inbound { get; init; }
    public double Outbound { get; init; }
}

/// <summary>
/// On-hand quantity held by a product group, with its share of total on-hand stock.
/// </summary>
public class OverviewCategoryItem
{
    public string Name { get; init; } = string.Empty;
    public double Quantity { get; init; }
    public double Percentage { get; init; }
}

/// <summary>
/// A recent warehouse document, rolled up from its inventory transaction lines.
/// </summary>
public class OverviewActivityItem
{
    public string ModuleName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public double Quantity { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
}

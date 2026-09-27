using Domain.Common;

namespace Domain.Entities;

public class SalesOrderItem : BaseEntity
{
    public string? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public string? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? Summary { get; set; }
    public double? UnitPrice { get; set; } = 0;

    /// <summary>
    /// Commission taken off each unit's price (a per-unit amount, not a percentage).
    /// The line discount is <c>CommissionRate * Quantity</c>.
    /// </summary>
    public double? CommissionRate { get; set; } = 0;
    public double? Quantity { get; set; } = 1;

    /// <summary>
    /// Net line total after commission: <c>(UnitPrice - CommissionRate) * Quantity</c>.
    /// </summary>
    public double? Total { get; set; } = 0;

}

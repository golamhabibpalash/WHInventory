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
    /// Commission as a percentage of the unit price (e.g. 5 = 5%).
    /// The line discount is <c>UnitPrice * Quantity * CommissionRate / 100</c>.
    /// </summary>
    public double? CommissionRate { get; set; } = 0;
    public double? Quantity { get; set; } = 1;

    /// <summary>
    /// Net line total after commission: <c>UnitPrice * (1 - CommissionRate / 100) * Quantity</c>.
    /// </summary>
    public double? Total { get; set; } = 0;

}

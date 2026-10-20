using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class PurchaseOrder : BaseEntity
{
    public string? Number { get; set; }
    public DateTime? OrderDate { get; set; }
    public PurchaseOrderStatus? OrderStatus { get; set; }
    public string? Description { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string? TaxId { get; set; }
    public Tax? Tax { get; set; }

    /// <summary>Gross subtotal before commission discount: sum of <c>UnitPrice * Quantity</c>.</summary>
    public double? SubTotalAmount { get; set; }

    /// <summary>Total commission discount across all items: sum of <c>CommissionRate * Quantity</c>.</summary>
    public double? DiscountAmount { get; set; }

    /// <summary>Taxable amount after discount: <c>SubTotalAmount - DiscountAmount</c>.</summary>
    public double? BeforeTaxAmount { get; set; }
    public double? TaxAmount { get; set; }
    public double? AfterTaxAmount { get; set; }
    public ICollection<PurchaseOrderItem> PurchaseOrderItemList { get; set; } = new List<PurchaseOrderItem>();
}

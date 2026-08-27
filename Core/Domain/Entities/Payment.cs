using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A single movement of money against a document. Attached to its document the same way
/// InventoryTransaction attaches to stock movements — by ModuleName plus ModuleId — so the
/// same ledger serves Sales Orders, Purchase Orders and anything added later.
/// </summary>
public class Payment : BaseEntity
{
    public string? Number { get; set; }
    public string? ModuleName { get; set; }
    public string? ModuleId { get; set; }
    public string? ModuleNumber { get; set; }
    public DateTime? PaymentDate { get; set; }
    public PaymentDirection Direction { get; set; } = PaymentDirection.Received;
    public double? Amount { get; set; }
    public string? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Cheque number, card authorisation code, bank reference — whatever proves it.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
}

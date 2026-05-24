using System.ComponentModel;

namespace Domain.Enums;

public enum PurchaseOrderStatus
{
    [Description("Draft")]
    Draft = 0,
    [Description("Cancelled")]
    Cancelled = 1,
    [Description("Confirmed")]
    Confirmed = 2,
    [Description("Archived")]
    Archived = 3,
    [Description("Open")]
    Open = 4,
    [Description("Partially Received")]
    PartiallyReceived = 5,
    [Description("Fully Received")]
    FullyReceived = 6,
    [Description("Closed")]
    Closed = 7
}

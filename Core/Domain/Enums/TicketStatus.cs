using System.ComponentModel;

namespace Domain.Enums;

public enum TicketStatus
{
    [Description("New")]
    New = 0,
    [Description("Open")]
    Open = 1,
    [Description("In Progress")]
    InProgress = 2,
    [Description("Pending User")]
    PendingUser = 3,
    [Description("Pending Internal")]
    PendingInternal = 4,
    [Description("Resolved")]
    Resolved = 5,
    [Description("Closed")]
    Closed = 6,
    [Description("Reopened")]
    Reopened = 7,
    [Description("Cancelled")]
    Cancelled = 8
}

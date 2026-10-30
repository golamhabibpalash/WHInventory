using System.ComponentModel;

namespace Domain.Enums;

/// <summary>Lifecycle of a tenant's request to change plan, decided by a platform administrator.</summary>
public enum SubscriptionRequestStatus
{
    [Description("Pending")]
    Pending = 0,

    [Description("Approved")]
    Approved = 1,

    [Description("Rejected")]
    Rejected = 2,

    /// <summary>Withdrawn by the tenant before a decision was made.</summary>
    [Description("Cancelled")]
    Cancelled = 3
}

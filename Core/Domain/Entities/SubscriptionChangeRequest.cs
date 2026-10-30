using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A tenant's request to move onto a different <see cref="SubscriptionPlan"/>, awaiting a platform
/// administrator's decision. Tenant-scoped like any other entity, so a tenant sees only its own
/// requests while platform (root) scope sees them all. Approving one applies the plan to the
/// tenant's <see cref="TenantSubscription"/>; the request itself is just the paper trail.
///
/// Plan fields are snapshotted so the request still reads correctly if the catalogue plan is later
/// renamed or retired.
/// </summary>
public class SubscriptionChangeRequest : BaseEntity
{
    public string? RequestedPlanId { get; set; }

    public string? RequestedPlanName { get; set; }

    public string? RequestedPlanCode { get; set; }

    /// <summary>The tenant's plan code at the moment of the request, for the approver's context.</summary>
    public string? CurrentPlanCode { get; set; }

    public SubscriptionRequestStatus Status { get; set; } = SubscriptionRequestStatus.Pending;

    /// <summary>Optional message from the tenant explaining the request.</summary>
    public string? RequestNote { get; set; }

    public string? DecisionById { get; set; }

    public DateTime? DecisionAtUtc { get; set; }

    /// <summary>Optional message from the administrator with the approve/reject decision.</summary>
    public string? DecisionNote { get; set; }
}

using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// The Ticketing module's aggregate root. Deliberately module-agnostic: a ticket may optionally
/// reference a record in another module (ReferenceModule/ReferenceEntityType/ReferenceEntityId)
/// but never holds a real foreign key into another module's table, so Ticketing has no compile-
/// time dependency on Inventory/Sales/etc. entity types.
/// </summary>
public class Ticket : BaseEntity
{
    /// <summary>Human-readable, e.g. "TKT-000001". Unique per tenant — see TicketConfiguration.</summary>
    public string? TicketNumber { get; set; }

    public string? Subject { get; set; }
    public string? Description { get; set; }

    public string? CategoryId { get; set; }
    public string? PriorityId { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.New;
    public TicketSource Source { get; set; } = TicketSource.User;

    /// <summary>Who the ticket is for/from. Distinct from BaseEntity.CreatedById, which for a
    /// System-sourced ticket may be a service action rather than the affected person.</summary>
    public string? RequesterId { get; set; }

    public string? AssignedToId { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>Computed from the priority's configured SLA hours at creation/priority-change
    /// time (see TicketPriority). No background SLA engine in this iteration — "overdue" is a
    /// simple now &gt; SlaResolutionDueAtUtc &amp;&amp; not resolved comparison at query time.</summary>
    public DateTime? SlaResponseDueAtUtc { get; set; }
    public DateTime? SlaResolutionDueAtUtc { get; set; }

    /// <summary>Bumped on every comment/status/assignment/priority change — drives "Last Activity"
    /// sort/column without re-deriving it from history on every list query.</summary>
    public DateTime? LastActivityAtUtc { get; set; }

    /// <summary>Generic related-record reference (e.g. "Inventory" / "StockTransfer" / the entity's
    /// Id) so the UI can offer an "Open Related Record" link without Ticketing referencing the
    /// other module's entity type directly.</summary>
    public string? ReferenceModule { get; set; }
    public string? ReferenceEntityType { get; set; }
    public string? ReferenceEntityId { get; set; }
}

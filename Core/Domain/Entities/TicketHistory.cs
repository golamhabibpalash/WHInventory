using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A curated, human-readable timeline entry for the ticket detail page (e.g. "Status changed:
/// New → Open"). Distinct from the app-wide AuditLog, which CommandContext already writes
/// automatically for every entity change as a raw compliance snapshot — this table exists only
/// for the UX-facing activity feed, so only meaningful events are written here, not every field
/// touch.
/// </summary>
public class TicketHistory : BaseEntity
{
    public string? TicketId { get; set; }

    /// <summary>e.g. "Created", "StatusChanged", "PriorityChanged", "CategoryChanged",
    /// "Assigned", "CommentAdded", "InternalNoteAdded", "AttachmentUploaded", "Resolved",
    /// "Closed", "Reopened".</summary>
    public string? Action { get; set; }

    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? PerformedById { get; set; }
}

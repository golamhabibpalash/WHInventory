using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Tenant-customizable priority level. Level is the sort/severity weight (lower = more urgent);
/// the two SLA fields double as this module's SLA foundation (section 17 of the brief) — a
/// ticket's SlaResponseDueAtUtc/SlaResolutionDueAtUtc are computed by adding these hours to
/// CreatedAtUtc at creation/priority-change time. No breach-detection engine/cron in this
/// iteration; "overdue" is computed at query time.
/// </summary>
public class TicketPriority : BaseEntity
{
    public string? Name { get; set; }
    public string? ColorHex { get; set; }
    public int Level { get; set; }
    public int? SlaResponseHours { get; set; }
    public int? SlaResolutionHours { get; set; }
    public bool IsActive { get; set; } = true;
}
